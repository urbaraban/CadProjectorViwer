using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CadProjector.App.Services.Hotkeys;
using CadProjector.App.ViewModels;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;
using CadProjector.Logging.Services;

namespace CadProjector.App.Views;

public partial class MainWindow : Window
{
    private const double LeftDockMin = 220;
    private const double LeftDockMax = 480;
    private const double RightDockMin = 280;
    private const double RightDockMax = 560;
    private const double BottomDockMin = 120;
    private const double BottomDockMax = 480;

    private bool _forceClose;
    private bool _syncingDockLayout;
    private MainViewModel? _vm;
    private readonly HashSet<HotkeyActionId> _heldNudges = [];
    private readonly HashSet<HotkeyActionId> _heldCommands = [];
    private KeyModifiers _nudgeModifiers;
    private DispatcherTimer? _nudgeTimer;

    private ColumnDefinition LeftDockColumn => WorkAreaGrid.ColumnDefinitions[1];
    private ColumnDefinition RightDockColumn => WorkAreaGrid.ColumnDefinitions[5];
    private RowDefinition BottomSplitterRow => RootShell.RowDefinitions[2];
    private RowDefinition BottomDockRow => RootShell.RowDefinitions[3];

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnHotkeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnHotkeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => StopNudge();
        Opened += OnOpened;
        Closing += OnClosing;
        SceneView.ModuleAnchorChanged += OnModuleAnchorChanged;
        SceneView.MaskBoundsChanged += OnMaskBoundsChanged;
        SceneView.DrawableMoved += OnDrawableMoved;
        SceneView.GestureEnded += OnGestureEnded;
        SceneView.AlignPicked += OnAlignPicked;
        View3D.AlignPicked += OnAlignPicked;
        View3D.CoverageComputed += OnCoverageComputed;

        LeftDockColumn.MinWidth = 0;
        LeftDockColumn.MaxWidth = LeftDockMax;
        RightDockColumn.MinWidth = 0;
        RightDockColumn.MaxWidth = RightDockMax;
        BottomDockRow.MinHeight = 0;
        BottomDockRow.MaxHeight = BottomDockMax;

