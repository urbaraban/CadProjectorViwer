using CadProjector.Core.Project;
using CadProjector.Core.Scene;

namespace CadProjector.FileFormats.Legacy;

public enum LegacyKind
{
    Hub,
    Scene,
    ObjectsRoot
}

public sealed class LegacyMigrationReport
{
    public string SourcePath { get; init; } = "";
    public LegacyKind Kind { get; set; }
    public List<string> Imported { get; } = [];
    public List<string> Skipped { get; } = [];
    public List<string> Warnings { get; } = [];

    public void ImportedItem(string message) => Imported.Add(message);
    public void Skip(string message) => Skipped.Add(message);
    public void Warn(string message) => Warnings.Add(message);

    public IEnumerable<string> AllLines()
    {
        foreach (var line in Imported)
            yield return "ok: " + line;
        foreach (var line in Warnings)
            yield return "warn: " + line;
        foreach (var line in Skipped)
            yield return "skip: " + line;
    }
}

public sealed class LegacyImportResult
{
    public required ProjectDocument Project { get; init; }
    public required LegacyMigrationReport Report { get; init; }
    public bool HasDevices { get; init; }
    public bool HasScenes { get; init; }
}
