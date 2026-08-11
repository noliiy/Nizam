using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nizam.Infrastructure.Persistence;

namespace Nizam.Infrastructure.Outbox;

/// <summary>
/// Polls unprocessed outbox rows, logs them, and optionally POSTs payload to n8n webhook.
/// </summary>
public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OutboxDispatcherHostedService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox işleme hatası.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NizamDbContext>();
        var webhookUrl = _configuration["Outbox:WebhookUrl"];

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        _logger.LogInformation("İşlenmemiş outbox mesajı: {Count}", pending.Count);

        foreach (var message in pending)
        {
            try
            {
                _logger.LogInformation(
                    "Outbox: {Type} Id={Id} CreatedAt={CreatedAt}",
                    message.Type, message.Id, message.CreatedAt);

                if (!string.IsNullOrWhiteSpace(webhookUrl))
                {
                    var client = _httpClientFactory.CreateClient("OutboxWebhook");
                    using var content = new StringContent(message.Payload, System.Text.Encoding.UTF8, "application/json");
                    content.Headers.TryAddWithoutValidation("X-Nizam-Event-Type", message.Type);
                    var response = await client.PostAsync(webhookUrl, content, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        message.Error = $"Webhook HTTP {(int)response.StatusCode}";
                        _logger.LogWarning(
                            "Outbox webhook başarısız: {Type} {Status}",
                            message.Type, response.StatusCode);
                        continue;
                    }
                }

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Outbox mesajı işlenemedi: {Id}", message.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
