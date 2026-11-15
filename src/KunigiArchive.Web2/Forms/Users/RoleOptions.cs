using KunigiArchive.Application.Authorization;

namespace KunigiArchive.Web2.Forms.Users;

// Display names from the old app. An empty value stands for "no role".
public static class RoleOptions
{
    public static readonly IReadOnlyList<(string Value, string Label)> All =
    [
        (Roles.Admin, "Γενικός Διαχειριστής"),
        (Roles.Manager, "Διαχειριστής Ομάδας"),
        (string.Empty, "Χωρίς ρόλο")
    ];

    public static string Label(string? role)
    {
        return All.FirstOrDefault(o => o.Value == (role ?? string.Empty)).Label ?? role ?? string.Empty;
    }
}
