using System.Text.Json;
using CadProjector.FileFormats.Dxf;

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

    public static AppPrefsState Load()
    {
        try
        {
            if (!File.Exists(PrefsPath)) return new AppPrefsState();
            using var stream = File.OpenRead(PrefsPath);
            return JsonSerializer.Deserialize<AppPrefsState>(stream) ?? new AppPrefsState();
        }
        catch
        {
            return new AppPrefsState();
        }
    }

    public static void Save(AppPrefsState state)
    {
        try
        {
            var dir = Path.GetDirectoryName(PrefsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(PrefsPath, JsonSerializer.Serialize(state, JsonOptions));
        }
        catch
        {
            // Best-effort persistence; ignore IO failures.
        }
    }

    public static void Update(Action<AppPrefsState> mutate)
    {
        var state = Load();
        mutate(state);
        Save(state);
    }

    public static string? LoadWorkFolder() => Load().WorkFolder;

    public static void SaveWorkFolder(string path) =>
        Update(s => s.WorkFolder = path);

    public static string? LoadLastProject() => Load().LastProjectPath;

    public static void SaveLastProject(string? path) =>
        Update(s => s.LastProjectPath = string.IsNullOrWhiteSpace(path) ? null : path);
}

public sealed class AppPrefsState
{
    public string? WorkFolder { get; set; }
    public string? LastProjectPath { get; set; }
    public string? Language { get; set; }
    public int UdpPort { get; set; } = 11000;
    public string? UdpBindIp { get; set; }
    public string? DxfUnits { get; set; }

    public DxfUnitPreference DxfUnitPreference =>
        Enum.TryParse<DxfUnitPreference>(DxfUnits, ignoreCase: true, out var value)
            ? value
            : DxfUnitPreference.Auto;

    public double NudgeStepMm { get; set; } = 1;
    public List<HotkeyPref> Hotkeys { get; set; } = [];
    public WorkspaceLayoutPrefs Workspace { get; set; } = new();
}

public sealed class WorkspaceLayoutPrefs
{
    public string? ActiveLeft { get; set; }
    public string? ActiveRight { get; set; }
    public string? ActiveBottom { get; set; }
    public double LeftWidth { get; set; } = 300;
    public double RightWidth { get; set; } = 340;
    public double BottomHeight { get; set; } = 200;
    public bool IsBottomOpen { get; set; }
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
    public bool IsMaximized { get; set; } = true;
}

public sealed class HotkeyPref
{
    public string Action { get; set; } = "";
    public string Key { get; set; } = "";
    public string Modifiers { get; set; } = "None";
}
