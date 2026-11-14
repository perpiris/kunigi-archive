namespace KunigiArchive.Infrastructure.Identity;

public sealed record AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string? Email { get; init; }
    public string? Password { get; init; }
}