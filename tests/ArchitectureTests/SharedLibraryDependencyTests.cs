using System.Reflection;

namespace CalorieTracker.ArchitectureTests;

// Contracts and BuildingBlocks are shared by every service and the client,
// so they must stay free of framework and infrastructure dependencies.
public class SharedLibraryDependencyTests
{
    [Theory]
    [InlineData("CalorieTracker.Contracts")]
    [InlineData("CalorieTracker.BuildingBlocks")]
    public void SharedLibrary_ReferencesOnlyBaseClassLibrary(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);

        var nonBaseReferences = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(name => !IsBaseClassLibrary(name))
            .ToList();

        Assert.Empty(nonBaseReferences);
    }

    private static bool IsBaseClassLibrary(string name) =>
        name.StartsWith("System", StringComparison.Ordinal)
        || name is "netstandard" or "mscorlib";
}
