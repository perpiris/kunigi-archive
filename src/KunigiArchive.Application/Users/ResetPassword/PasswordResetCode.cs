namespace KunigiArchive.Application.Users.ResetPassword;

// Code is URL-safe and goes into the emailed link as is. It is signed, expires after an hour and stops
// working once the password changes; nothing is stored for it.
public sealed record PasswordResetCode(Guid UserId, string Email, string Code);
