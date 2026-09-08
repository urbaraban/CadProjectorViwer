using Avalonia.Controls;
using Avalonia.Input;
using CadProjector.App.ViewModels;
using CadProjector.Geometry.Primitives;

namespace CadProjector.App.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.HostWindow = this;
                vm.WorkFolderBrowser.AttachHost(this);
                vm.ViewResetRequested += () => SceneView.ResetView();
                Title = "2Cut";
            }
        };
        Closing += OnClosing;
        SceneView.ModuleAnchorChanged += OnModuleAnchorChanged;
        SceneView.MaskBoundsChanged += OnMaskBoundsChanged;
        SceneView.DrawableMoved += OnDrawableMoved;
        SceneView.GestureEnded += OnGestureEnded;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || DataContext is not MainViewModel vm)
            return;
        if (!vm.IsDirty)
            return;

        e.Cancel = true;
        if (!await vm.ConfirmDiscardOrSaveAsync())
            return;

        _forceClose = true;
        Close();
    }

    private void OnDrawableMoved(int index, Point3 from, Point3 to)
    {
        if (DataContext is MainViewModel vm)
            vm.ApplyDrawableDrag(index, from, to);
    }

    private void OnGestureEnded()
    {
        if (DataContext is MainViewModel vm)
            vm.EndGesture();
    }

    private void OnModuleAnchorChanged(string moduleId, int anchorIndex, Point2 unit)
    {
        if (DataContext is MainViewModel vm)
            vm.ApplyModuleAnchor(moduleId, anchorIndex, unit);
    }

    private void OnMaskBoundsChanged(Rect2 from, Rect2 to)
    {
        if (DataContext is MainViewModel vm)
            vm.ApplyMaskBounds(from, to);
    }

    private async void WorkFolderList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is ListBox { SelectedItem: WorkFolderEntry entry })
            await vm.WorkFolderBrowser.ActivateCommand.ExecuteAsync(entry);
    }

    private async void WorkFolderList_KeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not ListBox list) return;

        if (e.Key == Key.Escape)
        {
            vm.WorkFolderBrowser.ClearFilterCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && list.SelectedItem is WorkFolderEntry entry)
        {
            await vm.WorkFolderBrowser.ActivateCommand.ExecuteAsync(entry);
            e.Handled = true;
        }
    }
}
