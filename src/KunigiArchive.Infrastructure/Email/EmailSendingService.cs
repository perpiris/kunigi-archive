using KunigiArchive.Application.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Infrastructure.Email;

// Sends the queued account emails one at a time (#20). A failed send is tried again after 10 seconds, then 1 and
// 5 minutes, without holding up the emails behind it; after the last try it's logged as an error.
internal sealed class EmailSendingService : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)];

    private readonly EmailQueue _queue;
    private readonly SmtpEmailSender _sender;
    private readonly ILogger<EmailSendingService> _logger;

    public EmailSendingService(EmailQueue queue, SmtpEmailSender sender, ILogger<EmailSendingService> logger)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (QueuedEmail email in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await SendAsync(email, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is stopping.
        }

        // One last try for whatever is still waiting (a deploy), without retries.
        while (_queue.Reader.TryRead(out QueuedEmail? email))
        {
            await SendAsync(email with { Attempt = RetryDelays.Length }, CancellationToken.None);
        }
    }

    private async Task SendAsync(QueuedEmail email, CancellationToken stoppingToken)
    {
        try
        {
            await _sender.SendAsync(email, stoppingToken);
            _logger.LogInformation("{EmailKind} email {EmailId} sent", email.Kind, email.Id);
        }
        // Everything is caught: an exception leaving a BackgroundService stops the whole app.
        catch (Exception exception) when (exception is not OperationCanceledException && email.Attempt < RetryDelays.Length)
        {
            TimeSpan delay = RetryDelays[email.Attempt];
            _logger.LogWarning(exception, "{EmailKind} email {EmailId} failed (attempt {Attempt}), trying again in {Delay}",
                email.Kind, email.Id, email.Attempt + 1, delay);
            _ = RetryLaterAsync(email with { Attempt = email.Attempt + 1 }, delay, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "{EmailKind} email {EmailId} could not be sent after {Attempts} attempts",
                email.Kind, email.Id, email.Attempt + 1);
        }
        catch (OperationCanceledException)
        {
            // Stopped in the middle of a send: back in the queue for the last try on shutdown.
            _queue.Enqueue(email);
        }
    }

    private async Task RetryLaterAsync(QueuedEmail email, TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Stopping: back in the queue now, so the last try on shutdown picks it up.
        }

        _queue.Enqueue(email);
    }
}
