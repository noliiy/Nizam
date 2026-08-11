using Microsoft.EntityFrameworkCore;
using Nizam.Application.Abstractions;
using Nizam.Domain.Entities;

namespace Nizam.Infrastructure.Persistence;

public sealed class NizamDbContext : DbContext, IApplicationDbContext
{
    public NizamDbContext(DbContextOptions<NizamDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WbsNode> WbsNodes => Set<WbsNode>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityRelationship> ActivityRelationships => Set<ActivityRelationship>();
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<CalendarWorkPattern> CalendarWorkPatterns => Set<CalendarWorkPattern>();
    public DbSet<CalendarWorkInterval> CalendarWorkIntervals => Set<CalendarWorkInterval>();
    public DbSet<CalendarException> CalendarExceptions => Set<CalendarException>();
    public DbSet<ScheduleRun> ScheduleRuns => Set<ScheduleRun>();
    public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();
    public DbSet<Baseline> Baselines => Set<Baseline>();
    public DbSet<BaselineActivity> BaselineActivities => Set<BaselineActivity>();
    public DbSet<BaselineWbsNode> BaselineWbsNodes => Set<BaselineWbsNode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NizamDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
