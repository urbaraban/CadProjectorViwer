using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CadProjector.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public sealed class WorkFolderEntry
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required bool IsDirectory { get; init; }
    public required DateTime LastWriteTime { get; init; }
    public bool IsParent { get; init; }

    public string IconGlyph => IsDirectory ? "□" : "·";
    public string DisplayExtension => IsDirectory ? string.Empty : Extension;
    public string DateText => LastWriteTime == default
        ? string.Empty
        : LastWriteTime.ToString("HH:mm dd.MM.yy");
}

public partial class WorkFolderViewModel : ObservableObject
{
    private static readonly string[] SupportedFileExtensions =
        [".dxf", ".svg", ".2scn", ".2cfg", ".mws", ".cproj", ".stl"];
    private readonly List<WorkFolderEntry> _all = [];
    private readonly Action<string> _onPathChanged;
    private readonly Func<string, Task> _onFileSelected;
    private Window? _host;
    private bool _suppressPathNotify;

    public WorkFolderViewModel(Action<string> onPathChanged, Func<string, Task> onFileSelected)
    {
        _onPathChanged = onPathChanged;
        _onFileSelected = onFileSelected;

        ExtensionChoices =
        [
            new ExtensionFilterItem("*", "All"),
            new ExtensionFilterItem(".dxf", "DXF"),
            new ExtensionFilterItem(".svg", "SVG"),
            new ExtensionFilterItem(".2scn", "2SCN"),
            new ExtensionFilterItem(".2cfg", "2CFG"),
            new ExtensionFilterItem(".mws", "MWS"),
            new ExtensionFilterItem(".cproj", "CPROJ"),
            new ExtensionFilterItem(".stl", "STL")
        ];
        SelectedExtension = ExtensionChoices[0];

        var saved = AppPrefs.LoadWorkFolder();
        var start = !string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved!)
            ? saved!
            : Environment.CurrentDirectory;
        SetPathInternal(start, notify: true);
        Refresh();
    }

    public void AttachHost(Window? host) => _host = host;

    public ObservableCollection<WorkFolderEntry> Items { get; } = [];
    public IReadOnlyList<ExtensionFilterItem> ExtensionChoices { get; }

    [ObservableProperty] public partial string CurrentPath { get; set; } = Environment.CurrentDirectory;
    [ObservableProperty] public partial string StringFilter { get; set; } = string.Empty;
    [ObservableProperty] public partial ExtensionFilterItem? SelectedExtension { get; set; }
    [ObservableProperty] public partial WorkFolderEntry? SelectedItem { get; set; }

    partial void OnCurrentPathChanged(string value)
    {
        if (_suppressPathNotify || string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var full = Path.GetFullPath(value);
            if (!Directory.Exists(full)) return;
            if (!string.Equals(value, full, StringComparison.OrdinalIgnoreCase))
            {
                SetPathInternal(full, notify: true);
                Refresh();
                return;
            }

            AppPrefs.SaveWorkFolder(full);
            _onPathChanged(full);
            Refresh();
        }
        catch
        {
            // Ignore invalid paths typed by the user until Browse/Refresh.
        }
    }

    private void SetPathInternal(string fullPath, bool notify)
    {
        _suppressPathNotify = true;
        try
        {
            CurrentPath = fullPath;
        }
        finally
        {
            _suppressPathNotify = false;
        }

        if (notify)
        {
            AppPrefs.SaveWorkFolder(fullPath);
            _onPathChanged(fullPath);
        }
    }

    partial void OnStringFilterChanged(string value) => ApplyFilter();
    partial void OnSelectedExtensionChanged(ExtensionFilterItem? value) => ApplyFilter();

    [RelayCommand]
    private void Refresh()
    {
        _all.Clear();
        Items.Clear();

        string path;
        try
        {
            path = Path.GetFullPath(CurrentPath);
        }
        catch
        {
            return;
        }

        if (!Directory.Exists(path))
            return;

        if (!string.Equals(CurrentPath, path, StringComparison.Ordinal))
            SetPathInternal(path, notify: true);

        try
        {
            var dir = new DirectoryInfo(path);
            if (dir.Parent is { } parent)
            {
                _all.Add(new WorkFolderEntry
                {
                    FullPath = parent.FullName,
                    Name = "..",
                    Extension = string.Empty,
                    IsDirectory = true,
                    IsParent = true,
                    LastWriteTime = default
                });
            }

            foreach (var sub in dir.EnumerateDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                _all.Add(new WorkFolderEntry
                {
                    FullPath = sub.FullName,
                    Name = sub.Name,
                    Extension = string.Empty,
                    IsDirectory = true,
                    LastWriteTime = sub.LastWriteTime
                });
            }

            foreach (var file in dir.EnumerateFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                var ext = file.Extension.ToLowerInvariant();
                if (!SupportedFileExtensions.Contains(ext))
                    continue;

                _all.Add(new WorkFolderEntry
                {
                    FullPath = file.FullName,
                    Name = Path.GetFileNameWithoutExtension(file.Name),
                    Extension = ext,
                    IsDirectory = false,
                    LastWriteTime = file.LastWriteTime
                });
            }
        }
        catch
        {
            // Access denied / IO — leave list empty.
        }

        ApplyFilter();
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (_host is null) return;
        var folders = await _host.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Work folder",
            AllowMultiple = false
        });
        if (folders.Count == 0) return;
        var local = folders[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(local) || !Directory.Exists(local)) return;
        SetPathInternal(local, notify: true);
        Refresh();
    }

    [RelayCommand]
    private void ClearFilter() => StringFilter = string.Empty;

    [RelayCommand]
    private async Task ActivateAsync(WorkFolderEntry? entry)
    {
        entry ??= SelectedItem;
        if (entry is null) return;

        if (entry.IsDirectory)
        {
            SetPathInternal(entry.FullPath, notify: true);
            Refresh();
            return;
        }

        await _onFileSelected(entry.FullPath);
    }

    [RelayCommand]
    private void RevealSelected()
    {
        var path = SelectedItem?.FullPath ?? CurrentPath;
        try { ShellReveal.RevealInFileManager(path); }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private void OpenSelectedInEditor()
    {
        if (SelectedItem is null || SelectedItem.IsDirectory) return;
        try { ShellReveal.OpenWithDefaultApp(SelectedItem.FullPath); }
        catch { /* ignore */ }
    }

    /// <summary>Called when MainViewModel.WorkFolder is set externally (e.g. Automation).</summary>
    public void SyncFromExternal(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var full = Path.GetFullPath(path);
            if (!Directory.Exists(full)) return;
            if (string.Equals(CurrentPath, full, StringComparison.OrdinalIgnoreCase)) return;
            SetPathInternal(full, notify: false);
            AppPrefs.SaveWorkFolder(full);
            Refresh();
        }
        catch
        {
            // ignore
        }
    }

    private void ApplyFilter()
    {
        Items.Clear();
        var filter = StringFilter ?? string.Empty;
        var ext = SelectedExtension?.Extension ?? "*";

        foreach (var item in _all)
        {
            if (item.IsDirectory)
            {
                if (item.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) || item.IsParent)
                    Items.Add(item);
                continue;
            }

            if (!string.IsNullOrEmpty(filter) &&
                !item.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) &&
                !item.Extension.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (ext != "*" && !string.Equals(item.Extension, ext, StringComparison.OrdinalIgnoreCase))
                continue;

            Items.Add(item);
        }
    }
}

public sealed class ExtensionFilterItem(string extension, string label)
{
    public string Extension { get; } = extension;
    public string Label { get; } = label;
    public override string ToString() => Label;
}
