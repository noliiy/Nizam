using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nizam.Domain.Security;
using Nizam.Infrastructure.Persistence;
using Nizam.Infrastructure.Persistence.Seed;

namespace Nizam.Infrastructure.Tests;

public class SeedTests
{
    [Fact]
    public async Task Seed_Creates_Admin_And_Permissions()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<NizamDbContext>(o => o.UseInMemoryDatabase(dbName));

        await using var sp = services.BuildServiceProvider();
        await DatabaseSeeder.SeedAsync(sp);

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NizamDbContext>();

        (await db.Users.AnyAsync(u => u.Email == "admin@nizam.local")).Should().BeTrue();
        (await db.Permissions.CountAsync()).Should().BeGreaterThanOrEqualTo(Permissions.All.Count);
        (await db.Roles.AnyAsync(r => r.Name == RoleNames.Administrator)).Should().BeTrue();
    }
}
