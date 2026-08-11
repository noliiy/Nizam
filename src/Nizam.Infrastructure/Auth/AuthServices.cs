using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nizam.Application.Abstractions;

namespace Nizam.Infrastructure.Auth;

public sealed class DevJwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public DevJwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(Guid userId, string email, Guid? organizationId = null)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key yapılandırılmamış.");
        var issuer = _configuration["Jwt:Issuer"] ?? "nizam";
        var audience = _configuration["Jwt:Audience"] ?? "nizam-api";
        var hours = int.TryParse(_configuration["Jwt:ExpiryHours"], out var h) ? h : 12;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email)
        };

        if (organizationId is not null)
            claims.Add(new Claim("org", organizationId.Value.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _http.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _http.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public Guid? OrganizationId
    {
        get
        {
            var value = _http.HttpContext?.User?.FindFirstValue("org");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}

public sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
