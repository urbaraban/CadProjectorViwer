using Avalonia.Controls;
using Avalonia.Input;
using CadProjector.App.ViewModels;
using CadProjector.Ilda;

namespace CadProjector.App.Views;

public partial class ProjectorPreviewWindow : Window
{
    public ProjectorPreviewWindow()
    {
        InitializeComponent();
        KeyUp += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };
    }

    public string DeviceId => (DataContext as ProjectorPreviewViewModel)?.DeviceId ?? string.Empty;

    public void ApplyIldaFrame(IldaFrame? frame, string? stats = null)
    {
        if (DataContext is not ProjectorPreviewViewModel vm) return;
        vm.ApplyIldaFrame(frame, stats);
    }
}
