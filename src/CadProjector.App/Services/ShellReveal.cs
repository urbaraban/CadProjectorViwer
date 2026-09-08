using System.Diagnostics;

namespace CadProjector.App.Services;

/// <summary>Open / reveal paths in the OS shell (Windows, macOS, Linux).</summary>
public static class ShellReveal
{
    public static void OpenWithDefaultApp(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public static void RevealInFileManager(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-R \"{path}\"",
                UseShellExecute = false
            });
            return;
        }

        // Linux / other: open containing directory (xdg-open via shell execute).
        var folder = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            folder = path;
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }
}
