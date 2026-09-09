using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CadProjector.App.Services.Hotkeys;
using CadProjector.App.ViewModels;
using CadProjector.Geometry.Primitives;
using CadProjector.Logging.Services;

namespace CadProjector.App.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;
    private bool _suppressTreeSync;
    private readonly HashSet<HotkeyActionId> _heldNudges = [];
    private readonly HashSet<Key> _keysDown = [];
    private readonly HashSet<HotkeyActionId> _heldCommands = [];
    private KeyModifiers _nudgeModifiers;
    private DispatcherTimer? _nudgeTimer;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnHotkeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnHotkeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => StopNudge();
        Opened += (_, _) =>
        {
            var sync = SynchronizationContext.Current;
            CadLogging.Instance.SetSynchronizationContext(sync);
            CadProgress.Inst.SetSynchronizationContext(sync);

            if (DataContext is MainViewModel vm)
            {
                vm.HostWindow = this;
                vm.WorkFolderBrowser.AttachHost(this);
                vm.ViewResetRequested += () =>
                {
                    SceneView.ResetView();
                    View3D.ResetView();
                };
                vm.PropertyChanged += OnViewModelPropertyChanged;
                Title = "2Cut";
            }
        };
        Closing += OnClosing;
        SceneView.ModuleAnchorChanged += OnModuleAnchorChanged;
        SceneView.MaskBoundsChanged += OnMaskBoundsChanged;
        SceneView.DrawableMoved += OnDrawableMoved;
        SceneView.GestureEnded += OnGestureEnded;
        SceneView.AlignPicked += OnAlignPicked;
        View3D.AlignPicked += OnAlignPicked;
    }

    private void OnAlignPicked(Point3 world, bool meshHit)
    {
        if (DataContext is MainViewModel vm)
            vm.OnMeshAlignPicked(world, meshHit);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.SelectedObjectItem) || _suppressTreeSync)
            return;
        if (sender is not MainViewModel vm)
            return;

        _suppressTreeSync = true;
        try
        {
            ObjectTree.SelectedItem = vm.SelectedObjectItem;
        }
        finally
        {
            _suppressTreeSync = false;
        }
    }

    private void ObjectTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTreeSync || DataContext is not MainViewModel vm || sender is not TreeView tree)
            return;
        var items = tree.SelectedItems.OfType<ObjectListItem>().ToList();
        _suppressTreeSync = true;
        try
        {
            vm.SetTreeSelection(items);
        }
        finally
        {
            _suppressTreeSync = false;
        }
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

    private void OnHotkeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        _nudgeModifiers = e.KeyModifiers;

        if (e.Key is Key.Escape && vm.IsCapturingHotkey)
        {
            vm.CancelHotkeyCapture();
            e.Handled = true;
            return;
        }

        if (vm.IsCapturingHotkey)
        {
            if (vm.TryCaptureHotkey(e.Key, e.KeyModifiers))
                e.Handled = true;
            return;
        }

        if (IsTextInputTarget(e.Source))
            return;

        var action = vm.MatchHotkey(e.Key, e.KeyModifiers);
        if (action is null)
            return;

        e.Handled = true;
        if (HotkeyMap.IsNudge(action.Value))
        {
            if (_heldNudges.Add(action.Value))
            {
                vm.ApplyHeldNudges(_heldNudges, _nudgeModifiers);
                StartNudgeTimer();
            }
            return;
        }

        if (_heldCommands.Add(action.Value))
            vm.ExecuteHotkey(action.Value, e.KeyModifiers);
    }

    private void OnHotkeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        _nudgeModifiers = e.KeyModifiers;
        var action = vm.MatchHotkey(e.Key, e.KeyModifiers);
        if (action is not { } id)
            return;
        _heldCommands.Remove(id);
        if (HotkeyMap.IsNudge(id) && _heldNudges.Remove(id) && _heldNudges.Count == 0)
            StopNudge();
    }

    private void StartNudgeTimer()
    {
        if (_nudgeTimer is not null)
            return;
        _nudgeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _nudgeTimer.Tick += OnNudgeTick;
        _nudgeTimer.Start();
    }

    private void OnNudgeTick(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm || _heldNudges.Count == 0)
        {
            StopNudge();
            return;
        }

        vm.ApplyHeldNudges(_heldNudges, _nudgeModifiers);
    }

    private void StopNudge()
    {
        _heldNudges.Clear();
        if (_nudgeTimer is not null)
        {
            _nudgeTimer.Tick -= OnNudgeTick;
            _nudgeTimer.Stop();
            _nudgeTimer = null;
        }

        if (DataContext is MainViewModel vm)
            vm.EndNudgeGesture();
    }

    private static bool IsTextInputTarget(object? source)
    {
        for (var current = source as Control; current is not null; current = current.Parent as Control)
        {
            if (current is TextBox or NumericUpDown)
                return true;
        }

        return false;
    }
}
