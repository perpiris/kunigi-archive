using System.Threading.Channels;

namespace KunigiArchive.Infrastructure.Email;

// In memory, between the requests that queue account emails and EmailSendingService (#20). An email still
// waiting when the app stops gets one last try (EmailSendingService); if that fails it's lost, and the admin can
// send a new temporary password or the user can ask for another reset link.
internal sealed class EmailQueue
{
    private readonly Channel<QueuedEmail> _channel = Channel.CreateUnbounded<QueuedEmail>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<QueuedEmail> Reader => _channel.Reader;

    public void Enqueue(QueuedEmail email)
    {
        // Unbounded and never completed, so this always succeeds.
        _channel.Writer.TryWrite(email);
    }
}
