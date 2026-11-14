# AgendaBot

Bot Discord d'agenda de serveur : C# / .NET 10, Discord.Net, EF Core sur PostgreSQL uniquement.
Configuration, déploiement et commandes du bot : voir `README.md`.

## Commandes

```bash
dotnet build --configuration Release          # warnings = erreurs
dotnet test --project tests/AgendaBot.UnitTests
dotnet test --project tests/AgendaBot.ArchitectureTests
dotnet test --project tests/AgendaBot.ComponentTests   # Persistence/ nécessite Docker (Testcontainers)
dotnet tool restore && dotnet ef migrations add <Nom> --project src/AgendaBot.Persistence
```

Les sorties de compilation vont dans `.artifacts/<os>/`, pas dans `bin/` ni `obj/`.

## Architecture hexagonale

Les dépendances pointent vers le domaine. `tests/AgendaBot.ArchitectureTests` le vérifie : ne contourne jamais un échec de ces tests.

- `Domain` : entités et value objects, aucune dépendance (pas même Microsoft.Extensions).
- `Application` : un cas d'usage par classe dans `UseCases/` (`HandleAsync(command)` → `Result<T>`), ports dans `Ports/`. Ne dépend que de `Domain`.
- `Persistence` : seul projet qui connaît EF Core et Npgsql. Implémente `IEventRepository`.
- `Discord` : seul projet qui connaît Discord.Net. Commandes (`Commands/`), mise en forme (`Presentation/`), rappels (`Reminders/`).
- `Host` : assemble tout (`AgendaBotSetup`), valide les options au démarrage. Rien ne dépend de lui.
- `Persistence` et `Discord` ne se connaissent pas.

Une règle métier va dans `Domain` ou `Application`, jamais dans un adaptateur. Un nouvel accès externe se fait par un port dans `Application/Ports`.

## Conventions de code

- Pas de commentaires : l'intention passe par le nommage. Extrais une méthode ou une variable bien nommée plutôt que d'expliquer.
- Les classes concrètes de `Domain` et `Application` sont `sealed`. Préfère les `record` pour les commandes et les value objects.
- Les erreurs métier sont des `Result<T>` / `Error` (`DomainErrors`, `ApplicationErrors`), pas des exceptions. Les exceptions restent pour l'infrastructure.
- Value objects : constructeur privé et `static Result<T> Create(...)` qui valide et normalise.
- Constructeurs primaires pour l'injection de dépendances.
- Le temps passe toujours par `TimeProvider`, jamais par `DateTime.Now` ou `DateTimeOffset.UtcNow`. Stocke en UTC, convertis dans le fuseau configuré seulement à l'entrée et à l'affichage.
- Identifiants typés (`GuildId`, `ChannelId`, `UserId`) plutôt que des `ulong` nus.
- Toute méthode async prend un `CancellationToken` et le propage.
- Code en anglais. Textes affichés aux utilisateurs Discord en français.
- Versions de packages uniquement dans `Directory.Packages.props`.

## Tests

- `UnitTests` : domaine, cas d'usage (avec les fakes) et configuration du Host.
- `ComponentTests` : adaptateurs. `Discord/` sans infrastructure, `Persistence/` sur un vrai PostgreSQL.
- `ArchitectureTests` : règles de dépendances et de code. Ajoutes-y toute nouvelle règle structurelle.
- `Testing` : builder (`AnAgendaEvent`), fakes (`InMemoryEventRepository`, `SpyReminderNotifier`) et `EventRepositoryContract`, partagés entre les suites.
- Toute nouvelle méthode de `IEventRepository` est couverte dans `EventRepositoryContract`, qui tourne sur le fake **et** sur PostgreSQL, puis implémentée dans les deux.
- xUnit v3 et Shouldly. Les noms de tests sont des phrases en anglais : `Rejects_event_starting_before_current_time`. Corps en Arrange / Act / Assert, séparés par une ligne vide.
- `FakeTimeProvider` pour tout ce qui dépend du temps.

## Contraintes de production (déjà rencontrées, à anticiper)

- **Entrées non fiables** : tout texte saisi par un membre et réaffiché est envoyé sans mention autorisée, avec `new AllowedMentions(AllowedMentionTypes.None)` (pas `AllowedMentions.None`, qui sérialise `"parse": null`). Utilise `OutgoingMessage` / `AgendaEventMessages`.
- **Délai de 3 s de Discord** : une commande qui touche la base appelle `DeferAsync` puis complète la réponse (voir `DeferredInteractionReplies`). Les erreurs restent éphémères.
- **Plusieurs instances, pannes partielles** : une action à effet de bord unique se réserve atomiquement en base avant de s'exécuter (modèle `TryClaimReminderAsync` / `ReleaseReminderClaimAsync`). Ne marque jamais une action comme faite avant qu'elle ait réussi.
- **Reconnexions** : `Ready` se déclenche à chaque reconnexion au gateway. Une initialisation unique passe par `UntilFirstSuccess`.
- **Cache Discord incomplet** : il peut être vide au démarrage. Cherche dans le cache puis via l'API REST, et un échec doit remonter, pas être journalisé puis ignoré.
- **Abus** : toute création par un membre est bornée (quota configurable, validé au démarrage).

Pour chaque nouvelle fonctionnalité, pose-toi explicitement la question de ces six points.

## Méthode de travail

- Nouvelle fonctionnalité ou changement transverse : propose d'abord un plan (couches touchées, ports, tests, cas d'échec) en mode plan, et attends sa validation. Le modèle du projet est `opusplan` : Opus planifie en mode plan, un modèle plus économe implémente une fois le plan validé.
- Écris les tests avant l'implémentation, en commençant par le cas d'usage.
- Avant chaque commit : build Release, puis tests unitaires et tests d'architecture au vert. Lance les tests de composant si un adaptateur a changé.
- Le hook Stop (`.claude/hooks/VerifyBeforeStop.cs`) relance build et tests à chaque fin de tâche si `src/` ou `tests/` ont changé, et bloque tant que c'est rouge. Corrige la cause, ne le contourne pas.
- Modification de `EventRecord` ou `AgendaDbContext` : génère la migration (`MigrationsTests` échoue sinon).
- Nouvelle option de configuration : valide-la au démarrage et documente-la dans `README.md` et `.env.example`.
- Un commit par idée. Message en français, préfixé par la couche (`Domain :`, `Application :`, `Persistence :`, `Discord :`, `Host :`, `Tests :`, `CI :`), titre qui dit le résultat. Corps qui explique le problème, puis une liste des changements.
