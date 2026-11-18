using System.Net;

namespace KunigiArchive.Infrastructure.Email;

// The account emails in the site's "Archive" look: whitewashed paper, sachnisi-brown ink, Venetian red.
// Email clients ignore stylesheets and CSS variables, so the theme colors are written inline as hex values
// (wwwroot/app.css, light theme). Everything passed in is HTML-encoded here.
internal static class AccountEmailTemplates
{
    private const string Paper = "#f2efe8";
    private const string Sheet = "#fbfaf6";
    private const string Ink = "#2b2018";
    private const string Faded = "#6e6255";
    private const string Rule = "#ddd5c8";
    private const string Accent = "#a8352a";
    private const string OnAccent = "#fbfaf6";
    private const string Sea = "#1d4f73";

    private const string Display = "'GFS Didot', Georgia, 'Times New Roman', serif";
    private const string Text = "'Source Sans 3', 'Segoe UI', Helvetica, Arial, sans-serif";

    public static (string Subject, string Body) AccountCreated(string temporaryPassword, string loginUrl, string siteUrl)
    {
        return (
            "Ο λογαριασμός σας στο Αρχείο του Κυνηγιού Θησαυρού",
            Render(
                siteUrl,
                "Ο λογαριασμός σας δημιουργήθηκε",
                Paragraph("Ο διαχειριστής του αρχείου δημιούργησε λογαριασμό για εσάς. Συνδεθείτε με το email σας και τον παρακάτω προσωρινό κωδικό.")
                + CodeBox("Προσωρινός κωδικός", temporaryPassword)
                + Button("Σύνδεση", loginUrl)
                + Note("Αλλάξτε τον κωδικό σας μετά την πρώτη σύνδεση.")));
    }

    public static (string Subject, string Body) TemporaryPassword(string temporaryPassword, string loginUrl, string siteUrl)
    {
        return (
            "Νέος προσωρινός κωδικός πρόσβασης",
            Render(
                siteUrl,
                "Νέος προσωρινός κωδικός",
                Paragraph("Ο διαχειριστής του αρχείου όρισε νέο προσωρινό κωδικό για τον λογαριασμό σας. Ο προηγούμενος κωδικός δεν ισχύει πλέον.")
                + CodeBox("Προσωρινός κωδικός", temporaryPassword)
                + Button("Σύνδεση", loginUrl)
                + Note("Αλλάξτε τον κωδικό σας μετά τη σύνδεση.")));
    }

    public static (string Subject, string Body) PasswordReset(string resetUrl, string siteUrl)
    {
        return (
            "Επαναφορά κωδικού πρόσβασης",
            Render(
                siteUrl,
                "Επαναφορά κωδικού",
                Paragraph("Ζητήθηκε νέος κωδικός για τον λογαριασμό σας. Πατήστε το κουμπί για να ορίσετε τον νέο κωδικό. Ο σύνδεσμος ισχύει για μία ώρα και μόνο για μία χρήση.")
                + Button("Ορισμός νέου κωδικού", resetUrl)
                + Fallback(resetUrl)
                + Note("Αν δεν το ζητήσατε εσείς, αγνοήστε αυτό το email. Ο κωδικός σας δεν αλλάζει.")));
    }

    private static string Paragraph(string text)
    {
        return $"""<p style="margin: 0 0 24px; font-size: 16px; line-height: 1.6; color: {Ink};">{WebUtility.HtmlEncode(text)}</p>""";
    }

    // A slip of paper with a red edge, like the site's stamps: the value is what the reader copies.
    private static string CodeBox(string label, string value)
    {
        return $"""
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin: 0 0 28px;">
                	<tr>
                		<td style="background-color: {Paper}; border-left: 3px solid {Accent}; padding: 16px 20px;">
                			<p style="margin: 0 0 6px; font-family: {Text}; font-size: 12px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: {Faded};">{WebUtility.HtmlEncode(label)}</p>
                			<p style="margin: 0; font-family: Consolas, 'Courier New', monospace; font-size: 24px; font-weight: 700; letter-spacing: 2px; color: {Ink};">{WebUtility.HtmlEncode(value)}</p>
                		</td>
                	</tr>
                </table>
                """;
    }

