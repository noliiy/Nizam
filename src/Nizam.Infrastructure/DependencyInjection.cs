using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nizam.Application.Abstractions;
using Nizam.Infrastructure.Auth;
using Nizam.Infrastructure.Outbox;
using Nizam.Infrastructure.Persistence;
using Nizam.Scheduling.Services;

namespace Nizam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=nizam;Username=nizam;Password=nizam";

        var useInMemory = string.Equals(
            configuration["Database:Provider"],
            "InMemory",
            StringComparison.OrdinalIgnoreCase);

        if (useInMemory)
        {
            var dbName = configuration["Database:InMemoryName"] ?? "NizamTests";
            services.AddDbContext<NizamDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        }
        else
        {
            services.AddDbContext<NizamDbContext>(opt => opt.UseNpgsql(connectionString));
        }

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NizamDbContext>());
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddScoped<IJwtTokenService, DevJwtTokenService>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<ISchedulingEngine, SchedulingEngine>();
        services.AddHttpClient("OutboxWebhook");
        services.AddHostedService<OutboxDispatcherHostedService>();

        var jwtKey = configuration["Jwt:Key"] ?? "NizamDevSigningKey_ChangeMe_32chars!";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "nizam",
                    ValidAudience = configuration["Jwt:Audience"] ?? "nizam-api",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });
        services.AddAuthorization();

        return services;
    }
}
