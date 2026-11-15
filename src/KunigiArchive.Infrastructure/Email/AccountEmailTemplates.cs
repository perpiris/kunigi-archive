using System.Net;

namespace KunigiArchive.Infrastructure.Email;

// The old app's account email (EmailService.GetAccountCreatedEmail), shared by both account emails.
internal static class AccountEmailTemplates
{
    public static (string Subject, string Body) AccountCreated(string temporaryPassword, string loginUrl)
    {
        return (
            "Καλώς ήρθατε - Ο λογαριασμός σας δημιουργήθηκε",
            Render(
                "Ο ΛΟΓΑΡΙΑΣΜΟΣ<br/>ΣΑΣ ΔΗΜΙΟΥΡΓΗΘΗΚΕ",
                "Χαιρόμαστε να σας ενημερώσουμε ότι ο λογαριασμός σας δημιουργήθηκε επιτυχώς από τον διαχειριστή του συστήματος.",
                temporaryPassword,
                loginUrl));
    }

    public static (string Subject, string Body) TemporaryPassword(string temporaryPassword, string loginUrl)
    {
        return (
            "Νέος προσωρινός κωδικός πρόσβασης",
            Render(
                "ΝΕΟΣ ΠΡΟΣΩΡΙΝΟΣ<br/>ΚΩΔΙΚΟΣ",
                "Ο διαχειριστής του συστήματος όρισε νέο προσωρινό κωδικό για τον λογαριασμό σας. Ο προηγούμενος κωδικός δεν ισχύει πλέον.",
                temporaryPassword,
                loginUrl));
    }

    private static string Render(string heading, string intro, string temporaryPassword, string loginUrl)
    {
        string password = WebUtility.HtmlEncode(temporaryPassword);
        string url = WebUtility.HtmlEncode(loginUrl);

        return $$"""
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
                 					<tr>
                 						<td style="background-color: #ff4d00; border-bottom: 4px solid #000000; padding: 40px 30px; text-align: left;">
                 							<h1 style="margin: 0; font-size: 32px; font-weight: 700; color: #000000; letter-spacing: -0.5px; text-transform: uppercase;">{{heading}}</h1>
                 						</td>
                 					</tr>
                 					<tr>
                 						<td style="padding: 40px 30px; color: #000000; line-height: 1.6;">
                 							<p style="margin: 0 0 24px 0; font-size: 16px; font-weight: 400;">Αγαπητέ χρήστη,</p>
                 							<p style="margin: 0 0 32px 0; font-size: 16px; font-weight: 400;">{{intro}}</p>
                 							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 32px 0;">
                 								<tr>
                 									<td style="background-color: #fff8f3; border: 4px solid #000000; padding: 24px; box-shadow: 4px 4px 0px #000000;">
                 										<p style="margin: 0 0 12px 0; font-size: 11px; color: #000000; text-transform: uppercase; font-weight: 700; letter-spacing: 1px;">Ο ΠΡΟΣΩΡΙΝΟΣ ΣΑΣ ΚΩΔΙΚΟΣ</p>
                 										<p style="margin: 0; font-family: 'Courier New', monospace; font-size: 28px; color: #ff4d00; font-weight: 700; letter-spacing: 2px;">{{password}}</p>
                 									</td>
                 								</tr>
                 							</table>
                 							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 32px 0;">
                 								<tr>
                 									<td style="background-color: #fffacd; border: 4px solid #000000; padding: 20px;">
                 										<p style="margin: 0; font-size: 14px; font-weight: 700; color: #000000;">⚠ ΣΗΜΑΝΤΙΚΟ</p>
                 										<p style="margin: 8px 0 0 0; font-size: 14px; color: #000000;">Συνιστούμε να αλλάξετε τον κωδικό σας μετά την πρώτη σύνδεση για λόγους ασφαλείας.</p>
                 									</td>
                 								</tr>
                 							</table>
                 							<table width="100%" cellpadding="0" cellspacing="0" style="margin: 40px 0 0 0;">
                 								<tr>
                 									<td align="left">
                 										<a target="_blank" href="{{url}}" style="display: inline-block; background-color: #ff4d00; color: #000000; padding: 18px 36px; text-decoration: none; font-weight: 700; font-size: 16px; text-transform: uppercase; letter-spacing: 0.5px; border: 4px solid #000000; box-shadow: 4px 4px 0px #000000;">ΣΥΝΔΕΣΗ ΣΤΟ ΣΥΣΤΗΜΑ →</a>
                 									</td>
                 								</tr>
                 							</table>
                 						</td>
                 					</tr>
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
    }
}
