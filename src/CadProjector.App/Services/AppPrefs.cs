using System.Text.Json;

namespace CadProjector.App.Services;

/// <summary>Lightweight local prefs (cross-platform via LocalApplicationData).</summary>
public static class AppPrefs
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string PrefsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "2Cut",
            "prefs.json");

    public static string? LoadWorkFolder()
    {
        try
        {
            if (!File.Exists(PrefsPath)) return null;
            using var stream = File.OpenRead(PrefsPath);
            var doc = JsonSerializer.Deserialize<PrefsDto>(stream);
            return string.IsNullOrWhiteSpace(doc?.WorkFolder) ? null : doc!.WorkFolder;
        }
        catch
        {
            return null;
        }
    }

    public static void SaveWorkFolder(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(PrefsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            PrefsDto existing = new();
            if (File.Exists(PrefsPath))
            {
                try
                {
                    var text = File.ReadAllText(PrefsPath);
                    existing = JsonSerializer.Deserialize<PrefsDto>(text) ?? new PrefsDto();
                }
                catch
                {
                    existing = new PrefsDto();
                }
            }

            existing.WorkFolder = path;
            File.WriteAllText(PrefsPath, JsonSerializer.Serialize(existing, JsonOptions));
        }
        catch
        {
            // Best-effort persistence; ignore IO failures.
        }
    }

    private sealed class PrefsDto
    {
        public string? WorkFolder { get; set; }
    }
}
