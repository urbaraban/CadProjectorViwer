using CadProjector.Ilda;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public partial class ProjectorPreviewViewModel : ObservableObject
{
    private readonly Func<Task> _refresh;
    private readonly Func<Task> _openInViewer;

    public ProjectorPreviewViewModel(
        string deviceId,
        string deviceName,
        Func<Task> refresh,
        Func<Task> openInViewer)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        _refresh = refresh;
        _openInViewer = openInViewer;
        Title = deviceName;
        Stats = "No ILDA frame";
    }

    public string DeviceId { get; }

    [ObservableProperty] public partial string DeviceName { get; set; }
    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial string Stats { get; set; }
    [ObservableProperty] public partial IldaFrame? Frame { get; set; }
    [ObservableProperty] public partial double StrokeThickness { get; set; } = 1.5;
    [ObservableProperty] public partial bool ShowBlanked { get; set; }
    [ObservableProperty] public partial bool ShowPoints { get; set; }

    public void ApplyIldaFrame(IldaFrame? frame, string? stats = null)
    {
        Frame = frame;
        var pts = frame?.Points.Count ?? 0;
        var lit = 0;
        if (frame is not null)
        {
            for (var i = 1; i < frame.Points.Count; i++)
            {
                if (!frame.Points[i - 1].Blanked && !frame.Points[i].Blanked)
                    lit++;
            }
        }

        Stats = stats ?? $"ILDA pts={pts}  lit segs={lit}";
        Title = $"{DeviceName} — ILDA {lit} segs";
    }

    [RelayCommand]
    private async Task RefreshAsync() => await _refresh();

    [RelayCommand]
    private async Task OpenInIldaViewerAsync() => await _openInViewer();
}
