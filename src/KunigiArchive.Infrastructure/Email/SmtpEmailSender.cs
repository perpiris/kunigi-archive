using System.Net;
using System.Net.Mail;
using KunigiArchive.Application.Users;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Infrastructure.Email;

// Delivers one message over SMTP (System.Net.Mail), or into PickupDirectory in development.
// Called by EmailSendingService, outside any request.
internal sealed class SmtpEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    // Throws EmailDeliveryException when the email can't be sent.
    public async Task SendAsync(QueuedEmail email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PickupDirectory) && string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new EmailDeliveryException($"{EmailOptions.SectionName}:Host is not configured.");
        }

        using MailMessage message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = email.Subject,
            Body = email.Body,
            IsBodyHtml = true
        };
        message.To.Add(email.To);

        // SmtpClient isn't thread-safe, so one per message.
        using SmtpClient client = CreateClient();
        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or IOException)
        {
            throw new EmailDeliveryException("The SMTP server did not accept the email.", exception);
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
