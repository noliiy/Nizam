using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nizam.Domain.Entities;
using Nizam.Domain.Security;

namespace Nizam.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static readonly Guid DemoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DemoOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NizamDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        try
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EnsureCreated failed; continuing seed if database is available.");
        }

        if (!await db.Permissions.AnyAsync(cancellationToken))
        {
            foreach (var code in Permissions.All)
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Description = code
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Roles.AnyAsync(cancellationToken))
        {
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Administrator,
                Description = "Tam yetkili yönetici"
            };
            var pmRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.ProjectManager,
                Description = "Proje yöneticisi"
            };
            db.Roles.AddRange(adminRole, pmRole);
            await db.SaveChangesAsync(cancellationToken);

            var perms = await db.Permissions.ToListAsync(cancellationToken);
            foreach (var p in perms)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = adminRole.Id,
                    PermissionId = p.Id
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(u => u.Email == "admin@nizam.local", cancellationToken))
        {
            db.Users.Add(new User
            {
                Id = DemoUserId,
                Email = "admin@nizam.local",
                DisplayName = "Nizam Admin",
                PasswordHash = HashPassword("Admin123!"),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Organizations.AnyAsync(o => o.Id == DemoOrgId, cancellationToken))
        {
            var now = DateTime.UtcNow;
            db.Organizations.Add(new Organization
            {
                Id = DemoOrgId,
                Name = "Demo Organizasyon",
                CreatedAt = now,
                UpdatedAt = now
            });
            db.OrganizationMembers.Add(new OrganizationMember
            {
                Id = Guid.NewGuid(),
                OrganizationId = DemoOrgId,
                UserId = DemoUserId,
                RoleName = RoleNames.Administrator
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Database seed completed.");
    }
}
