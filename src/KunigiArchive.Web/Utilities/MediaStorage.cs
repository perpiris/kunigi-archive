namespace KunigiArchive.Web.Utilities;

public static class MediaStorage
{
    public const string ConfigKey = "MediaDirectory";

    // Relative paths resolve against the content root (the project folder), not the working directory.
    public static string ResolveRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        string? configured = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"{ConfigKey} is not configured. Set it in appsettings or the {ConfigKey} environment variable.");
        }

        return Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured));
    }
}