    private static string Button(string label, string url)
    {
        return $"""
                <table role="presentation" cellpadding="0" cellspacing="0" style="margin: 0 0 28px;">
                	<tr>
                		<td style="background-color: {Accent}; border-radius: 2px;">
                			<a href="{WebUtility.HtmlEncode(url)}" target="_blank" style="display: inline-block; padding: 12px 28px; font-family: {Text}; font-size: 16px; font-weight: 600; color: {OnAccent}; text-decoration: none;">{WebUtility.HtmlEncode(label)}</a>
                		</td>
                	</tr>
                </table>
                """;
    }

    // For clients that block the button: the same link as text.
    private static string Fallback(string url)
    {
        string encoded = WebUtility.HtmlEncode(url);
        return $"""
                <p style="margin: 0 0 24px; font-size: 13px; line-height: 1.5; color: {Faded};">Αν το κουμπί δεν λειτουργεί, ανοίξτε αυτόν τον σύνδεσμο:<br />
                <a href="{encoded}" target="_blank" style="color: {Sea}; word-break: break-all;">{encoded}</a></p>
                """;
    }

    private static string Note(string text)
    {
        return $"""<p style="margin: 0; padding-top: 20px; border-top: 1px solid {Rule}; font-size: 14px; line-height: 1.5; color: {Faded};">{WebUtility.HtmlEncode(text)}</p>""";
    }

    private static string Render(string siteUrl, string heading, string content)
    {
        string site = WebUtility.HtmlEncode(siteUrl);
        string title = WebUtility.HtmlEncode(heading);

        return $"""
                <!DOCTYPE html>
                <html lang="el">
                <head>
                	<meta charset="UTF-8">
                	<meta name="viewport" content="width=device-width, initial-scale=1">
                	<meta name="color-scheme" content="light">
                	<title>{title}</title>
                </head>
                <body style="margin: 0; padding: 0; background-color: {Paper}; font-family: {Text}; color: {Ink};">
                	<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color: {Paper};">
                		<tr>
                			<td align="center" style="padding: 32px 16px;">
                				<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width: 560px;">
                					<tr>
                						<td style="padding: 0 4px 20px;">
                							<a href="{site}" target="_blank" style="text-decoration: none; color: {Ink};">
                								<table role="presentation" cellpadding="0" cellspacing="0">
                									<tr>
                										<td style="padding-right: 12px; vertical-align: middle;">
                											<img src="{site}/favicon-96x96.png" width="40" height="40" alt="" style="display: block; border: 0; border-radius: 9px;">
                										</td>
                										<td style="vertical-align: middle;">
                											<p style="margin: 0; font-family: {Display}; font-size: 20px; line-height: 1.2; color: {Ink};">Το Κυνήγι Θησαυρού</p>
                										</td>
                									</tr>
                								</table>
                							</a>
                						</td>
                					</tr>
                					<tr>
                						<td style="background-color: {Sheet}; border: 1px solid {Rule}; border-top: 3px solid {Accent}; padding: 36px 32px 32px;">
                							<h1 style="margin: 0 0 20px; font-family: {Display}; font-size: 30px; font-weight: 400; line-height: 1.2; color: {Ink};">{title}</h1>
                							{content}
                						</td>
                					</tr>
                					<tr>
                						<td style="padding: 20px 4px 0; font-family: {Text}; font-size: 12px; line-height: 1.5; color: {Faded};">
                							Το αρχείο του Κυνηγιού Θησαυρού του Ρεθύμνου. Το email στάλθηκε αυτόματα, μην απαντήσετε σε αυτό.
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