using Nizam.Domain.Enums;

namespace Nizam.Domain.Entities;

public class Baseline
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BaselineType BaselineType { get; set; }
    public BaselineStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
