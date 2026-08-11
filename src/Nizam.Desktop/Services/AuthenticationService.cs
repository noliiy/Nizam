namespace Nizam.Desktop.Services;

public sealed class AuthenticationService
{
    public string? Token { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public void SetSession(string token, Guid userId, string email, string displayName, Guid? organizationId)
    {
        Token = token;
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        OrganizationId = organizationId;
    }

    public void Clear()
    {
        Token = null;
        UserId = null;
        Email = null;
        DisplayName = null;
        OrganizationId = null;
    }
}
