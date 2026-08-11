using Nizam.Domain.Enums;

namespace Nizam.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? PortfolioId { get; set; }
    public Guid? ProgramId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public int Priority { get; set; }
    public Guid? ProjectManagerId { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedFinishDate { get; set; }
    public DateTime? CurrentStartDate { get; set; }
    public DateTime? CurrentFinishDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualFinishDate { get; set; }
    public DateTime? DataDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Timezone { get; set; } = "Europe/Istanbul";
    public Guid? DefaultCalendarId { get; set; }
    public decimal PercentComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
