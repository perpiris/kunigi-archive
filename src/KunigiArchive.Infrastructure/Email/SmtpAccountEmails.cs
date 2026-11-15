using System.Net;
using System.Net.Mail;
using KunigiArchive.Application.Users;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Infrastructure.Email;

// Same delivery as the old app's EmailService (System.Net.Mail over SMTP), sent within the request.
internal sealed class SmtpAccountEmails : IAccountEmails
{
    private readonly EmailOptions _options;

    public SmtpAccountEmails(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAccountCreatedAsync(string email, string temporaryPassword, CancellationToken cancellationToken)
    {
        (string subject, string body) = AccountEmailTemplates.AccountCreated(temporaryPassword, LoginUrl());
        await SendAsync(email, subject, body, cancellationToken);
    }

    public async Task SendTemporaryPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken)
    {
        (string subject, string body) = AccountEmailTemplates.TemporaryPassword(temporaryPassword, LoginUrl());
        await SendAsync(email, subject, body, cancellationToken);
    }

    private string LoginUrl()
    {
        return _options.ApplicationUrl.TrimEnd('/') + "/auth/login";
    }

    private async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PickupDirectory) && string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new EmailDeliveryException($"{EmailOptions.SectionName}:Host is not configured.");
        }

        using MailMessage message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        // SmtpClient isn't thread-safe, so one per message.
        using SmtpClient client = CreateClient();
        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or IOException)
        {
            throw new EmailDeliveryException($"The email to {to} could not be sent.", exception);
        }
    }

    private SmtpClient CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_options.PickupDirectory))
        {
            Directory.CreateDirectory(_options.PickupDirectory);
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = _options.PickupDirectory
            };
        }

        return new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl = _options.EnableSsl
        };
    }
}
