using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Domain.Entities;

namespace Nizam.Application.Common;

public static class TenantAuthorization
{
    public static async Task EnsureOrganizationAccessAsync(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new ForbiddenException("Kimlik doğrulama gerekli.");

        var isMember = await db.OrganizationMembers.AsNoTracking().AnyAsync(
            m => m.OrganizationId == organizationId && m.UserId == currentUser.UserId.Value,
            cancellationToken);

        if (!isMember)
            throw new ForbiddenException("Bu organizasyona erişim yetkiniz yok.");
    }

    public static async Task<Project> EnsureProjectAccessAsync(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        Guid projectId,
        CancellationToken cancellationToken = default,
        bool tracking = true)
    {
        var query = tracking ? db.Projects.AsQueryable() : db.Projects.AsNoTracking();
        var project = await query.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new NotFoundException("Proje bulunamadı.");

        await EnsureOrganizationAccessAsync(db, currentUser, project.OrganizationId, cancellationToken);
        return project;
    }
}
