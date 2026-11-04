using System.Reflection;
using AgendaBot.Application.UseCases;
using AgendaBot.Configuration;
using AgendaBot.Discord;
using AgendaBot.Domain;
using AgendaBot.Persistence;

namespace AgendaBot.ArchitectureTests;

internal static class Layers
{
    public static readonly Assembly DomainLayer = typeof(AgendaEvent).Assembly;
    public static readonly Assembly ApplicationLayer = typeof(AddEvent).Assembly;
    public static readonly Assembly PersistenceAdapter = typeof(EfEventRepository).Assembly;
    public static readonly Assembly DiscordAdapter = typeof(DiscordSetup).Assembly;
    public static readonly Assembly HostProject = typeof(DotEnv).Assembly;

    public static readonly Assembly[] All = [DomainLayer, ApplicationLayer, PersistenceAdapter, DiscordAdapter, HostProject];

    public static IEnumerable<string> ProjectsUsedBy(Assembly assembly) =>
        DependenciesOf(assembly).Where(IsOneOfOurProjects).Order();

    public static IEnumerable<string> ExternalLibrariesUsedBy(Assembly assembly) =>
        DependenciesOf(assembly).Where(name => !IsOneOfOurProjects(name) && !IsDotNetRuntime(name)).Order();

    public static string NameOf(Assembly assembly) => assembly.GetName().Name!;

    private static IEnumerable<string> DependenciesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name!);

    private static bool IsOneOfOurProjects(string assemblyName) => assemblyName.StartsWith("AgendaBot.", StringComparison.Ordinal);

    private static bool IsDotNetRuntime(string assemblyName) =>
        assemblyName is "netstandard" or "mscorlib" || assemblyName.StartsWith("System", StringComparison.Ordinal);
}
