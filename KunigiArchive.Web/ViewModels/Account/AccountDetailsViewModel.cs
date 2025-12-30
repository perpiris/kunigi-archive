namespace KunigiArchive.Web.ViewModels.Account;

public class AccountDetailsViewModel
{
    public long AppUserId { get; set; }
    public required string Email { get; set; }
    public string? Role { get; set; }
}