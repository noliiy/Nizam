using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nizam.Desktop.Models;

namespace Nizam.Desktop.Services;

public sealed class ApiClient : INizamApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly AuthenticationService _auth;

    public ApiClient(HttpClient http, AuthenticationService auth)
    {
        _http = http;
        _auth = auth;
    }

    private void ApplyAuth()
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(_auth.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    public async Task<LoginResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = email, Password = password },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new ApiClientException("Giriş başarısız. E-posta veya şifre hatalı.");

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions, cancellationToken)
            ?? throw new ApiClientException("Giriş yanıtı okunamadı.");

        _auth.SetSession(body.Token, body.UserId, body.Email, body.DisplayName, body.OrganizationId);
        ApplyAuth();
        return body;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var list = await _http.GetFromJsonAsync<List<ProjectDto>>(
            $"/api/projects?organizationId={organizationId}", JsonOptions, cancellationToken);
        return list ?? new List<ProjectDto>();
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsJsonAsync("/api/projects", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "Proje oluşturulamadı.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var list = await _http.GetFromJsonAsync<List<ActivityDto>>(
            $"/api/projects/{projectId}/activities", JsonOptions, cancellationToken);
        return list ?? new List<ActivityDto>();
    }

    public async Task<ActivityDto> CreateActivityAsync(Guid projectId, CreateActivityRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsJsonAsync($"/api/projects/{projectId}/activities", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "Aktivite oluşturulamadı.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ActivityDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<ActivityDto> UpdateActivityAsync(Guid projectId, Guid activityId, UpdateActivityRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PutAsJsonAsync(
            $"/api/projects/{projectId}/activities/{activityId}", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "Aktivite güncellenemedi.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ActivityDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<RelationshipDto> CreateRelationshipAsync(Guid projectId, CreateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsJsonAsync(
            $"/api/projects/{projectId}/relationships", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "İlişki oluşturulamadı.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<RelationshipDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<ScheduleRunDto> RunScheduleAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsync($"/api/projects/{projectId}/schedule/run", null, cancellationToken);
        await EnsureSuccessAsync(response, "Zamanlama çalıştırılamadı.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ScheduleRunDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<ProjectDto> SetDataDateAsync(Guid projectId, DateTime dataDate, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PutAsJsonAsync(
            $"/api/projects/{projectId}/data-date",
            new SetDataDateRequest { DataDate = dataDate },
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, "Veri tarihi güncellenemedi.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<ProgressUpdateDto> CreateProgressAsync(Guid projectId, CreateProgressRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsJsonAsync($"/api/projects/{projectId}/progress", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "İlerleme kaydedilemedi.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<ProgressUpdateDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<BaselineDto> CreateBaselineAsync(Guid projectId, CreateBaselineRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsJsonAsync($"/api/projects/{projectId}/baselines", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "Baseline oluşturulamadı.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<BaselineDto>(JsonOptions, cancellationToken))!;
    }

    public async Task<BaselineCompareDto> CompareBaselineAsync(Guid projectId, Guid baselineId, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var result = await _http.GetFromJsonAsync<BaselineCompareDto>(
            $"/api/projects/{projectId}/baselines/{baselineId}/compare", JsonOptions, cancellationToken);
        return result ?? throw new ApiClientException("Baseline karşılaştırması okunamadı.");
    }

    public async Task<ProjectDashboardDto> GetDashboardAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var result = await _http.GetFromJsonAsync<ProjectDashboardDto>(
            $"/api/projects/{projectId}/dashboard", JsonOptions, cancellationToken);
        return result ?? throw new ApiClientException("Gösterge paneli okunamadı.");
    }

    public async Task<IReadOnlyList<WbsNodeDto>> GetWbsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var list = await _http.GetFromJsonAsync<List<WbsNodeDto>>(
            $"/api/projects/{projectId}/wbs", JsonOptions, cancellationToken);
        return list ?? new List<WbsNodeDto>();
    }

    public async Task<WbsNodeDto> CreateWbsAsync(Guid projectId, CreateWbsRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuth();
        var response = await _http.PostAsJsonAsync($"/api/projects/{projectId}/wbs", request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, "WBS oluşturulamadı.", cancellationToken);
        return (await response.Content.ReadFromJsonAsync<WbsNodeDto>(JsonOptions, cancellationToken))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallback, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await response.Content.ReadAsStringAsync(ct);
        throw new ApiClientException(string.IsNullOrWhiteSpace(detail) ? fallback : $"{fallback} {detail}");
    }
}

public sealed class ApiClientException : Exception
{
    public ApiClientException(string message) : base(message) { }
}
