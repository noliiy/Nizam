using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nizam.Domain.Entities;

namespace Nizam.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("organizations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
    }
}

public sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> b)
    {
        b.ToTable("organization_members");
        b.HasKey(x => x.Id);
        b.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
    }
}

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("projects");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProjectCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Timezone).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.ProjectCode }).IsUnique();
    }
}

public sealed class WbsNodeConfiguration : IEntityTypeConfiguration<WbsNode>
{
    public void Configure(EntityTypeBuilder<WbsNode> b)
    {
        b.ToTable("wbs_nodes");
        b.HasKey(x => x.Id);
        b.Property(x => x.WbsCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.ProjectId, x.WbsCode }).IsUnique();
    }
}

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> b)
    {
        b.ToTable("activities");
        b.HasKey(x => x.Id);
        b.Property(x => x.ActivityCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.RowVersion).IsConcurrencyToken();
        b.HasIndex(x => new { x.ProjectId, x.ActivityCode }).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.ProjectId });
        b.HasIndex(x => new { x.ProjectId, x.WbsId });
        b.HasIndex(x => new { x.ProjectId, x.IsCritical });
    }
}

public sealed class ActivityRelationshipConfiguration : IEntityTypeConfiguration<ActivityRelationship>
{
    public void Configure(EntityTypeBuilder<ActivityRelationship> b)
    {
        b.ToTable("activity_relationships");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ProjectId, x.PredecessorActivityId });
        b.HasIndex(x => new { x.ProjectId, x.SuccessorActivityId });
    }
}

public sealed class CalendarConfiguration : IEntityTypeConfiguration<Calendar>
{
    public void Configure(EntityTypeBuilder<Calendar> b)
    {
        b.ToTable("calendars");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class CalendarWorkPatternConfiguration : IEntityTypeConfiguration<CalendarWorkPattern>
{
    public void Configure(EntityTypeBuilder<CalendarWorkPattern> b)
    {
        b.ToTable("calendar_work_patterns");
        b.HasKey(x => x.Id);
    }
}

public sealed class CalendarWorkIntervalConfiguration : IEntityTypeConfiguration<CalendarWorkInterval>
{
    public void Configure(EntityTypeBuilder<CalendarWorkInterval> b)
    {
        b.ToTable("calendar_work_intervals");
        b.HasKey(x => x.Id);
    }
}

public sealed class CalendarExceptionConfiguration : IEntityTypeConfiguration<CalendarException>
{
    public void Configure(EntityTypeBuilder<CalendarException> b)
    {
        b.ToTable("calendar_exceptions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.CalendarId, x.Date }).IsUnique();
    }
}

public sealed class ScheduleRunConfiguration : IEntityTypeConfiguration<ScheduleRun>
{
    public void Configure(EntityTypeBuilder<ScheduleRun> b)
    {
        b.ToTable("schedule_runs");
        b.HasKey(x => x.RunId);
    }
}

public sealed class ProgressUpdateConfiguration : IEntityTypeConfiguration<ProgressUpdate>
{
    public void Configure(EntityTypeBuilder<ProgressUpdate> b)
    {
        b.ToTable("progress_updates");
        b.HasKey(x => x.Id);
    }
}

public sealed class BaselineConfiguration : IEntityTypeConfiguration<Baseline>
{
    public void Configure(EntityTypeBuilder<Baseline> b)
    {
        b.ToTable("baselines");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class BaselineActivityConfiguration : IEntityTypeConfiguration<BaselineActivity>
{
    public void Configure(EntityTypeBuilder<BaselineActivity> b)
    {
        b.ToTable("baseline_activities");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BaselineId, x.ActivityId });
    }
}

public sealed class BaselineWbsNodeConfiguration : IEntityTypeConfiguration<BaselineWbsNode>
{
    public void Configure(EntityTypeBuilder<BaselineWbsNode> b)
    {
        b.ToTable("baseline_wbs_nodes");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BaselineId, x.WbsNodeId });
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
    }
}
