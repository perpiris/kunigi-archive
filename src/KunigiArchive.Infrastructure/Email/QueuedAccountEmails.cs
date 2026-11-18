using KunigiArchive.Application.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Infrastructure.Email;

// Builds the account emails and queues them: the request returns at once and EmailSendingService sends them,
// with retries (#20). Returning at once also means a password reset for a known email takes as long as one for
// an unknown email, so the timing no longer tells which addresses have accounts.
internal sealed class QueuedAccountEmails : IAccountEmails
{
    private readonly EmailQueue _queue;
    private readonly EmailOptions _options;
    private readonly ILogger<QueuedAccountEmails> _logger;

    public QueuedAccountEmails(EmailQueue queue, IOptions<EmailOptions> options, ILogger<QueuedAccountEmails> logger)
    {
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public Task SendAccountCreatedAsync(string email, string temporaryPassword, CancellationToken cancellationToken)
    {
        (string subject, string body) = AccountEmailTemplates.AccountCreated(temporaryPassword, LoginUrl(), SiteUrl());
        Enqueue("AccountCreated", email, subject, body);
        return Task.CompletedTask;
    }

    public Task SendTemporaryPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken)
    {
        (string subject, string body) = AccountEmailTemplates.TemporaryPassword(temporaryPassword, LoginUrl(), SiteUrl());
        Enqueue("TemporaryPassword", email, subject, body);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string code, CancellationToken cancellationToken)
    {
        // Escaped: emails can contain "+" and other characters that break a query string.
        string resetUrl = $"{SiteUrl()}/auth/reset-password?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(code)}";
        (string subject, string body) = AccountEmailTemplates.PasswordReset(resetUrl, SiteUrl());
        Enqueue("PasswordReset", email, subject, body);
        return Task.CompletedTask;
    }

    private void Enqueue(string kind, string to, string subject, string body)
    {
        QueuedEmail email = new QueuedEmail(Guid.NewGuid(), kind, to, subject, body);
        _queue.Enqueue(email);
        _logger.LogInformation("{EmailKind} email {EmailId} queued", kind, email.Id);
    }

    private string SiteUrl()
    {
        return _options.ApplicationUrl.TrimEnd('/');
    }

    private string LoginUrl()
    {
        return SiteUrl() + "/auth/login";
    }
}