        LeftSplitter.PointerReleased += OnDockSplitterReleased;
        RightSplitter.PointerReleased += OnDockSplitterReleased;
        BottomSplitter.PointerReleased += OnDockSplitterReleased;
    }

    private void OnDockSplitterReleased(object? sender, PointerReleasedEventArgs e)
        => CaptureDockLayoutToWorkspace();

    private void OnOpened(object? sender, EventArgs e)
    {
        var sync = SynchronizationContext.Current;
        CadLogging.Instance.SetSynchronizationContext(sync);
        CadProgress.Inst.SetSynchronizationContext(sync);

        if (DataContext is not MainViewModel vm)
            return;

        _vm = vm;
        vm.HostWindow = this;
        vm.WorkFolderBrowser.AttachHost(this);
        vm.ViewResetRequested += () =>
        {
            SceneView.ResetView();
            View3D.ResetView();
        };
        Title = "2Cut";
        vm.LoadWorkspaceLayout();
        ApplyDockLayoutFromWorkspace(forceWidths: true);
        vm.PropertyChanged += OnViewModelPropertyChanged;
        vm.Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        _ = vm.TryOpenLastProjectAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsLeftDockOpen)
            or nameof(MainViewModel.IsRightDockOpen))
            ApplyDockLayoutFromWorkspace(forceWidths: false);
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorkspaceViewModel.LeftWidth)
            or nameof(WorkspaceViewModel.RightWidth)
            or nameof(WorkspaceViewModel.BottomHeight))
            ApplyDockLayoutFromWorkspace(forceWidths: true);
        else if (e.PropertyName is nameof(WorkspaceViewModel.IsBottomOpen)
            or nameof(WorkspaceViewModel.ActiveLeft)
            or nameof(WorkspaceViewModel.ActiveRight))
            ApplyDockLayoutFromWorkspace(forceWidths: false);
    }

    private void ApplyDockLayoutFromWorkspace(bool forceWidths)
    {
        if (_vm is null || _syncingDockLayout)
            return;

        _syncingDockLayout = true;
        try
        {
            var ws = _vm.Workspace;
            ApplySideDock(
                _vm.IsLeftDockOpen, LeftDockColumn, LeftDockHost, LeftSplitter,
                ws.LeftWidth, LeftDockMin, LeftDockMax, forceWidths,
                w => ws.LeftWidth = w);
            ApplySideDock(
                _vm.IsRightDockOpen, RightDockColumn, RightDockHost, RightSplitter,
                ws.RightWidth, RightDockMin, RightDockMax, forceWidths,
                w => ws.RightWidth = w);

            var bottomOpen = ws.IsBottomOpen;
            if (bottomOpen)
            {
                var current = BottomDockRow.Height.IsAbsolute ? BottomDockRow.Height.Value : 0;
                if (forceWidths || current < BottomDockMin)
                    BottomDockRow.Height = new GridLength(Clamp(ws.BottomHeight, BottomDockMin, BottomDockMax));
                BottomDockRow.MinHeight = BottomDockMin;
                BottomSplitterRow.Height = new GridLength(4);
            }
            else
            {
                CaptureRow(BottomDockRow, BottomDockMin, BottomDockMax, h => ws.BottomHeight = h);
                BottomDockRow.Height = new GridLength(0);
                BottomDockRow.MinHeight = 0;
                BottomSplitterRow.Height = new GridLength(0);
            }

            BottomSplitter.IsVisible = bottomOpen;
            BottomSplitter.IsEnabled = bottomOpen;
            BottomDockHost.IsVisible = bottomOpen;
        }
        finally
        {
            _syncingDockLayout = false;
        }
    }

    private static void ApplySideDock(
        bool open,
        ColumnDefinition column,
        Control host,
        Control splitter,
        double storedWidth,
        double min,
        double max,
        bool forceWidth,
        Action<double> saveWidth)
    {
        if (open)
        {
            var current = column.Width.IsAbsolute ? column.Width.Value : 0;
            if (forceWidth || current < min)
                column.Width = new GridLength(Clamp(storedWidth, min, max));
            column.MinWidth = min;
        }
        else
        {
            CaptureColumn(column, min, max, saveWidth);
            column.Width = new GridLength(0);
            column.MinWidth = 0;
        }

        host.IsVisible = open;
        splitter.IsVisible = open;
        splitter.IsEnabled = open;
    }

    private void CaptureDockLayoutToWorkspace()
    {
        if (_vm is null || _syncingDockLayout)
            return;

        var ws = _vm.Workspace;
        CaptureColumn(LeftDockColumn, LeftDockMin, LeftDockMax, w => ws.LeftWidth = w);
        CaptureColumn(RightDockColumn, RightDockMin, RightDockMax, w => ws.RightWidth = w);
        CaptureRow(BottomDockRow, BottomDockMin, BottomDockMax, h => ws.BottomHeight = h);
    }

    private static void CaptureColumn(ColumnDefinition column, double min, double max, Action<double> save)
    {
        if (column.Width.IsAbsolute && column.Width.Value >= min)
            save(Clamp(column.Width.Value, min, max));
    }

    private static void CaptureRow(RowDefinition row, double min, double max, Action<double> save)
    {
        if (row.Height.IsAbsolute && row.Height.Value >= min)
            save(Clamp(row.Height.Value, min, max));
    }

    private static double Clamp(double value, double min, double max)
        => Math.Clamp(value, min, max);

    private void ProjectPath_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.RevealProjectFolderCommand.Execute(null);
    }

    private void OnCoverageComputed(MeshCoverageStats stats, bool shadowsTested)
    {
        if (DataContext is MainViewModel vm)
            vm.ReportCoverage(stats, shadowsTested);
    }

    private void OnAlignPicked(Point3 world, bool meshHit)
    {
        if (DataContext is MainViewModel vm)
            vm.OnMeshAlignPicked(world, meshHit);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || DataContext is not MainViewModel vm)
            return;

        CaptureDockLayoutToWorkspace();
        vm.SaveWorkspaceLayout(Bounds.Width, Bounds.Height, WindowState == WindowState.Maximized);

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

        if (e.Key is Key.Escape && !IsTextInputTarget(e.Source))
        {
            CaptureDockLayoutToWorkspace();
            vm.Workspace.CloseFocusedOrBottom();
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

        if (e.KeyModifiers == KeyModifiers.None)
        {
            switch (e.Key)
            {
                case Key.F1:
                    vm.OpenHotkeysCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.F2:
                    CaptureDockLayoutToWorkspace();
                    vm.OpenTreeCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.F3:
                    vm.IsViewport3D = !vm.IsViewport3D;
                    e.Handled = true;
                    return;
                case Key.F4:
                    CaptureDockLayoutToWorkspace();
                    vm.OpenTransformCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.F12:
                    CaptureDockLayoutToWorkspace();
                    vm.ToggleBottomConsoleCommand.Execute(null);
                    e.Handled = true;
                    return;
            }
        }

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
