using System.Security.Cryptography;

namespace KunigiArchive.Application.Users;

public static class TemporaryPassword
{
    private const int Length = 12;

    // No look-alikes (I/l/1, O/0), since people type it from an email.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";

    // Always has an uppercase letter, a lowercase letter and a digit, as the Identity password policy requires.
    public static string Generate()
    {
        char[] password =
        [
            .. RandomNumberGenerator.GetItems<char>(Upper, 1),
            .. RandomNumberGenerator.GetItems<char>(Lower, 1),
            .. RandomNumberGenerator.GetItems<char>(Digits, 1),
            .. RandomNumberGenerator.GetItems<char>(Upper + Lower + Digits, Length - 3)
        ];

        RandomNumberGenerator.Shuffle(password.AsSpan());
        return new string(password);
    }
}
