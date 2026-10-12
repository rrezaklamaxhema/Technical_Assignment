namespace Claims.Auditing;

public interface IAuditQueue
{
    void Enqueue(AuditEvent auditEvent);

    IAsyncEnumerable<AuditEvent> ReadAllAsync(
        CancellationToken cancellationToken);
}
