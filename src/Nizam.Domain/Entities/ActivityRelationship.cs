using Nizam.Domain.Enums;
using Nizam.Domain.Exceptions;

namespace Nizam.Domain.Entities;

public class ActivityRelationship
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PredecessorActivityId { get; set; }
    public Guid SuccessorActivityId { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public int LagMinutes { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ActivityRelationship Create(
        Guid organizationId,
        Guid projectId,
        Guid predecessorActivityId,
        Guid successorActivityId,
        RelationshipType relationshipType,
        int lagMinutes = 0,
        Guid? id = null,
        DateTime? createdAt = null)
    {
        if (predecessorActivityId == successorActivityId)
        {
            throw new DomainException(
                "An activity cannot depend on itself.",
                "self_dependency");
        }

        return new ActivityRelationship
        {
            Id = id ?? Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            PredecessorActivityId = predecessorActivityId,
            SuccessorActivityId = successorActivityId,
            RelationshipType = relationshipType,
            LagMinutes = lagMinutes,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }
}
