using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Identity.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task ApplyIdentityMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
