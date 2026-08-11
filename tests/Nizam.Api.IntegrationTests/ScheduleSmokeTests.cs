using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nizam.Application.Abstractions;
using Nizam.Infrastructure.Persistence;
using Nizam.Infrastructure.Persistence.Seed;

namespace Nizam.Api.IntegrationTests;

public sealed class NizamApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"NizamTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:InMemoryName", _dbName);
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");
        builder.UseSetting("Jwt:Key", "NizamDevSigningKey_ChangeMe_AtLeast32!");
        builder.UseSetting("Jwt:Issuer", "nizam");
        builder.UseSetting("Jwt:Audience", "nizam-api");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<NizamDbContext>>();
            services.RemoveAll<NizamDbContext>();
            services.RemoveAll<IApplicationDbContext>();

            services.AddDbContext<NizamDbContext>(opt => opt.UseInMemoryDatabase(_dbName));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NizamDbContext>());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        // Ensure seed after host is built
        using var scope = Services.CreateScope();
        DatabaseSeeder.SeedAsync(Services).GetAwaiter().GetResult();
        base.ConfigureClient(client);
    }
}

public class ScheduleSmokeTests : IClassFixture<NizamApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ScheduleSmokeTests(NizamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_CreateProject_Activities_Relationship_RunSchedule_SetsEarlyDates()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@nizam.local", password = "Admin123!" });
        login.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = loginBody.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgResp = await _client.PostAsJsonAsync("/api/organizations", new { name = "Test Org" });
        orgResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created, await orgResp.Content.ReadAsStringAsync());
        var org = await orgResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var orgId = org.GetProperty("id").GetGuid();

        var projectResp = await _client.PostAsJsonAsync("/api/projects", new
        {
            organizationId = orgId,
            projectCode = "P-001",
            name = "Test Project",
            description = "Smoke"
        });
        projectResp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created, await projectResp.Content.ReadAsStringAsync());
        var project = await projectResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var projectId = project.GetProperty("id").GetGuid();

        var wbsResp = await _client.GetAsync($"/api/projects/{projectId}/wbs");
        wbsResp.EnsureSuccessStatusCode();
        var wbsList = await wbsResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var wbsId = wbsList.EnumerateArray().First().GetProperty("id").GetGuid();

        var aResp = await _client.PostAsJsonAsync($"/api/projects/{projectId}/activities", new
        {
            wbsId,
            activityCode = "A1000",
            name = "Activity A",
            originalDurationMinutes = 960
        });
        aResp.EnsureSuccessStatusCode();
        var actA = await aResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var aId = actA.GetProperty("id").GetGuid();

        var bResp = await _client.PostAsJsonAsync($"/api/projects/{projectId}/activities", new
        {
            wbsId,
            activityCode = "A2000",
            name = "Activity B",
            originalDurationMinutes = 480
        });
        bResp.EnsureSuccessStatusCode();
        var actB = await bResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var bId = actB.GetProperty("id").GetGuid();

        var relResp = await _client.PostAsJsonAsync($"/api/projects/{projectId}/relationships", new
        {
            predecessorActivityId = aId,
            successorActivityId = bId,
            relationshipType = 0,
            lagMinutes = 0
        });
        relResp.EnsureSuccessStatusCode();

        var scheduleResp = await _client.PostAsync($"/api/projects/{projectId}/schedule/run", null);
        scheduleResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, await scheduleResp.Content.ReadAsStringAsync());
        var schedule = await scheduleResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        schedule.GetProperty("status").GetInt32().Should().Be(2); // Completed

        var activitiesResp = await _client.GetAsync($"/api/projects/{projectId}/activities");
        activitiesResp.EnsureSuccessStatusCode();
        var activities = await activitiesResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        foreach (var activity in activities.EnumerateArray())
        {
            activity.TryGetProperty("earlyStart", out var es).Should().BeTrue();
            es.ValueKind.Should().NotBe(JsonValueKind.Null);
            activity.TryGetProperty("earlyFinish", out var ef).Should().BeTrue();
            ef.ValueKind.Should().NotBe(JsonValueKind.Null);
        }
    }
}
