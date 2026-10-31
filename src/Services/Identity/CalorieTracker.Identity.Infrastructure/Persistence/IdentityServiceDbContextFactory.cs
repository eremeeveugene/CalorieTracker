using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalorieTracker.Identity.Infrastructure.Persistence;

// Lets `dotnet ef` create migrations without Aspire or a running database
internal sealed class IdentityServiceDbContextFactory : IDesignTimeDbContextFactory<IdentityServiceDbContext>
{
    public IdentityServiceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityServiceDbContext>()
            .UseNpgsql("Host=localhost;Database=identitydb")
            .Options;

        return new IdentityServiceDbContext(options);
    }
}
