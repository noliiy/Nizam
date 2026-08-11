using Microsoft.EntityFrameworkCore;
using Nizam.Domain.Entities;

namespace Nizam.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<OrganizationMember> OrganizationMembers { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Project> Projects { get; }
    DbSet<WbsNode> WbsNodes { get; }
    DbSet<Activity> Activities { get; }
    DbSet<ActivityRelationship> ActivityRelationships { get; }
    DbSet<Calendar> Calendars { get; }
    DbSet<CalendarWorkPattern> CalendarWorkPatterns { get; }
    DbSet<CalendarWorkInterval> CalendarWorkIntervals { get; }
    DbSet<CalendarException> CalendarExceptions { get; }
    DbSet<ScheduleRun> ScheduleRuns { get; }
    DbSet<ProgressUpdate> ProgressUpdates { get; }
    DbSet<Baseline> Baselines { get; }
    DbSet<BaselineActivity> BaselineActivities { get; }
    DbSet<BaselineWbsNode> BaselineWbsNodes { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
