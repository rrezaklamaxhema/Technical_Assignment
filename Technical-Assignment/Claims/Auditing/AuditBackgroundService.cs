using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Auditing;

public sealed class AuditBackgroundService : BackgroundService
{
    private readonly IAuditQueue _auditQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditBackgroundService> _logger;

    public AuditBackgroundService(
        IAuditQueue auditQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditBackgroundService> logger)
    {
        _auditQueue = auditQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            await foreach (
                var auditEvent in _auditQueue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await SaveAuditAsync(auditEvent, stoppingToken);

                    _logger.LogInformation(
                        "Saved audit: {EntityType} {EntityId} {Operation}",
                        auditEvent.EntityType,
                        auditEvent.EntityId,
                        auditEvent.HttpRequestType);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to save audit: {EntityType} {EntityId} " +
                        "{Operation}. This event will not be retried.",
                        auditEvent.EntityType,
                        auditEvent.EntityId,
                        auditEvent.HttpRequestType);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal cancellation when the application stops.
        }
    }

    private async Task SaveAuditAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AuditContext>();

        switch (auditEvent.EntityType)
        {
            case AuditEntityType.Claim:
                context.ClaimAudits.Add(new ClaimAudit
                {
                    ClaimId = auditEvent.EntityId,
                    HttpRequestType = auditEvent.HttpRequestType,
                    Created = auditEvent.Created
                });
                break;

            case AuditEntityType.Cover:
                context.CoverAudits.Add(new CoverAudit
                {
                    CoverId = auditEvent.EntityId,
                    HttpRequestType = auditEvent.HttpRequestType,
                    Created = auditEvent.Created
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported audit entity: {auditEvent.EntityType}");
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
