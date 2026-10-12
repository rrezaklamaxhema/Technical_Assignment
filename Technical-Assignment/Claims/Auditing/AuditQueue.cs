using System.Threading.Channels;

namespace Claims.Auditing;

public sealed class AuditQueue : IAuditQueue
{
    private readonly Channel<AuditEvent> _channel =
        Channel.CreateUnbounded<AuditEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

    public void Enqueue(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (!_channel.Writer.TryWrite(auditEvent))
        {
            throw new InvalidOperationException(
                "The audit event could not be queued.");
        }
    }

    public IAsyncEnumerable<AuditEvent> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
