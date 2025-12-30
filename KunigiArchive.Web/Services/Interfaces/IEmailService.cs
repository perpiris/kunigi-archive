namespace KunigiArchive.Web.Services.Interfaces;

public interface IEmailService
{
    Task<ServiceResponse> SendAccountCreationEmailAsync(string toEmail, string password);
    Task<ServiceResponse> SendPasswordResetEmailAsync(string toEmail, string resetCode);
}