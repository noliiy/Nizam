using Nizam.Domain.Exceptions;

namespace Nizam.Domain.Entities;

public class WbsNode
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Validates that assigning <paramref name="newParentId"/> does not create a cycle
    /// given the existing parent map (nodeId → parentId).
    /// </summary>
    public static void ValidateNoCycle(
        Guid nodeId,
        Guid? newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentByNodeId)
    {
        if (newParentId is null)
            return;

        if (newParentId.Value == nodeId)
            throw new CircularHierarchyException(nodeId, newParentId);

        var visited = new HashSet<Guid> { nodeId };
        var current = newParentId;

        while (current is not null)
        {
            if (!visited.Add(current.Value))
                throw new CircularHierarchyException(nodeId, newParentId);

            if (!parentByNodeId.TryGetValue(current.Value, out var parent))
                break;

            current = parent;
        }
    }

    public void SetParent(Guid? newParentId, IReadOnlyDictionary<Guid, Guid?> parentByNodeId)
    {
        ValidateNoCycle(Id, newParentId, parentByNodeId);
        ParentWbsId = newParentId;
    }
}
