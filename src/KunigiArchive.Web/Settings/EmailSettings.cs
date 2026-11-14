namespace KunigiArchive.Web.Settings;

public class EmailSettings
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool EnableSsl { get; set; }
    public required string SenderEmail { get; set; }
    public required string SenderName { get; set; }
    public required string ApplicationUrl { get; set; }
}