using CalorieTracker.Identity.Application.Login;
using CalorieTracker.Identity.Domain.RefreshTokens;
using CalorieTracker.Identity.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace CalorieTracker.ArchitectureTests;

// Clean Architecture: dependencies point inward only (Api -> Infrastructure -> Application -> Domain)
public class IdentityLayerTests
{
    private const string Application = "CalorieTracker.Identity.Application";
    private const string Infrastructure = "CalorieTracker.Identity.Infrastructure";
    private const string Api = "CalorieTracker.Identity.Api";

    [Fact]
    public void Domain_DependsOnNoOtherLayerOrFramework()
    {
        var result = Types.InAssembly(typeof(RefreshToken).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api, "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Microsoft.Extensions")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_DependsOnNeitherInfrastructureNorWebOrDatabaseFrameworks()
    {
        var result = Types.InAssembly(typeof(LoginCommand).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api, "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(typeof(IdentityServiceDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn(Api)
            .GetResult();

        AssertSuccessful(result);
    }

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(result.IsSuccessful, $"Violating types: {string.Join(", ", result.FailingTypeNames ?? [])}");
}
