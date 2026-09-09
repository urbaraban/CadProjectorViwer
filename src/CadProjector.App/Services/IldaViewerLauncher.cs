using System.Diagnostics;

namespace CadProjector.App.Services;

/// <summary>Launch external ILDAViewer.net on a temp/exported .ild when available.</summary>
public static class IldaViewerLauncher
{
    private static readonly string[] CandidateRelative =
    [
        Path.Combine("ILDAViewer.net", "src", "bin", "Release", "net10.0-windows7.0", "ILDAViewer.net.exe"),
        Path.Combine("ILDAViewer.net", "src", "bin", "Debug", "net10.0-windows7.0", "ILDAViewer.net.exe"),
        Path.Combine("ILDAViewer.net", "bin", "Release", "ILDAViewer.net.exe"),
    ];

    public static string? FindExecutable()
    {
        var env = Environment.GetEnvironmentVariable("ILDAVIEWER_PATH");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            return env;

        // Walk up from app base / cwd looking for sibling repo.
        foreach (var root in EnumerateSearchRoots())
        {
            foreach (var rel in CandidateRelative)
            {
                var full = Path.GetFullPath(Path.Combine(root, rel));
                if (File.Exists(full))
                    return full;
            }

            // Also: sibling of CadProjectorViwer folder
            var sibling = Path.GetFullPath(Path.Combine(root, "..", "ILDAViewer.net", "src", "bin", "Release", "net10.0-windows7.0", "ILDAViewer.net.exe"));
            if (File.Exists(sibling))
                return sibling;
            sibling = Path.GetFullPath(Path.Combine(root, "..", "ILDAViewer.net", "src", "bin", "Debug", "net10.0-windows7.0", "ILDAViewer.net.exe"));
            if (File.Exists(sibling))
                return sibling;
        }

        return null;
    }

    public static bool TryOpen(string ildPath, out string? error)
    {
        error = null;
        if (!File.Exists(ildPath))
        {
            error = "ILDA file not found.";
            return false;
        }

        var exe = FindExecutable();
        if (exe is null)
        {
            error = "ILDAViewer.net.exe not found. Build ILDAViewer.net or set ILDAVIEWER_PATH.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"\"{ildPath}\"",
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        yield return AppContext.BaseDirectory;
        yield return Environment.CurrentDirectory;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
            yield return dir.FullName;
    }
}
