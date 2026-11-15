namespace KunigiArchive.Infrastructure.Email;

// Same section and keys as the old app (EmailSettings__Password etc. in the env file).
public sealed class EmailOptions
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;

    // Base URL for links in emails, e.g. https://tokinigi.gr.
    public string ApplicationUrl { get; set; } = string.Empty;

    // Development: write .eml files here instead of sending. Relative paths resolve against the content root.
    public string? PickupDirectory { get; set; }
}
