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

| Projet | Contenu | Prérequis |
|---|---|---|
| `tests/AgendaBot.UnitTests` | Domaine, cas d'usage, contrat du repository sur le fake en mémoire | aucun |
| `tests/AgendaBot.ArchitectureTests` | Sens des dépendances entre projets, classes du cœur scellées | aucun |
| `tests/AgendaBot.ComponentTests` — `Discord/` | Lecture des dates (fuseau, heure d'été), embeds et messages | aucun |
| `tests/AgendaBot.ComponentTests` — `Persistence/` | Contrat du repository sur PostgreSQL réel (Testcontainers) | Docker démarré |

```bash
dotnet test --project tests/AgendaBot.UnitTests
```
```bash
dotnet test --project tests/AgendaBot.ArchitectureTests
```
```bash
dotnet test --project tests/AgendaBot.ComponentTests
```

`dotnet test` à la racine lance les deux. Assertions : [Shouldly](https://docs.shouldly.org/).

Le même contrat (`tests/AgendaBot.Testing/Contracts/EventRepositoryContract.cs`) est exécuté contre le fake
en mémoire et contre `EfEventRepository` sur PostgreSQL : le fake utilisé par les tests unitaires se comporte
donc comme la vraie base.

### Intégration continue

`.github/workflows/ci.yml` s'exécute à chaque push sur `main`, sur chaque pull request et à la demande :

- **Compilation et tests** : compilation Release (warnings = erreurs), puis tests unitaires, d'architecture et
  de composant. Docker est disponible sur les runners GitHub : le contrat du repository y tourne sur un vrai
  PostgreSQL. Toutes les suites s'exécutent même si l'une échoue ; les rapports TRX sont publiés en artefact
  `rapports-de-tests`.
- **Image Docker** : construction de l'image pour vérifier le `Dockerfile` (sans publication).

## Configuration

Toutes les clés de `appsettings.json` peuvent être surchargées par variable d'environnement,
en remplaçant `:` par `__` (double underscore) :

| Variable | Défaut | Description |
|---|---|---|
| `Discord__Token` | — (obligatoire) | Token du bot |
| `Discord__DevGuildId` | vide | Serveur de test pour l'enregistrement instantané des commandes |
| `Discord__TimeZone` | `Europe/Paris` | Fuseau horaire des dates saisies (ID IANA) |
| `Discord__ReminderMinutesBefore` | `15` | Délai du rappel avant l'événement (> 0) |
| `Agenda__MaxUpcomingEventsPerCreator` | `10` | Nombre maximal d'événements à venir créés par un même membre sur un serveur (> 0) |
| `ConnectionStrings__Agenda` | — (obligatoire) | Chaîne de connexion PostgreSQL (Npgsql) |
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

La configuration est validée au démarrage : token manquant, fuseau horaire inconnu, délai de rappel ou
quota d'événements invalide font échouer le lancement avec un message explicite.

## Déploiement Docker

`compose.yaml` lance deux services :

- **`db`** : PostgreSQL 17 (volume `pg-data`, non exposé sur l'hôte, healthcheck `pg_isready`) ;
- **`agendabot`** : le bot, démarré une fois la base prête.

```bash
cp .env.example .env        # renseigner Discord__Token et POSTGRES_PASSWORD
docker compose up -d --build
docker compose logs -f agendabot
```

Les migrations EF Core sont appliquées automatiquement au démarrage du bot. Les variables `POSTGRES_DB`, `POSTGRES_USER`
et `POSTGRES_PASSWORD` de `.env` servent à la fois au service `db` et à la chaîne de connexion du bot.
Le fichier `.env` n'est pas copié dans l'image (`.dockerignore`) : il est injecté à l'exécution.

Accès à la base : `docker compose exec db psql -U agenda agenda`.

### Base de données en dev local

Le bot utilise uniquement PostgreSQL. Pour le lancer hors Docker Compose, démarrer une base locale :

```bash
docker run -d --name agenda-db -p 5432:5432 -e POSTGRES_DB=agenda -e POSTGRES_USER=agenda -e POSTGRES_PASSWORD=changeme postgres:17-alpine
```

La chaîne de connexion correspondante est déjà dans `.env.example`.

### Migrations

Le schéma est géré par des migrations EF Core (`src/AgendaBot.Persistence/Migrations`), appliquées au démarrage.
Après une modification de `EventRecord` ou `AgendaDbContext` :

```bash
dotnet tool restore
```
```bash
dotnet ef migrations add <NomDeLaMigration> --project src/AgendaBot.Persistence
```

Le test `MigrationsTests` échoue tant que la migration n'a pas été générée.

## Structure

Architecture hexagonale : les dépendances pointent vers le domaine. Les règles sont vérifiées par
`tests/AgendaBot.ArchitectureTests` :

- `Domain` ne dépend de rien ; `Application` ne dépend que de `Domain` ;
- les adaptateurs `Persistence` et `Discord` ne se connaissent pas ;
- EF Core / Npgsql ne sont utilisés que par `Persistence`, Discord.Net que par `Discord` ;
- seul `Host` assemble le tout, et rien ne dépend de lui ;
- les classes concrètes de `Domain` et `Application` sont `sealed`.

```
AgendaBot.slnx
Directory.Build.props            Propriétés communes (net10.0, nullable, warnings = erreurs)
Directory.Packages.props         Versions des packages NuGet (gestion centralisée)
src/
  AgendaBot.Domain/              Cœur métier, sans dépendance
    AgendaEvent.cs               Entité : création validée, logique de rappel
    EventTitle.cs, EventDescription.cs, DiscordIds.cs   Value objects
    Result.cs, Error.cs, DomainErrors.cs                Erreurs métier sans exceptions
  AgendaBot.Application/         Cas d'usage et ports
    Ports/                       IEventRepository, IReminderNotifier
    UseCases/                    AddEvent, ListUpcomingEvents, GetEvent, DeleteEvent, SendDueReminders
  AgendaBot.Persistence/         Adaptateur EF Core PostgreSQL, migrations
  AgendaBot.Discord/             Adaptateur Discord
    Commands/                    Slash commands /agenda, lecture des dates saisies
    Presentation/                Embeds et messages
    Reminders/                   Envoi des rappels, worker périodique
  AgendaBot.Host/                Démarrage, assemblage des services, chargement du .env
tests/
  AgendaBot.Testing/             Builder, fakes et contrat du repository (partagés)
  AgendaBot.UnitTests/           Tests unitaires (xUnit v3 + Shouldly)
  AgendaBot.ArchitectureTests/   Règles de dépendances entre projets
  AgendaBot.ComponentTests/      Adaptateurs : Discord (sans infra), PostgreSQL (Testcontainers)
```
