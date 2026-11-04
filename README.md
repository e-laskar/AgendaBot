# AgendaBot

Bot Discord (C# / .NET 10, Discord.Net) pour gérer un agenda de serveur.

## Commandes

| Commande | Description |
|---|---|
| `/agenda ajouter titre date [description] [duree]` | Ajoute un événement (date au format `jj/mm/aaaa hh:mm`) |
| `/agenda lister [nombre]` | Liste les prochains événements |
| `/agenda voir id` | Détail d'un événement |
| `/agenda supprimer id` | Supprime un événement (permission « Gérer les événements ») |

Un rappel est posté dans le salon d'origine `ReminderMinutesBefore` minutes avant chaque événement.

## Lancer le bot

1. Créer une application sur https://discord.com/developers/applications, ajouter un bot et copier son token.
2. Inviter le bot avec les scopes `bot` + `applications.commands`.
3. Renseigner le token (hors du dépôt) :
   ```bash
   dotnet user-secrets set "Discord:Token" "<TOKEN>" --project src/AgendaBot.Host
   ```
   ou via la variable d'environnement `Discord__Token` (voir « Configuration » ci-dessous).
4. (Dev) Renseigner `Discord:DevGuildId` avec l'ID de votre serveur de test pour un enregistrement instantané des commandes.
5. `DOTNET_ENVIRONMENT=Development dotnet run --project src/AgendaBot.Host`

## Tests

```bash
dotnet test
```

Tests unitaires : xUnit v3 + [Shouldly](https://docs.shouldly.org/) pour les assertions.

## Configuration

Toutes les clés de `appsettings.json` peuvent être surchargées par variable d'environnement,
en remplaçant `:` par `__` (double underscore) :

| Variable | Défaut | Description |
|---|---|---|
| `Discord__Token` | — (obligatoire) | Token du bot |
| `Discord__DevGuildId` | vide | Serveur de test pour l'enregistrement instantané des commandes |
| `Discord__TimeZone` | `Europe/Paris` | Fuseau horaire des dates saisies (ID IANA) |
| `Discord__ReminderMinutesBefore` | `15` | Délai du rappel avant l'événement (> 0) |
| `Database__Provider` | `Sqlite` | `Sqlite` ou `Postgres` |
| `ConnectionStrings__Agenda` | `Data Source=agenda.db` | Chaîne de connexion (SQLite ou Npgsql) |
| `DOTNET_ENVIRONMENT` | `Production` | `Development` active les user-secrets |

### Fichier `.env`

Pour le dev local, copier `.env.example` en `.env` (ignoré par git) et le remplir :

```bash
cp .env.example .env
```

Le fichier est lu au démarrage depuis le répertoire courant ou l'un de ses parents (la racine du dépôt
convient donc aussi quand l'IDE lance depuis `src/AgendaBot.Host`), à défaut à côté de l'exécutable.
Les variables déjà définies dans l'environnement ne sont pas écrasées, et les valeurs vides sont ignorées
(un `Discord__Token=` vide ne masque donc pas le token des user-secrets).

Ordre de priorité, du plus faible au plus fort : `appsettings.json` → `appsettings.{Env}.json` →
user-secrets (Development) → variables d'environnement / `.env` → arguments de ligne de commande.

La configuration est validée au démarrage : token manquant, fuseau horaire inconnu ou délai de rappel
invalide font échouer le lancement avec un message explicite.

## Déploiement Docker

`compose.yaml` lance deux services :

- **`db`** : PostgreSQL 17 (volume `pg-data`, non exposé sur l'hôte, healthcheck `pg_isready`) ;
- **`agendabot`** : le bot, démarré une fois la base prête, avec `Database__Provider=Postgres`.

```bash
cp .env.example .env        # renseigner Discord__Token et POSTGRES_PASSWORD
docker compose up -d --build
docker compose logs -f agendabot
```

Le schéma est créé automatiquement au premier démarrage. Les variables `POSTGRES_DB`, `POSTGRES_USER`
et `POSTGRES_PASSWORD` de `.env` servent à la fois au service `db` et à la chaîne de connexion du bot.
Le fichier `.env` n'est pas copié dans l'image (`.dockerignore`) : il est injecté à l'exécution.

Accès à la base : `docker compose exec db psql -U agenda agenda`.

Sans Compose, l'image fonctionne aussi seule avec SQLite :

```bash
docker build -t agendabot .
docker run -d --name agendabot --restart unless-stopped \
  -e Discord__Token=<TOKEN> \
  -v agenda-data:/data \
  agendabot
```

### Fournisseur de base de données

`Database__Provider` vaut `Sqlite` (défaut) ou `Postgres` ; `ConnectionStrings__Agenda` doit correspondre.

> Le binaire SQLite embarqué requiert glibc ≥ 2.34 : il ne se charge pas sous Ubuntu 20.04 (glibc 2.31).
> Sur ces systèmes, utiliser Docker ou Postgres en local.

## Structure

```
AgendaBot.slnx
Directory.Build.props         Propriétés communes (net10.0, nullable, warnings = erreurs)
Directory.Packages.props      Versions des packages NuGet (gestion centralisée)
src/AgendaBot.Host/
  Program.cs                  Host, DI, création de la base
  Configuration/              Options (token, fuseau horaire, rappels) et chargement du .env
  Models/AgendaEvent.cs       Entité événement
  Data/AgendaDbContext.cs     DbContext EF Core
  Services/                   Accès aux données, connexion Discord, rappels
  Modules/AgendaModule.cs     Slash commands /agenda
tests/AgendaBot.UnitTests/    Tests unitaires (xUnit v3 + Shouldly)
```

Migration en cours vers une architecture hexagonale (Domain / Application / adaptateurs).
