using System.Net;
using System.Net.Mail;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Settings;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Web.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;
    private readonly SmtpClient _client;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.EnableSsl
        };
    }

    public async Task<ServiceResponse> SendAccountCreationEmailAsync(string toEmail, string password)
    {
        try
        {
            var (subject, body) = GetAccountCreatedEmail(password);
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await _client.SendMailAsync(mailMessage);

            return ServiceResponse.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email of account creation to {ToEmail}", toEmail);
            return ServiceResponse.Failure();
        }
    }

    public async Task<ServiceResponse> SendPasswordResetEmailAsync(string toEmail, string resetCode)
    {
        try
        {
            var (subject, body) = GetPasswordResetEmail(toEmail, resetCode);
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await _client.SendMailAsync(mailMessage);

            return ServiceResponse.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {ToEmail}", toEmail);
            return ServiceResponse.Failure();
        }
    }

    private (string subject, string body) GetAccountCreatedEmail(string password)
    {
        const string subject = "Καλώς ήρθατε - Ο λογαριασμός σας δημιουργήθηκε";
        var loginUrl = _settings.ApplicationUrl + "/auth/login";

        var body = $$"""
                     <html>
                     <head>
                     	<meta charset="UTF-8">
                     	<style>
                     		@import url('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;700&display=swap');
                     	</style>
                     </head>
                     <body style="margin: 0; padding: 0; font-family: 'Space Grotesk', 'Helvetica Neue', Arial, sans-serif; background-color: #fafafa;">
                     	<table width="100%" cellpadding="0" cellspacing="0" style="background-color: #fafafa; padding: 40px 20px;">
                     		<tr>
                     			<td align="center">
                     				<table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border: 4px solid #000000; box-shadow: 8px 8px 0px #000000;">
                     					<!-- Header -->
                     					<tr>
                     						<td style="background-color: #ff4d00; border-bottom: 4px solid #000000; padding: 40px 30px; text-align: left;">
                     							<h1 style="margin: 0; font-size: 32px; font-weight: 700; color: #000000; letter-spacing: -0.5px; text-transform: uppercase;">Ο ΛΟΓΑΡΙΑΣΜΟΣ<br/>ΣΑΣ ΔΗΜΙΟΥΡΓΗΘΗΚΕ</h1>
                     						</td>
                     					</tr>
                     					
                     					<!-- Content -->
                     					<tr>
                     						<td style="padding: 40px 30px; color: #000000; line-height: 1.6;">
                     							<p style="margin: 0 0 24px 0; font-size: 16px; font-weight: 400;">Αγαπητέ χρήστη,</p>
                     							<p style="margin: 0 0 32px 0; font-size: 16px; font-weight: 400;">Χαιρόμαστε να σας ενημερώσουμε ότι ο λογαριασμός σας δημιουργήθηκε επιτυχώς από τον διαχειριστή του συστήματος.</p>
                     							
                     							<!-- Password Box -->
                     							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 32px 0;">
                     								<tr>
                     									<td style="background-color: #fff8f3; border: 4px solid #000000; padding: 24px; box-shadow: 4px 4px 0px #000000;">
                     										<p style="margin: 0 0 12px 0; font-size: 11px; color: #000000; text-transform: uppercase; font-weight: 700; letter-spacing: 1px;">Ο ΠΡΟΣΩΡΙΝΟΣ ΣΑΣ ΚΩΔΙΚΟΣ</p>
                     										<p style="margin: 0; font-family: 'Courier New', monospace; font-size: 28px; color: #ff4d00; font-weight: 700; letter-spacing: 2px;">{{password}}</p>
                     									</td>
                     								</tr>
                     							</table>

                     							<!-- Warning Box -->
                     							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 32px 0;">
                     								<tr>
                     									<td style="background-color: #fffacd; border: 4px solid #000000; padding: 20px;">
                     										<p style="margin: 0; font-size: 14px; font-weight: 700; color: #000000;">⚠ ΣΗΜΑΝΤΙΚΟ</p>
                     										<p style="margin: 8px 0 0 0; font-size: 14px; color: #000000;">Συνιστούμε να αλλάξετε τον κωδικό σας μετά την πρώτη σύνδεση για λόγους ασφαλείας.</p>
                     									</td>
                     								</tr>
                     							</table>
                     							
                     							<!-- CTA Button -->
                     							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 40px 0 0 0;">
                     								<tr>
                     									<td align="left">
                     										<a target="_blank" href="{{loginUrl}}" style="display: inline-block; background-color: #ff4d00; color: #000000; padding: 18px 36px; text-decoration: none; font-weight: 700; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; border: 4px solid #000000; box-shadow: 4px 4px 0px #000000; transition: all 0.2s;">ΣΥΝΔΕΣΗ ΣΤΟ ΣΥΣΤΗΜΑ →</a>
                     									</td>
                     								</tr>
                     							</table>
                     						</td>
                     					</tr>
                     					
                     					<!-- Footer -->
                     					<tr>
                     						<td style="background-color: #000000; padding: 24px 30px; text-align: left; border-top: 4px solid #000000;">
                     							<p style="margin: 0 0 8px 0; font-size: 16px; color: #ff4d00; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;">ΤΟ ΚΥΝΗΓΙ</p>
                     							<p style="margin: 0; font-size: 12px; color: #ffffff;">Αυτό το email στάλθηκε αυτόματα - παρακαλούμε μην απαντήσετε.</p>
                     						</td>
                     					</tr>
                     				</table>
                     			</td>
                     		</tr>
                     	</table>
                     </body>
                     </html>
                     """;

        return (subject, body);
    }

    private (string subject, string body) GetPasswordResetEmail(string email, string resetCode)
    {
        const string subject = "Επαναφορά κωδικού πρόσβασης";
        var resetUrl = _settings.ApplicationUrl + $"/auth/reset-password?code={resetCode}&email={email}";

        var body = $$"""
                     <html>
                     <head>
                     	<meta charset="UTF-8">
                     	<style>
                     		@import url('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;700&display=swap');
                     	</style>
                     </head>
                     <body style="margin: 0; padding: 0; font-family: 'Space Grotesk', 'Helvetica Neue', Arial, sans-serif; background-color: #fafafa;">
                     	<table width="100%" cellpadding="0" cellspacing="0" style="background-color: #fafafa; padding: 40px 20px;">
                     		<tr>
                     			<td align="center">
                     				<table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border: 4px solid #000000; box-shadow: 8px 8px 0px #000000;">
                     					<!-- Header -->
                     					<tr>
                     						<td style="background-color: #ff4d00; border-bottom: 4px solid #000000; padding: 40px 30px; text-align: left;">
                     							<h1 style="margin: 0; font-size: 32px; font-weight: 700; color: #000000; letter-spacing: -0.5px; text-transform: uppercase;">ΕΠΑΝΑΦΟΡΑ<br/>ΚΩΔΙΚΟΥ ΠΡΟΣΒΑΣΗΣ</h1>
                     						</td>
                     					</tr>
                     					
                     					<!-- Content -->
                     					<tr>
                     						<td style="padding: 40px 30px; color: #000000; line-height: 1.6;">
                     							<p style="margin: 0 0 24px 0; font-size: 16px; font-weight: 400;">Αγαπητέ χρήστη,</p>
                     							<p style="margin: 0 0 24px 0; font-size: 16px; font-weight: 400;">Λάβαμε αίτημα για επαναφορά του κωδικού πρόσβασης του λογαριασμού σας.</p>
                     							
                     							<p style="margin: 0 0 32px 0; font-size: 16px; font-weight: 400;">Για να ορίσετε έναν νέο κωδικό πρόσβασης, κάντε κλικ στο παρακάτω κουμπί:</p>
                     							
                     							<!-- CTA Button -->
                     							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 0 0 32px 0;">
                     								<tr>
                     									<td align="left">
                     										<a target="_blank" href="{{resetUrl}}" style="display: inline-block; background-color: #ff4d00; color: #000000; padding: 18px 36px; text-decoration: none; font-weight: 700; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; border: 4px solid #000000; box-shadow: 4px 4px 0px #000000; transition: all 0.2s;">ΕΠΑΝΑΦΟΡΑ ΚΩΔΙΚΟΥ →</a>
                     									</td>
                     								</tr>
                     							</table>

                     							<!-- Info Box -->
                     							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 32px 0;">
                     								<tr>
                     									<td style="background-color: #e8f4ff; border: 4px solid #000000; padding: 20px;">
                     										<p style="margin: 0; font-size: 14px; font-weight: 700; color: #000000;">⏱ ΧΡΟΝΙΚΟ ΟΡΙΟ</p>
                     										<p style="margin: 8px 0 0 0; font-size: 14px; color: #000000;">Ο σύνδεσμος θα παραμείνει ενεργός για 1 ώρα.</p>
                     									</td>
                     								</tr>
                     							</table>

                     							<!-- Warning Box -->
                     							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 32px 0;">
                     								<tr>
                     									<td style="background-color: #fffacd; border: 4px solid #000000; padding: 20px;">
                     										<p style="margin: 0; font-size: 14px; font-weight: 700; color: #000000;">⚠ ΔΕΝ ΖΗΤΗΣΑΤΕ ΑΥΤΗ ΤΗΝ ΑΛΛΑΓΗ;</p>
                     										<p style="margin: 8px 0 0 0; font-size: 14px; color: #000000;">Αν δεν ζητήσατε εσείς την επαναφορά κωδικού, παρακαλούμε αγνοήστε αυτό το email. Ο λογαριασμός σας παραμένει ασφαλής.</p>
                     									</td>
                     								</tr>
                     							</table>
                     						</td>
                     					</tr>
                     					
                     					<!-- Footer -->
                     					<tr>
                     						<td style="background-color: #000000; padding: 24px 30px; text-align: left; border-top: 4px solid #000000;">
                     							<p style="margin: 0 0 8px 0; font-size: 16px; color: #ff4d00; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;">ΤΟ ΚΥΝΗΓΙ</p>
                     							<p style="margin: 0; font-size: 12px; color: #ffffff;">Αυτό το email στάλθηκε αυτόματα - παρακαλούμε μην απαντήσετε.</p>
                     						</td>
                     					</tr>
                     				</table>
                     			</td>
                     		</tr>
                     	</table>
                     </body>
                     </html>
                     """;

        return (subject, body);
    }
}