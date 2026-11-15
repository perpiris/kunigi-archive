using System.ComponentModel.DataAnnotations;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;

namespace KunigiArchive.Application.Users;

public static class UserRules
{
    public const int EmailMaxLength = 256;

    private static readonly EmailAddressAttribute EmailFormat = new();

    public static string NormalizeEmail(string? email)
    {
        string trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > EmailMaxLength || !EmailFormat.IsValid(trimmed))
        {
            throw new InvalidRequestException("Το email δεν είναι έγκυρο.");
        }

        return trimmed;
    }

    // Empty means "no role".
    public static string? ValidateRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return Roles.All.FirstOrDefault(r => r == role)
            ?? throw new InvalidRequestException($"Ο ρόλος '{role}' δεν υπάρχει.");
    }
}
