using System.Reflection;
using static AgendaBot.ArchitectureTests.Layers;

namespace AgendaBot.ArchitectureTests;

public class CodingRulesTests
{
    public static TheoryData<string> CoreLayers => [NameOf(DomainLayer), NameOf(ApplicationLayer)];

    [Theory]
    [MemberData(nameof(CoreLayers))]
    public void Concrete_classes_of_the_core_are_sealed(string layerName)
    {
        var layer = All.Single(assembly => NameOf(assembly) == layerName);

        UnsealedConcreteClassesOf(layer).ShouldBeEmpty();
    }

    private static IEnumerable<string> UnsealedConcreteClassesOf(Assembly layer) =>
        layer.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.IsSealed)
            .Select(type => type.FullName!)
            .Order();
}
