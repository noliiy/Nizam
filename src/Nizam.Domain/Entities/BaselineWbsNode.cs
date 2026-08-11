namespace Nizam.Domain.Entities;

public class BaselineWbsNode
{
    public Guid Id { get; set; }
    public Guid BaselineId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WbsNodeId { get; set; }
    public Guid? ParentWbsId { get; set; }
    public string WbsCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedFinish { get; set; }
    public DateTime? CurrentStart { get; set; }
    public DateTime? CurrentFinish { get; set; }
    public decimal Progress { get; set; }
}
