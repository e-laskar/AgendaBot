using static AgendaBot.ArchitectureTests.Layers;

namespace AgendaBot.ArchitectureTests;

public class DependencyRulesTests
{
    [Fact]
    public void Domain_uses_no_other_project()
    {
        ProjectsUsedBy(DomainLayer).ShouldBeEmpty();
    }

    [Fact]
    public void Domain_uses_no_external_library()
    {
        ExternalLibrariesUsedBy(DomainLayer).ShouldBeEmpty();
    }

    [Fact]
    public void Application_uses_only_the_domain()
    {
        ProjectsUsedBy(ApplicationLayer).ShouldBe([NameOf(DomainLayer)]);
    }

    [Fact]
    public void Application_uses_no_external_library()
    {
        ExternalLibrariesUsedBy(ApplicationLayer).ShouldBeEmpty();
    }

    [Fact]
    public void Persistence_adapter_does_not_know_the_other_adapters()
    {
        ProjectsUsedBy(PersistenceAdapter).ShouldNotContain(NameOf(DiscordAdapter));
        ProjectsUsedBy(PersistenceAdapter).ShouldNotContain(NameOf(HostProject));
    }

    [Fact]
    public void Discord_adapter_does_not_know_the_other_adapters()
    {
        ProjectsUsedBy(DiscordAdapter).ShouldNotContain(NameOf(PersistenceAdapter));
        ProjectsUsedBy(DiscordAdapter).ShouldNotContain(NameOf(HostProject));
    }

    [Fact]
    public void Discord_library_is_only_used_by_the_discord_adapter()
    {
        All.Where(layer => layer != DiscordAdapter && ExternalLibrariesUsedBy(layer).Any(IsDiscordLibrary))
            .Select(NameOf)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Entity_framework_is_only_used_by_the_persistence_adapter()
    {
        All.Where(layer => layer != PersistenceAdapter && ExternalLibrariesUsedBy(layer).Any(IsEntityFramework))
            .Select(NameOf)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Nothing_depends_on_the_host()
    {
        All.Where(layer => ProjectsUsedBy(layer).Contains(NameOf(HostProject)))
            .Select(NameOf)
            .ShouldBeEmpty();
    }

    private static bool IsDiscordLibrary(string assemblyName) => assemblyName.StartsWith("Discord.", StringComparison.Ordinal);

    private static bool IsEntityFramework(string assemblyName) =>
        assemblyName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
        assemblyName.StartsWith("Npgsql", StringComparison.Ordinal);
}
