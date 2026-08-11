namespace Nizam.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
