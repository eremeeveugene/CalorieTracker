using System.Reflection;
using CalorieTracker.Catalog.Infrastructure.Persistence;
using CalorieTracker.Identity.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace CalorieTracker.ArchitectureTests;

// Clean Architecture for every service: dependencies point inward only
// (Api -> Infrastructure -> Application -> Domain)
public class ServiceLayerTests
{
    // One type per service is enough to reach its Infrastructure assembly; the other layers
    // are loaded by name next to it
    private static readonly Dictionary<string, Type> InfrastructureMarkers = new()
    {
        ["Identity"] = typeof(IdentityServiceDbContext),
        ["Catalog"] = typeof(CatalogDbContext),
    };

    public static TheoryData<string> Services => [.. InfrastructureMarkers.Keys];

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_DependsOnNoOtherLayerOrFramework(string service)
    {
        var result = Types.InAssembly(Layer(service, "Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(
                Namespace(service, "Application"),
                Namespace(service, "Infrastructure"),
                Namespace(service, "Api"),
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions")
            .GetResult();

        AssertSuccessful(result);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_DependsOnNeitherInfrastructureNorWebOrDatabaseFrameworks(string service)
    {
        var result = Types.InAssembly(Layer(service, "Application"))
            .ShouldNot()
            .HaveDependencyOnAny(
                Namespace(service, "Infrastructure"),
                Namespace(service, "Api"),
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Infrastructure_DoesNotDependOnApi(string service)
    {
        var result = Types.InAssembly(Layer(service, "Infrastructure"))
            .ShouldNot()
            .HaveDependencyOn(Namespace(service, "Api"))
            .GetResult();

        AssertSuccessful(result);
    }

    private static string Namespace(string service, string layer) => $"CalorieTracker.{service}.{layer}";

    private static Assembly Layer(string service, string layer) =>
        Assembly.Load(new AssemblyName(Namespace(service, layer)));

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(result.IsSuccessful, $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
}
