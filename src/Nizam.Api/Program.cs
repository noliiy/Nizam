using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Nizam.Application;
using Nizam.Application.Abstractions;
using Nizam.Application.Activities;
using Nizam.Application.Baselines;
using Nizam.Application.Common;
using Nizam.Application.Organizations;
using Nizam.Application.Progress;
using Nizam.Application.Projects;
using Nizam.Application.Relationships;
using Nizam.Application.Scheduling;
using Nizam.Application.Wbs;
using Nizam.Domain.Enums;
using Nizam.Infrastructure;
using Nizam.Infrastructure.Persistence;
using Nizam.Infrastructure.Persistence.Seed;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((_, cfg) => cfg.WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddProblemDetails();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nizam API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Bearer token",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(opt =>
    {
        opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async context =>
        {
            var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var ex = feature?.Error;
            var (status, title, errors) = ex switch
            {
                FluentValidation.ValidationException ve => (
                    StatusCodes.Status400BadRequest,
                    "Doğrulama hatası",
                    ve.Errors.Select(e => e.ErrorMessage).ToArray()),
                Nizam.Application.Common.ValidationException ave => (
                    StatusCodes.Status400BadRequest,
                    "Doğrulama hatası",
                    ave.Errors.ToArray()),
                NotFoundException nf => (StatusCodes.Status404NotFound, nf.Message, Array.Empty<string>()),
                ForbiddenException fb => (StatusCodes.Status403Forbidden, fb.Message, Array.Empty<string>()),
                ConflictException cf => (StatusCodes.Status409Conflict, cf.Message, Array.Empty<string>()),
                Nizam.Domain.Exceptions.DomainException de => (
                    StatusCodes.Status400BadRequest,
                    de.Message,
                    Array.Empty<string>()),
                _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.", Array.Empty<string>())
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = errors.Length > 0 ? string.Join(" ", errors) : title,
                Extensions = { ["errors"] = errors }
            });
        });
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    try
        {
            await DatabaseSeeder.SeedAsync(app.Services);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Veritabanı seed atlandı veya başarısız oldu.");
        }

    MapAuthEndpoints(app);
    MapOrganizationEndpoints(app);
    MapProjectEndpoints(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API başlatılamadı");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void MapAuthEndpoints(WebApplication app)
{
    app.MapPost("/api/auth/login", async (
        LoginRequest request,
        NizamDbContext db,
        IJwtTokenService jwt) =>
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { title = "E-posta ve şifre zorunludur." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant()
            || u.Email == request.Email.Trim());
        if (user is null || !string.Equals(user.PasswordHash, DatabaseSeeder.HashPassword(request.Password), StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        var membership = await db.OrganizationMembers
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.OrganizationId)
            .FirstOrDefaultAsync();

        var token = jwt.CreateToken(user.Id, user.Email, membership?.OrganizationId);
        return Results.Ok(new LoginResponse(token, user.Id, user.Email, user.DisplayName, membership?.OrganizationId));
    })
    .AllowAnonymous()
    .WithTags("Auth");
}

static void MapOrganizationEndpoints(WebApplication app)
{
    app.MapPost("/api/organizations", async (CreateOrganizationCommand cmd, IMediator mediator) =>
    {
        var result = await mediator.Send(cmd);
        return Results.Created($"/api/organizations/{result.Id}", result);
    })
    .RequireAuthorization()
    .WithTags("Organizations");
}

static void MapProjectEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/projects").RequireAuthorization().WithTags("Projects");

    group.MapGet("/", async ([FromQuery] Guid organizationId, IMediator mediator) =>
        Results.Ok(await mediator.Send(new ListProjectsQuery(organizationId))));

    group.MapPost("/", async (CreateProjectCommand cmd, IMediator mediator) =>
    {
        var result = await mediator.Send(cmd);
        return Results.Created($"/api/projects/{result.Id}", result);
    });

    group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        Results.Ok(await mediator.Send(new GetProjectQuery(id))));

    group.MapGet("/{id:guid}/wbs", async (Guid id, IMediator mediator) =>
        Results.Ok(await mediator.Send(new ListWbsQuery(id))));

    group.MapPost("/{id:guid}/wbs", async (Guid id, CreateWbsBody body, IMediator mediator) =>
    {
        var result = await mediator.Send(new CreateWbsNodeCommand(
            id, body.ParentWbsId, body.WbsCode, body.Name, body.Description, body.SortOrder));
        return Results.Created($"/api/projects/{id}/wbs/{result.Id}", result);
    });

    group.MapGet("/{id:guid}/activities", async (Guid id, IMediator mediator) =>
        Results.Ok(await mediator.Send(new ListActivitiesQuery(id))));

    group.MapPost("/{id:guid}/activities", async (Guid id, CreateActivityBody body, IMediator mediator) =>
    {
        var result = await mediator.Send(new CreateActivityCommand(
            id, body.WbsId, body.ActivityCode, body.Name, body.Description,
            body.OriginalDurationMinutes, body.ActivityType));
        return Results.Created($"/api/projects/{id}/activities/{result.Id}", result);
    });

    group.MapPut("/{id:guid}/activities/{activityId:guid}", async (
        Guid id, Guid activityId, UpdateActivityBody body, IMediator mediator) =>
    {
        var result = await mediator.Send(new UpdateActivityCommand(
            id, activityId, body.Name, body.Description,
            body.OriginalDurationMinutes, body.RemainingDurationMinutes, body.WbsId));
        return Results.Ok(result);
    });

    group.MapPost("/{id:guid}/relationships", async (Guid id, CreateRelationshipBody body, IMediator mediator) =>
    {
        var result = await mediator.Send(new CreateRelationshipCommand(
            id, body.PredecessorActivityId, body.SuccessorActivityId, body.RelationshipType, body.LagMinutes));
        return Results.Created($"/api/projects/{id}/relationships/{result.Id}", result);
    });

    group.MapDelete("/{id:guid}/relationships/{relationshipId:guid}", async (
        Guid id, Guid relationshipId, IMediator mediator) =>
    {
        await mediator.Send(new DeleteRelationshipCommand(id, relationshipId));
        return Results.NoContent();
    });

    group.MapPost("/{id:guid}/schedule/run", async (Guid id, IMediator mediator) =>
        Results.Ok(await mediator.Send(new RunScheduleCommand(id))));

    group.MapPut("/{id:guid}/data-date", async (Guid id, SetDataDateBody body, IMediator mediator) =>
        Results.Ok(await mediator.Send(new SetDataDateCommand(id, body.DataDate))));

    group.MapPost("/{id:guid}/progress", async (Guid id, CreateProgressBody body, IMediator mediator) =>
    {
        var result = await mediator.Send(new CreateProgressUpdateCommand(
            id, body.ActivityId, body.ActualStart, body.ActualFinish,
            body.PercentComplete, body.RemainingDurationMinutes, body.Notes));
        return Results.Created($"/api/projects/{id}/progress/{result.Id}", result);
    });

    group.MapPost("/{id:guid}/baselines", async (Guid id, CreateBaselineBody body, IMediator mediator) =>
    {
        var result = await mediator.Send(new CreateBaselineCommand(id, body.Name, body.BaselineType));
        return Results.Created($"/api/projects/{id}/baselines/{result.Id}", result);
    });

    group.MapGet("/{id:guid}/baselines/{baselineId:guid}/compare", async (
        Guid id, Guid baselineId, IMediator mediator) =>
        Results.Ok(await mediator.Send(new CompareBaselineQuery(id, baselineId))));

    group.MapGet("/{id:guid}/dashboard", async (Guid id, IMediator mediator) =>
        Results.Ok(await mediator.Send(new GetProjectDashboardQuery(id))));
}

public partial class Program;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, Guid UserId, string Email, string DisplayName, Guid? OrganizationId);
public sealed record CreateWbsBody(Guid? ParentWbsId, string WbsCode, string Name, string? Description, int SortOrder = 0);
public sealed record CreateActivityBody(Guid WbsId, string ActivityCode, string Name, string? Description, int OriginalDurationMinutes, ActivityType ActivityType = ActivityType.TaskDependent);
public sealed record UpdateActivityBody(string Name, string? Description, int OriginalDurationMinutes, int RemainingDurationMinutes, Guid? WbsId = null);
public sealed record CreateRelationshipBody(Guid PredecessorActivityId, Guid SuccessorActivityId, RelationshipType RelationshipType, int LagMinutes = 0);
public sealed record SetDataDateBody(DateTime DataDate);
public sealed record CreateProgressBody(Guid ActivityId, DateTime? ActualStart, DateTime? ActualFinish, decimal PercentComplete, int? RemainingDurationMinutes, string? Notes);
public sealed record CreateBaselineBody(string Name, BaselineType BaselineType = BaselineType.Approved);
