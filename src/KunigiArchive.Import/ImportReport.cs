using System.Text;

namespace KunigiArchive.Import;

// What the import did beyond copying: rows left out, values it had to change, and files no row points at.
internal sealed class ImportReport
{
    private readonly List<string> _info = [];
    private readonly List<string> _skipped = [];
    private readonly List<string> _changed = [];
    private readonly List<string> _unreferenced = [];

    public void Info(string message) => _info.Add(message);

    public void Skipped(string what, string reason) => _skipped.Add($"{what}: {reason}");

    public void Changed(string what, string change) => _changed.Add($"{what}: {change}");

    public void Unreferenced(string path) => _unreferenced.Add(path);

    public string Render()
    {
        StringBuilder text = new StringBuilder();
        Section(text, "Summary", _info);
        Section(text, "Not imported", _skipped);
        Section(text, "Imported with changes", _changed);
        Section(text, "Files in the old media folder that no row uses (not imported)", _unreferenced);
        return text.ToString();
    }

    private static void Section(StringBuilder text, string title, List<string> lines)
    {
        text.AppendLine($"== {title} ({lines.Count})");
        foreach (string line in lines)
        {
            text.AppendLine($"  - {line}");
        }

        text.AppendLine();
    }
}

internal sealed record ImportSettings(bool Apply, string SourceConnectionString, string SourceMediaRoot, string TargetMediaRoot);
