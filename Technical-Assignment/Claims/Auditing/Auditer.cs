namespace Claims.Auditing;

public class Auditer : IAuditer
{
    private readonly IAuditQueue _auditQueue;

    public Auditer(IAuditQueue auditQueue)
    {
        _auditQueue = auditQueue;
    }

    public void AuditClaim(string id, string httpRequestType)
    {
        _auditQueue.Enqueue(new AuditEvent(
            AuditEntityType.Claim,
            id,
            httpRequestType,
            DateTime.UtcNow));
    }

    public void AuditCover(string id, string httpRequestType)
    {
        _auditQueue.Enqueue(new AuditEvent(
            AuditEntityType.Cover,
            id,
            httpRequestType,
            DateTime.UtcNow));
    }
}
