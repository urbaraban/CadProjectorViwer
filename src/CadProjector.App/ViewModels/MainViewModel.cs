using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CadProjector.App.Controls;
using CadProjector.App.Services;
using CadProjector.Automation;
using CadProjector.Core.Devices;
using CadProjector.Core.Editing;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Devices;
using CadProjector.FileFormats;
using CadProjector.FileFormats.ProjectJson;
using CadProjector.Geometry.Primitives;
using CadProjector.Ilda;
using CadProjector.Rendering;
using CadProjector.Rendering.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly VirtualProjector _virtual = new();
    private readonly DrawingImportService _import = new();
    private readonly AutomationHub _hub = new();
    private readonly DevicePipeline _pipeline = new();
    private readonly Dictionary<string, VltProjector> _vlts = new();
    private string? _projectPath;
    private bool _suppressMeshUi;
    private bool _suppressTransformUi;
    private int _selectedAnchor = -1;
    private bool _suppressDeviceUi;
    private CancellationTokenSource? _ildExportCts;
    private bool _suppressWorkFolderSync;

    /// <summary>Set while undo/redo replays a change, so the replay is not recorded again.</summary>
    private bool _restoring;
    private bool _suppressSceneUi;

    public MainViewModel()
    {
        WorkFolderBrowser = new WorkFolderViewModel(
            onPathChanged: path =>
            {
                _suppressWorkFolderSync = true;
                try { WorkFolder = path; }
                finally { _suppressWorkFolderSync = false; }
                _hub.WorkFolder = path;
            },
            onFileSelected: async path => await ImportPathAsync(path, clear: true, play: false));

        Project = new ProjectDocument { Name = "2Cut" };
        var primary = ProjectorProfile.CreateDefault("VLT-1", "192.168.0.10", 10000);
        Projectors.Add(primary);
        SelectedProjector = primary;
        Scenes.Add(Project.ActiveScene);
        SelectedScene = Project.ActiveScene;
        primary.FitFovToScene(SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
        MeshColumns = 3;
        MeshRows = 3;
        PullDeviceUiFromSelection();
        RefreshFovOverlays();
        _ = _virtual.ConnectAsync();
        WorkFolder = WorkFolderBrowser.CurrentPath;
        _hub.WorkFolder = WorkFolder;
        _hub.CommandReceived += OnRemoteCommand;
        _hub.Error += (_, msg) => Dispatcher.UIThread.Post(() => Log($"Automation error: {msg}"));
        RefreshEndpointItems();
        NewEndpointType = RemoteEndpointType.UdpBinary;
        RefreshObjectNames();
        History.Changed += OnHistoryChanged;
        PullSceneUiFromSelection();
        UseLayerColor = Project.ColorMode == LaserColorMode.LayerColor;
        Log("Ready — add projectors and modules in Devices");
    }

    public AutomationHub Hub => _hub;
    public WorkFolderViewModel WorkFolderBrowser { get; }
    public ObservableCollection<EndpointItemViewModel> EndpointItems { get; } = [];
    public ObservableCollection<ConnectedClient> AutomationClients => _hub.Clients;
    public ObservableCollection<string> AutomationLog => _hub.RecentLog;

    public Array EndpointTypeChoices { get; } = Enum.GetValues<RemoteEndpointType>();

    public EditHistory History { get; } = new();

    public string UndoHint => History.UndoLabel is { } l
        ? $"{UiLanguage.Text("Ui.Undo", "Undo")}: {l}"
        : UiLanguage.Text("Ui.UndoEmpty", "Nothing to undo");

    public string RedoHint => History.RedoLabel is { } l
        ? $"{UiLanguage.Text("Ui.Redo", "Redo")}: {l}"
        : UiLanguage.Text("Ui.RedoEmpty", "Nothing to redo");

    private void OnHistoryChanged()
    {
        OnPropertyChanged(nameof(UndoHint));
        OnPropertyChanged(nameof(RedoHint));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        if (!_restoring)
            MarkDirty();
    }

    public void MarkDirty()
    {
        if (IsDirty) return;
        IsDirty = true;
        RefreshWindowTitle();
    }

    public void ClearDirty()
    {
        IsDirty = false;
        RefreshWindowTitle();
    }

    private void RefreshWindowTitle()
    {
        if (HostWindow is null) return;
        var name = string.IsNullOrWhiteSpace(Project.Name) ? "2Cut" : Project.Name;
        var file = _projectPath is null ? "" : $" — {Path.GetFileName(_projectPath)}";
        HostWindow.Title = IsDirty ? $"{name}*{file}" : $"{name}{file}";
    }

    /// <summary>Returns false if the user cancelled (keep the current document).</summary>
    public async Task<bool> ConfirmDiscardOrSaveAsync()
    {
        if (!IsDirty || HostWindow is null)
            return true;

        var choice = await ConfirmDialog.UnsavedChangesAsync(
            HostWindow,
            UiLanguage.Text("Ui.UnsavedMessage", "Save changes to the current project?"));
        if (choice == ConfirmUnsavedResult.Cancel)
            return false;
        if (choice == ConfirmUnsavedResult.Discard)
            return true;

        await SaveProjectAsync();
        return !IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => Replay(History.Undo, History.UndoLabel);

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => Replay(History.Redo, History.RedoLabel);

    private bool CanUndo() => History.CanUndo;
    private bool CanRedo() => History.CanRedo;

    private void Replay(Func<bool> step, string? label)
    {
        // Stays set through the refresh below: mirroring the model back into the panels must not
        // look like a fresh user edit.
        _restoring = true;
        bool ok;
        try
        {
            ok = step();
            if (ok)
            {
                // A reverted action can touch anything, so rebuild the views that mirror the model.
                _suppressTransformUi = true;
                LoadTransformFromSelection();
                _suppressTransformUi = false;
                PullDeviceUiFromSelection();
                RefreshFovOverlays();
                BumpCanvas();
            }
        }
        finally
        {
            _restoring = false;
        }

        if (ok)
            Log($"{UiLanguage.Text("Ui.Undo", "Undo")}/{UiLanguage.Text("Ui.Redo", "Redo")}: {label}");
    }

    /// <summary>Pointer released on the table: the next change starts a fresh history entry.</summary>
    public void EndGesture()
    {
        History.Break();
        _moduleEditId = null;
        _moduleGestureBefore = null;
    }

    private string? _moduleEditId;
    private ModuleState? _moduleGestureBefore;

    /// <summary>History entry captions, resolved when the entry is recorded.</summary>
    private static string Hist(string key, string fallback) => UiLanguage.Text($"Hist.{key}", fallback);

    private void Record(IEditAction action, string mergeKey)
    {
        if (_restoring) return;
        History.Push(action, mergeKey);
    }

    /// <summary>Records a module change the caller has already applied.</summary>
    private void RecordModuleEdit(DeviceModuleConfig cfg, ModuleState before, string label)
    {
        Record(
            new ValueEdit<ModuleState>(label, s => cfg.RestoreState(s), before, cfg.CaptureState()),
            $"module:{cfg.Id}");
    }

    /// <summary>Records the module chain's composition (add / remove / reorder).</summary>
    private void RecordChainEdit(ProjectorProfile device, List<DeviceModuleConfig> before, string label)
    {
        Record(
            new ValueEdit<List<DeviceModuleConfig>>(
                label,
                saved =>
                {
                    device.ModuleChain.Clear();
                    device.ModuleChain.AddRange(saved);
                },
                before,
                [.. device.ModuleChain]),
            $"chain:{device.Id}:{label}");
    }

    [ObservableProperty] public partial ProjectDocument Project { get; set; } = new() { Name = "2Cut" };
    public ObservableCollection<ProjectorProfile> Projectors { get; } = [];
    public ObservableCollection<ProjectionScene> Scenes { get; } = [];
    public ObservableCollection<ObjectListItem> ObjectItems { get; } = [];
    public ObservableCollection<FovOverlay> FovOverlays { get; } = [];
    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<ModuleItemViewModel> ModuleItems { get; } = [];

    public CalibrationPatternKind[] CalibPatternOptions { get; } =
        [CalibrationPatternKind.Dot, CalibrationPatternKind.Rect, CalibrationPatternKind.Grid];

    public IReadOnlyList<ModuleDescriptor> AvailableModuleTypes { get; } = ModuleRegistry.All;

    [ObservableProperty] public partial ProjectorProfile? SelectedProjector { get; set; }
    [ObservableProperty] public partial ModuleDescriptor? ModuleTypeToAdd { get; set; } = ModuleRegistry.All[0];
    [ObservableProperty] public partial ModuleItemViewModel? SelectedModuleItem { get; set; }
    [ObservableProperty] public partial ProjectionScene? SelectedScene { get; set; }
    [ObservableProperty] public partial int SelectedObjectIndex { get; set; } = -1;
    /// <summary>Sticky 3×3 docking used when opening/importing drawings (LT…RB, default center).</summary>
    [ObservableProperty] public partial string DockMode { get; set; } = "CM";
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    /// <summary>Raised when the canvas should re-fit zoom/pan (Clear, etc.).</summary>
    public event Action? ViewResetRequested;
    [ObservableProperty] public partial bool IsTreeOpen { get; set; }
    [ObservableProperty] public partial bool IsSceneOpen { get; set; }
    [ObservableProperty] public partial bool IsDevicesOpen { get; set; }
    [ObservableProperty] public partial bool IsTransformOpen { get; set; }
    [ObservableProperty] public partial bool IsCalibrateOpen { get; set; }
    [ObservableProperty] public partial bool IsLogsOpen { get; set; }
    [ObservableProperty] public partial bool IsAutomationOpen { get; set; }
    [ObservableProperty] public partial bool IsWorkFolderOpen { get; set; }
    [ObservableProperty] public partial string LanguageCode { get; set; } = "EN";
    [ObservableProperty] public partial CalibrationPatternKind CalibPattern { get; set; } = CalibrationPatternKind.Grid;
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsExportingIld { get; set; }
    [ObservableProperty] public partial bool IsDirty { get; set; }
    [ObservableProperty] public partial bool UseLayerColor { get; set; }
    [ObservableProperty] public partial int CanvasRevision { get; set; }
    [ObservableProperty] public partial bool MaskEnabled { get; set; }
    [ObservableProperty] public partial string FrameInfo { get; set; } = "No frame";
    [ObservableProperty] public partial bool UseVlt { get; set; }
    [ObservableProperty] public partial bool MeshEnabled { get; set; }
    [ObservableProperty] public partial bool ShowMeshOverlay { get; set; } = true;
    [ObservableProperty] public partial bool UdpEnabled { get; set; }
    [ObservableProperty] public partial int UdpPort { get; set; } = 11000;
    [ObservableProperty] public partial string WorkFolder { get; set; } = Environment.CurrentDirectory;
    [ObservableProperty] public partial RemoteEndpointType NewEndpointType { get; set; } = RemoteEndpointType.UdpBinary;
    [ObservableProperty] public partial string NewEndpointBindIp { get; set; } = "0.0.0.0";
    [ObservableProperty] public partial int NewEndpointPort { get; set; } = 11000;

    [ObservableProperty] public partial string DeviceName { get; set; } = "VLT-1";
    [ObservableProperty] public partial string DeviceHost { get; set; } = "192.168.0.10";
    [ObservableProperty] public partial int DevicePort { get; set; } = 10000;
    [ObservableProperty] public partial double FovWidthMm { get; set; } = 1000;
    [ObservableProperty] public partial double FovHeightMm { get; set; } = 1000;
    [ObservableProperty] public partial double PoseX { get; set; }
    [ObservableProperty] public partial double PoseY { get; set; }
    [ObservableProperty] public partial int ColorR { get; set; } = 255;
    [ObservableProperty] public partial int ColorG { get; set; }
    [ObservableProperty] public partial int ColorB { get; set; }

    [ObservableProperty] public partial double Tx { get; set; }
    [ObservableProperty] public partial double Ty { get; set; }
    [ObservableProperty] public partial double Tz { get; set; }
    [ObservableProperty] public partial double Rot { get; set; }
    [ObservableProperty] public partial double Scale { get; set; } = 1;

    [ObservableProperty] public partial int MeshColumns { get; set; } = 3;
    [ObservableProperty] public partial int MeshRows { get; set; } = 3;
    [ObservableProperty] public partial int MeshPointCol { get; set; }
    [ObservableProperty] public partial int MeshPointRow { get; set; }
    [ObservableProperty] public partial double MeshPointX { get; set; }
    [ObservableProperty] public partial double MeshPointY { get; set; }

    [ObservableProperty] public partial string SceneName { get; set; } = "Scene";
    [ObservableProperty] public partial double SceneWidthMm { get; set; } = 1000;
    [ObservableProperty] public partial double SceneHeightMm { get; set; } = 1000;

    public ObservableCollection<SceneProjectorItem> SceneProjectorItems { get; } = [];

    /// <summary>Geometry the selected device's modules draw on the table.</summary>
    public ObservableCollection<ModuleOverlay> ModuleOverlays { get; } = [];

    private DeviceModuleConfig? ActiveMeshModule =>
        ModuleTypes.FindMesh((SelectedProjector ?? Projectors.FirstOrDefault())?.ModuleChain ?? []);

    public CalibrationMesh? ActiveMesh => ActiveMeshModule?.Mesh;
    public string MeshOwnerLabel => $"Mesh: {SelectedProjector?.DisplayName ?? "—"}";

    public string ActiveDeviceName =>
        Projectors.Count == 0
            ? _virtual.DisplayName
            : string.Join(" · ", Projectors.Select(p =>
                $"{p.DisplayName}{(UseVlt && _vlts.ContainsKey(p.Id) ? "*" : "")} {p.Host}:{p.Port}"));

    public string PipelineHelp =>
        "N projectors. Per device: FOV split → module chain in list order (the mesh is one of them) → laser.";

    public bool CanRemoveProjector => Projectors.Count > 1;
    public Window? HostWindow { get; set; }

    partial void OnSelectedProjectorChanged(ProjectorProfile? value)
    {
        if (_suppressDeviceUi || value is null) return;
        PullDeviceUiFromSelection();
        OnPropertyChanged(nameof(ActiveMesh));
        OnPropertyChanged(nameof(MeshOwnerLabel));
        Log($"Selected {value.DisplayName}");
        BumpCanvas();
    }

    partial void OnMaskEnabledChanged(bool value)
    {
        if (SelectedScene is null) return;
        SelectedScene.Mask.IsEnabled = value;
        if (value && SelectedScene.Mask.Bounds.Width <= 0)
            SelectedScene.Mask.Bounds = new Rect2(0, 0, SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
        BumpCanvas();
    }

    partial void OnSelectedSceneChanged(ProjectionScene? value)
    {
        if (value is not null)
        {
            var idx = Project.Scenes.IndexOf(value);
            if (idx >= 0)
                Project.ActiveSceneIndex = idx;
        }
        PullSceneUiFromSelection();
        if (value is not null)
            MaskEnabled = value.Mask.IsEnabled;
        RefreshObjectNames();
        RefreshModuleOverlays();
        RefreshFovOverlays();
        BumpCanvas();
        ViewResetRequested?.Invoke();
        RemoveSceneCommand.NotifyCanExecuteChanged();
    }

    partial void OnUseLayerColorChanged(bool value)
    {
        Project.ColorMode = value ? LaserColorMode.LayerColor : LaserColorMode.SolidSceneColor;
        MarkDirty();
        BumpCanvas();
    }

    [RelayCommand]
    private void AddScene()
    {
        var n = Project.Scenes.Count + 1;
        var scene = new ProjectionScene { Name = $"Scene {n}" };
        scene.Target.WidthMm = SelectedScene?.Target.WidthMm ?? 1000;
        scene.Target.HeightMm = SelectedScene?.Target.HeightMm ?? 1000;
        Project.Scenes.Add(scene);
        Scenes.Add(scene);
        SelectedScene = scene;
        RemoveSceneCommand.NotifyCanExecuteChanged();
        MarkDirty();
        Log($"Added {scene.Name}");
    }

    [RelayCommand(CanExecute = nameof(CanRemoveScene))]
    private void RemoveScene()
    {
        if (SelectedScene is null || Project.Scenes.Count <= 1) return;
        var doomed = SelectedScene;
        var idx = Project.Scenes.IndexOf(doomed);
        Project.Scenes.Remove(doomed);
        Scenes.Remove(doomed);
        SelectedScene = Project.Scenes[Math.Clamp(idx, 0, Project.Scenes.Count - 1)];
        Project.ActiveSceneIndex = Project.Scenes.IndexOf(SelectedScene!);
        RemoveSceneCommand.NotifyCanExecuteChanged();
        MarkDirty();
        Log($"Removed {doomed.Name}");
    }

    private bool CanRemoveScene() => Project.Scenes.Count > 1;

    partial void OnSceneNameChanged(string value)
    {
        if (_suppressSceneUi || SelectedScene is null) return;
        SelectedScene.Name = value;
        MarkDirty();
    }

    partial void OnSceneWidthMmChanged(double value) => ApplySceneSize();
    partial void OnSceneHeightMmChanged(double value) => ApplySceneSize();

    private void ApplySceneSize()
    {
        if (_suppressSceneUi || SelectedScene is null) return;
        var w = Math.Max(1, SceneWidthMm);
        var h = Math.Max(1, SceneHeightMm);
        SelectedScene.Target.WidthMm = w;
        SelectedScene.Target.HeightMm = h;
        if (SelectedScene.Mask.IsEnabled)
            SelectedScene.Mask.Bounds = new Rect2(0, 0, w, h);
        if (Projectors.Count == 1)
            Projectors[0].FitFovToScene(w, h);
        else
            LayoutFovs();
        RefreshFovOverlays();
        MarkDirty();
        BumpCanvas();
        ViewResetRequested?.Invoke();
    }

    private void PullSceneUiFromSelection()
    {
        if (SelectedScene is null) return;
        _suppressSceneUi = true;
        SceneName = SelectedScene.Name;
        SceneWidthMm = SelectedScene.Target.WidthMm;
        SceneHeightMm = SelectedScene.Target.HeightMm;
        _suppressSceneUi = false;
        RefreshSceneProjectorItems();
    }

    private void RefreshSceneProjectorItems()
    {
        SceneProjectorItems.Clear();
        if (SelectedScene is null) return;
        var bound = SelectedScene.BoundProjectorIds;
        var allBound = bound.Count == 0;
        foreach (var p in Projectors)
        {
            var isBound = allBound || bound.Contains(p.Id);
            SceneProjectorItems.Add(new SceneProjectorItem(p, isBound, SetProjectorBoundToScene));
        }
    }

    private void SetProjectorBoundToScene(string projectorId, bool isBound)
    {
        if (SelectedScene is null || _suppressSceneUi) return;

        // Materialize "all" into an explicit list the first time the user toggles one.
        if (SelectedScene.BoundProjectorIds.Count == 0)
            SelectedScene.BoundProjectorIds.AddRange(Projectors.Select(p => p.Id));

        if (isBound)
        {
            if (!SelectedScene.BoundProjectorIds.Contains(projectorId))
                SelectedScene.BoundProjectorIds.Add(projectorId);
        }
        else
            SelectedScene.BoundProjectorIds.Remove(projectorId);

        // Empty list again means "all projectors" — refresh ticks to match.
        if (SelectedScene.BoundProjectorIds.Count == 0
            || SelectedScene.BoundProjectorIds.Count == Projectors.Count)
        {
            SelectedScene.BoundProjectorIds.Clear();
            _suppressSceneUi = true;
            RefreshSceneProjectorItems();
            _suppressSceneUi = false;
        }

        RefreshFovOverlays();
        BumpCanvas();
    }

    /// <summary>Projectors that play / split on the active scene.</summary>
    private List<ProjectorProfile> GetSceneProjectors()
    {
        if (SelectedScene is null) return [];
        var ids = SelectedScene.BoundProjectorIds;
        if (ids.Count == 0) return Projectors.ToList();
        return Projectors.Where(p => ids.Contains(p.Id)).ToList();
    }

    partial void OnMeshEnabledChanged(bool value)
    {
        if (_suppressDeviceUi) return;
        if (ActiveMeshModule is not { } module) return;
        var before = module.CaptureState();
        module.IsEnabled = value;
        if (module.Mesh is not null)
            module.Mesh.IsEnabled = value;
        RecordModuleEdit(module, before, Hist("MeshEnabled", "Mesh on/off"));
        RefreshModuleItems();
        BumpCanvas();
    }

    partial void OnShowMeshOverlayChanged(bool value)
    {
        if (!_suppressDeviceUi && ActiveMeshModule is { } module)
        {
            var before = module.CaptureState();
            module.ShowOnTable = value;
            RecordModuleEdit(module, before, Hist("MeshOverlay", "Mesh on table"));
        }
        RefreshModuleOverlays();
        BumpCanvas();
    }

    partial void OnMeshColumnsChanged(int value)
    {
        if (_suppressMeshUi || ActiveMeshModule is not { Mesh: { } grid } module) return;
        var before = module.CaptureState();
        grid.Resize(Math.Max(1, value), grid.Rows);
        MeshPointCol = Math.Min(MeshPointCol, grid.Columns);
        RecordModuleEdit(module, before, Hist("MeshSize", "Mesh size"));
        SyncMeshPointFromSelection();
        BumpCanvas();
    }

    partial void OnMeshRowsChanged(int value)
    {
        if (_suppressMeshUi || ActiveMeshModule is not { Mesh: { } grid } module) return;
        var before = module.CaptureState();
        grid.Resize(grid.Columns, Math.Max(1, value));
        MeshPointRow = Math.Min(MeshPointRow, grid.Rows);
        RecordModuleEdit(module, before, Hist("MeshSize", "Mesh size"));
        SyncMeshPointFromSelection();
        BumpCanvas();
    }

    partial void OnMeshPointColChanged(int value) => SyncMeshPointFromSelection();
    partial void OnMeshPointRowChanged(int value) => SyncMeshPointFromSelection();
    partial void OnSelectedModuleItemChanged(ModuleItemViewModel? value) => RefreshModuleOverlays();
    partial void OnMeshPointXChanged(double value) => PushSelectedMeshPoint();
    partial void OnMeshPointYChanged(double value) => PushSelectedMeshPoint();

    partial void OnDeviceNameChanged(string value)
    {
        if (_suppressDeviceUi || SelectedProjector is null) return;
        SelectedProjector.DisplayName = value;
        OnPropertyChanged(nameof(MeshOwnerLabel));
        OnPropertyChanged(nameof(ActiveDeviceName));
        RefreshFovOverlays();
        BumpCanvas();
    }

    partial void OnDeviceHostChanged(string value)
    {
        if (_suppressDeviceUi || SelectedProjector is null) return;
        SelectedProjector.Host = value;
        OnPropertyChanged(nameof(ActiveDeviceName));
    }

    partial void OnDevicePortChanged(int value)
    {
        if (_suppressDeviceUi || SelectedProjector is null) return;
        SelectedProjector.Port = value;
        OnPropertyChanged(nameof(ActiveDeviceName));
    }

    partial void OnFovWidthMmChanged(double value) => PushPoseFovToSelection();
    partial void OnFovHeightMmChanged(double value) => PushPoseFovToSelection();
    partial void OnPoseXChanged(double value) => PushPoseFovToSelection();
    partial void OnPoseYChanged(double value) => PushPoseFovToSelection();
    partial void OnColorRChanged(int value) => PushColorToSelection();
    partial void OnColorGChanged(int value) => PushColorToSelection();
    partial void OnColorBChanged(int value) => PushColorToSelection();

    partial void OnSelectedObjectIndexChanged(int value) => LoadTransformFromSelection();
    partial void OnTxChanged(double value) => ApplyTransformToSelection();
    partial void OnTyChanged(double value) => ApplyTransformToSelection();
    partial void OnTzChanged(double value) => ApplyTransformToSelection();
    partial void OnRotChanged(double value) => ApplyTransformToSelection();
    partial void OnScaleChanged(double value) => ApplyTransformToSelection();

    [RelayCommand] private void OpenTree() => IsTreeOpen = !IsTreeOpen;
    [RelayCommand] private void OpenScenePanel() => IsSceneOpen = !IsSceneOpen;
    [RelayCommand] private void OpenDevices() => IsDevicesOpen = !IsDevicesOpen;
    [RelayCommand] private void OpenAutomation() => IsAutomationOpen = !IsAutomationOpen;
    [RelayCommand] private void OpenWorkFolder() => IsWorkFolderOpen = !IsWorkFolderOpen;
    [RelayCommand] private void OpenLogs() => IsLogsOpen = !IsLogsOpen;
    [RelayCommand] private void ClearLogs() => LogLines.Clear();

    [RelayCommand]
    private void ToggleLanguage()
    {
        var code = UiLanguage.Toggle();
        LanguageCode = code.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? "RU" : "EN";
        OnHistoryChanged();
        Log($"Language: {LanguageCode}");
    }

    [RelayCommand]
    private void OpenCalibrate()
    {
        IsCalibrateOpen = !IsCalibrateOpen;
        if (IsCalibrateOpen)
        {
            ShowMeshOverlay = true;
            MeshEnabled = true;
            if (ModuleItems.FirstOrDefault(i => i.TypeId == ModuleTypes.Mesh) is { } meshItem)
                SelectedModuleItem = meshItem;
            IsDevicesOpen = true;
            Log($"Calibrate mesh of {SelectedProjector?.DisplayName ?? "?"}");
        }
    }

    [RelayCommand]
    private void OpenTransform()
    {
        IsTransformOpen = !IsTransformOpen;
        if (IsTransformOpen) LoadTransformFromSelection();
    }

    [RelayCommand]
    private void AddProjector()
    {
        var n = Projectors.Count + 1;
        var p = ProjectorProfile.CreateDefault($"VLT-{n}", $"192.168.0.{9 + n}", 10000 + (n - 1));
        Projectors.Add(p);
        LayoutFovs();
        SelectedProjector = p;
        OnPropertyChanged(nameof(CanRemoveProjector));
        OnPropertyChanged(nameof(ActiveDeviceName));
        RefreshSceneProjectorItems();
        MarkDirty();
        Log($"Added {p.DisplayName}");
    }

    [RelayCommand]
    private async Task RemoveProjectorAsync()
    {
        if (SelectedProjector is null || Projectors.Count <= 1) return;
        var victim = SelectedProjector;
        await DisconnectProjectorAsync(victim.Id);
        var idx = Projectors.IndexOf(victim);
        Projectors.Remove(victim);
        foreach (var scene in Scenes)
            scene.BoundProjectorIds.Remove(victim.Id);
        SelectedProjector = Projectors[Math.Clamp(idx, 0, Projectors.Count - 1)];
        LayoutFovs();
        OnPropertyChanged(nameof(CanRemoveProjector));
        OnPropertyChanged(nameof(ActiveDeviceName));
        RefreshSceneProjectorItems();
        MarkDirty();
        Log($"Removed {victim.DisplayName}");
    }

    [RelayCommand]
    private void AddModule()
    {
        if (SelectedProjector is null) return;
        var descriptor = ModuleTypeToAdd ?? ModuleRegistry.All[0];
        var mod = ModuleRegistry.CreateConfig(descriptor.TypeId);
        var before = SelectedProjector.ModuleChain.ToList();
        SelectedProjector.ModuleChain.Add(mod);
        RecordChainEdit(SelectedProjector, before, $"{Hist("Add", "Add")} {mod.DisplayName}");
        RefreshModuleItems();
        SelectedModuleItem = ModuleItems.LastOrDefault();
        BumpCanvas();
        Log($"Added module {mod.DisplayName} → {SelectedProjector.DisplayName}");
    }

    [RelayCommand]
    private void RemoveModule()
    {
        if (SelectedProjector is null || SelectedModuleItem is null) return;
        var before = SelectedProjector.ModuleChain.ToList();
        var removed = SelectedModuleItem.Model;
        SelectedProjector.ModuleChain.Remove(removed);
        RecordChainEdit(SelectedProjector, before, $"{Hist("Remove", "Remove")} {removed.DisplayName}");
        RefreshModuleItems();
        BumpCanvas();
        Log("Module removed");
    }

    [RelayCommand]
    private void MoveModuleUp()
    {
        if (SelectedProjector is null || SelectedModuleItem is null) return;
        var list = SelectedProjector.ModuleChain;
        var i = list.IndexOf(SelectedModuleItem.Model);
        if (i <= 0) return;
        var before = list.ToList();
        (list[i - 1], list[i]) = (list[i], list[i - 1]);
        RecordChainEdit(SelectedProjector, before, Hist("Reorder", "Reorder modules"));
        RefreshModuleItems();
        SelectedModuleItem = ModuleItems[i - 1];
        BumpCanvas();
    }

    [RelayCommand]
    private void MoveModuleDown()
    {
        if (SelectedProjector is null || SelectedModuleItem is null) return;
        var list = SelectedProjector.ModuleChain;
        var i = list.IndexOf(SelectedModuleItem.Model);
        if (i < 0 || i >= list.Count - 1) return;
        var before = list.ToList();
        (list[i + 1], list[i]) = (list[i], list[i + 1]);
        RecordChainEdit(SelectedProjector, before, Hist("Reorder", "Reorder modules"));
        RefreshModuleItems();
        SelectedModuleItem = ModuleItems[i + 1];
        BumpCanvas();
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (HostWindow is null) return;
        var files = await HostWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open drawing",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("DXF / SVG") { Patterns = ["*.dxf", "*.svg"] },
                new FilePickerFileType("DXF") { Patterns = ["*.dxf"] },
                new FilePickerFileType("SVG") { Patterns = ["*.svg"] }
            ]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path is null) return;
        await ImportPathAsync(path, clear: true, play: false);
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (HostWindow is null) return;
        if (_projectPath is null)
        {
            var file = await HostWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save project",
                DefaultExtension = "cproj",
                FileTypeChoices = [new FilePickerFileType("2Cut project") { Patterns = ["*.cproj"] }]
            });
            if (file is null) return;
            _projectPath = file.TryGetLocalPath();
            if (_projectPath is null) return;
        }

        try
        {
            PushDeviceFieldsToSelection();
            Project.Devices = Projectors.Select(DeviceSnapshot.From).ToList();
            // Mesh lives only inside each device's module chain now.
            Project.CalibrationMesh = null;
            await ProjectJsonStore.SaveAsync(Project, _projectPath);
            ClearDirty();
            Log($"Saved {_projectPath} ({Project.Devices.Count} devices)");
        }
        catch (Exception ex)
        {
            Log($"Save failed: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportIld))]
    private async Task ExportIldAsync()
    {
        if (HostWindow is null || SelectedScene is null) return;
        var device = SelectedProjector ?? Projectors.FirstOrDefault();
        if (device is null) return;
        var file = await HostWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export ILDA",
            DefaultExtension = "ild",
            FileTypeChoices = [new FilePickerFileType("ILDA format 5") { Patterns = ["*.ild"] }]
        });
        if (file is null) return;
        var path = file.TryGetLocalPath();
        if (path is null) return;

        PushDeviceFieldsToSelection();
        var scene = SelectedScene;
        var project = Project;
        var width = device.WidthResolution;
        var height = device.HeightResolution;
        var r = device.Red;
        var g = device.Green;
        var b = device.Blue;
        var a = device.Alpha;
        var deviceName = device.DisplayName;
        var frameName = Path.GetFileNameWithoutExtension(path);

        _ildExportCts?.Cancel();
        _ildExportCts?.Dispose();
        _ildExportCts = new CancellationTokenSource();
        var ct = _ildExportCts.Token;

        IsExportingIld = true;
        StatusText = UiLanguage.Text("Ui.ExportIldBusy", "Exporting .ild…");
        Log($"ILDA export started → {path}");
        try
        {
            var ilda = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var frame = _pipeline.BuildFrame(scene, project, device);
                ct.ThrowIfCancellationRequested();
                var result = IldaEncoder.FromNormalizedLines(frame, width, height, r, g, b, a, ct);
                result.FrameName = frameName;
                return result;
            }, ct);

            await IldaFileWriter.WriteAsync(path, ilda, ct);
            Log($"Exported ILDA via {deviceName} ({ilda.Points.Count} pts)");
        }
        catch (OperationCanceledException)
        {
            TryDeletePartialExport(path);
            Log("ILDA export cancelled");
        }
        catch (Exception ex)
        {
            TryDeletePartialExport(path);
            Log($"ILDA export failed: {ex.Message}");
        }
        finally
        {
            IsExportingIld = false;
            _ildExportCts?.Dispose();
            _ildExportCts = null;
        }
    }

    private bool CanExportIld() => !IsExportingIld;

    [RelayCommand(CanExecute = nameof(CanCancelExportIld))]
    private void CancelExportIld()
    {
        _ildExportCts?.Cancel();
        Log("ILDA export cancel requested…");
    }

    private bool CanCancelExportIld() => IsExportingIld;

    partial void OnIsExportingIldChanged(bool value)
    {
        ExportIldCommand.NotifyCanExecuteChanged();
        CancelExportIldCommand.NotifyCanExecuteChanged();
    }

    private static void TryDeletePartialExport(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            /* ignore cleanup failures */
        }
    }

    [RelayCommand]
    private async Task LoadProjectAsync()
    {
        if (HostWindow is null) return;
        if (!await ConfirmDiscardOrSaveAsync())
            return;
        var files = await HostWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open project",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("2Cut project") { Patterns = ["*.cproj"] }]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path is null) return;

        try
        {
            Project = await ProjectJsonStore.LoadAsync(path);
            _projectPath = path;
            Scenes.Clear();
            foreach (var s in Project.Scenes)
                Scenes.Add(s);
            SelectedScene = Project.ActiveScene;
            MaskEnabled = SelectedScene.Mask.IsEnabled;
            UseLayerColor = Project.ColorMode == LaserColorMode.LayerColor;
            await ApplyLoadedDevicesAsync(Project);
            RefreshObjectNames();
            RefreshFovOverlays();
            History.Clear();
            ClearDirty();
            BumpCanvas();
            Log($"Loaded {path}");
        }
        catch (Exception ex)
        {
            Log($"Load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AlignObject(string? mode)
    {
        // Sticky dock setting from the Transform panel — used on the next import too.
        if (!string.IsNullOrEmpty(mode))
            DockMode = mode;

        var d = GetSelectedDrawable();
        if (d is null || SelectedScene is null) return;

        var before = DrawableTransform.Read(d);
        ApplyDock(d, SelectedScene.Target, DockMode);
        Record(
            new ValueEdit<DrawableTransform>(
                $"{Hist("Align", "Align")} {d.Name}",
                v => v.ApplyTo(d),
                before,
                DrawableTransform.Read(d)),
            $"drawable:{d.Id}:align");

        SyncTransformPanel();
        BumpCanvas();
        Log($"Aligned {d.Name} → {DockMode}");
    }

    /// <summary>
    /// Edge-button snap: parks the selection (or all objects) without changing <see cref="DockMode"/>.
    /// </summary>
    [RelayCommand]
    private void SnapObject(string? mode)
    {
        if (SelectedScene is null || string.IsNullOrEmpty(mode)) return;
        var targets = GetSnapTargets();
        if (targets.Count == 0) return;
        if (!ObjectAligner.TryGetGroupBounds(targets, out var group)) return;

        var (h, v) = ParseDock(mode);
        var delta = ObjectAligner.DeltaToAlign(group, SelectedScene.Target, h, v);
        if (Math.Abs(delta.X) < 1e-9 && Math.Abs(delta.Y) < 1e-9) return;

        var steps = new List<IEditAction>();
        foreach (var d in targets)
        {
            var before = DrawableTransform.Read(d);
            d.Translation = new Point3(
                d.Translation.X + delta.X,
                d.Translation.Y + delta.Y,
                d.Translation.Z);
            steps.Add(new ValueEdit<DrawableTransform>(
                d.Name, x => x.ApplyTo(d), before, DrawableTransform.Read(d)));
        }

        Record(new CompositeEdit($"{Hist("Snap", "Snap")} {mode}", steps), $"snap:{mode}");
        History.Break();
        SyncTransformPanel();
        BumpCanvas();
        Log($"Snap {mode} ({targets.Count})");
    }

    [RelayCommand]
    private void RotateObject90()
    {
        if (SelectedScene is null) return;
        var targets = GetSnapTargets();
        if (targets.Count == 0) return;

        var steps = new List<IEditAction>();
        foreach (var d in targets)
        {
            var before = DrawableTransform.Read(d);
            d.RotationDeg += 90;
            // Same as legacy: after a 90° turn, re-park to the sticky dock setting.
            ApplyDock(d, SelectedScene.Target, DockMode);
            steps.Add(new ValueEdit<DrawableTransform>(
                d.Name, x => x.ApplyTo(d), before, DrawableTransform.Read(d)));
        }

        Record(new CompositeEdit(Hist("Rotate90", "Rotate 90°"), steps), "rotate90");
        History.Break();
        SyncTransformPanel();
        BumpCanvas();
        Log($"Rotated 90° → dock {DockMode}");
    }

    [RelayCommand]
    private void ClearScene()
    {
        if (SelectedScene is null) return;
        SelectedScene.Drawables.Clear();
        SelectedScene.Mask.Bounds = new Rect2(0, 0, SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
        SelectedObjectIndex = -1;
        History.Clear();
        RefreshObjectNames();
        MarkDirty();
        BumpCanvas();
        ViewResetRequested?.Invoke();
        Log("Scene cleared");
    }

    private List<Drawable> GetSnapTargets()
    {
        if (SelectedScene is null) return [];
        var selected = GetSelectedDrawable();
        if (selected is not null) return [selected];
        return SelectedScene.Drawables.Where(d => d.IsVisible).ToList();
    }

    private void SyncTransformPanel()
    {
        _suppressTransformUi = true;
        LoadTransformFromSelection();
        _suppressTransformUi = false;
    }

    private static void ApplyDock(Drawable drawable, PlaneTarget plane, string mode)
    {
        var (h, v) = ParseDock(mode);
        ObjectAligner.Align(drawable, plane, h, v);
    }

    private static (AlignH H, AlignV V) ParseDock(string? mode) => mode switch
    {
        "LT" => (AlignH.Left, AlignV.Top),
        "CT" => (AlignH.Center, AlignV.Top),
        "RT" => (AlignH.Right, AlignV.Top),
        "LM" => (AlignH.Left, AlignV.Middle),
        "RM" => (AlignH.Right, AlignV.Middle),
        "LB" => (AlignH.Left, AlignV.Bottom),
        "CB" => (AlignH.Center, AlignV.Bottom),
        "RB" => (AlignH.Right, AlignV.Bottom),
        _ => (AlignH.Center, AlignV.Middle)
    };

    /// <summary>Table drag of a module handle: move it, write it back to the config, re-render.</summary>
    public void ApplyModuleAnchor(string moduleId, int anchorIndex, Point2 unit)
    {
        var device = SelectedProjector ?? Projectors.FirstOrDefault();
        if (device?.ModuleChain.FirstOrDefault(m => m.Id == moduleId) is not { } cfg)
            return;
        if (ModuleRegistry.Materialize(cfg) is not IRenderableModule module)
            return;

        // One history entry per gesture: snapshot on the first move, then keep overwriting it.
        if (_moduleEditId != cfg.Id)
        {
            _moduleEditId = cfg.Id;
            _moduleGestureBefore = cfg.CaptureState();
        }

        _selectedAnchor = anchorIndex;
        if (module.MoveAnchor(anchorIndex, unit))
        {
            ModuleRegistry.Capture(module, cfg);
            if (cfg.TypeId == ModuleTypes.Mesh)
                SyncMeshUiFromAnchor(anchorIndex);
            if (_moduleGestureBefore is not null)
                RecordModuleEdit(cfg, _moduleGestureBefore, $"{Hist("Drag", "Drag")} {cfg.DisplayName}");
        }

        if (ModuleItems.FirstOrDefault(i => i.Model.Id == moduleId) is { } item && !ReferenceEquals(item, SelectedModuleItem))
            SelectedModuleItem = item;

        RefreshModuleOverlays();
        BumpCanvas();
    }

    private void SyncMeshUiFromAnchor(int anchorIndex)
    {
        if (ActiveMesh is null) return;
        var stride = ActiveMesh.Columns + 1;
        _suppressMeshUi = true;
        MeshPointCol = anchorIndex % stride;
        MeshPointRow = anchorIndex / stride;
        var p = ActiveMesh.GetPoint(MeshPointCol, MeshPointRow);
        MeshPointX = p.X;
        MeshPointY = p.Y;
        _suppressMeshUi = false;
        if (!MeshEnabled)
            MeshEnabled = true;
    }

    /// <summary>Table drag of an object: select it and keep the transform panel in step.</summary>
    public void ApplyDrawableDrag(int index, Point3 from, Point3 to)
    {
        if (SelectedScene is null || index < 0 || index >= SelectedScene.Drawables.Count)
            return;
        if (SelectedObjectIndex != index)
            SelectedObjectIndex = index;

        // The drawable already carries the new translation; pushing it back through the spinners
        // one axis at a time would briefly write a half-updated position.
        _suppressTransformUi = true;
        Tx = to.X;
        Ty = to.Y;
        Tz = to.Z;
        _suppressTransformUi = false;

        var d = SelectedScene.Drawables[index];
        var baseline = DrawableTransform.Read(d);
        Record(
            new ValueEdit<DrawableTransform>(
                $"{Hist("Move", "Move")} {d.Name}",
                v => v.ApplyTo(d),
                baseline.With(from),
                baseline.With(to)),
            $"drawable:{d.Id}:move");
        BumpCanvas();
    }

    public void ApplyMaskBounds(Rect2 from, Rect2 to)
    {
        if (SelectedScene is null) return;
        var mask = SelectedScene.Mask;
        mask.Bounds = to;
        MaskEnabled = true;
        Record(
            new ValueEdit<Rect2>(Hist("Mask", "Mask bounds"), v => mask.Bounds = v, from, to),
            "mask:bounds");
        BumpCanvas();
    }

    [RelayCommand]
    private async Task PlayCalibrationPatternAsync()
    {
        if (SelectedScene is null) return;
        var device = SelectedProjector ?? Projectors.FirstOrDefault();
        if (device is null) return;
        try
        {
            PushDeviceFieldsToSelection();
            MeshEnabled = true;
            ShowMeshOverlay = true;
            var pattern = CalibrationPatternBuilder.Create(
                SelectedScene.Target, CalibPattern, MeshColumns, MeshRows);
            var bags = _pipeline.BuildPerDevice(pattern, Project, [device]);
            await SendBagsAsync(bags, $"Calib {CalibPattern} → {device.DisplayName}");
        }
        catch (Exception ex)
        {
            Log($"Calib play failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (HostWindow is null || SelectedProjector is null) return;
        var file = await HostWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save device profile",
            DefaultExtension = "cdev",
            FileTypeChoices = [new FilePickerFileType("2Cut device") { Patterns = ["*.cdev"] }]
        });
        if (file is null) return;
        var path = file.TryGetLocalPath();
        if (path is null) return;
        try
        {
            PushDeviceFieldsToSelection();
            await ProfileJsonStore.SaveAsync(SelectedProjector, path);
            Log($"Device saved {SelectedProjector.DisplayName}");
        }
        catch (Exception ex)
        {
            Log($"Profile save failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        if (HostWindow is null || SelectedProjector is null) return;
        var files = await HostWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load device profile",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("2Cut device") { Patterns = ["*.cdev"] }]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path is null) return;
        try
        {
            await ProfileJsonStore.LoadIntoAsync(SelectedProjector, path);
            PullDeviceUiFromSelection();
            RefreshFovOverlays();
            BumpCanvas();
            Log($"Device loaded into {SelectedProjector.DisplayName}");
            OnPropertyChanged(nameof(ActiveDeviceName));
        }
        catch (Exception ex)
        {
            Log($"Profile load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleSelectedLayerVisibility()
    {
        var d = GetSelectedDrawable();
        if (d is null) return;
        var layer = d.LayerName;
        var targets = SelectedScene!.Drawables
            .Where(x => string.Equals(x.LayerName, layer, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (targets.Count == 0) return;
        var turnOn = targets.Any(x => !x.IsVisible);
        foreach (var t in targets)
            t.IsVisible = turnOn;
        RefreshObjectNames();
        BumpCanvas();
    }

    [RelayCommand]
    private async Task ConnectSelectedAsync()
    {
        if (SelectedProjector is null) return;
        try
        {
            PushDeviceFieldsToSelection();
            UseVlt = true;
            await DisconnectProjectorAsync(SelectedProjector.Id);
            var vlt = new VltProjector(SelectedProjector);
            await vlt.ConnectAsync();
            _vlts[SelectedProjector.Id] = vlt;
            Log($"Connected {SelectedProjector.DisplayName} {SelectedProjector.Host}:{SelectedProjector.Port}");
            OnPropertyChanged(nameof(ActiveDeviceName));
        }
        catch (Exception ex)
        {
            Log($"Connect failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UseVirtualAsync()
    {
        UseVlt = false;
        foreach (var id in _vlts.Keys.ToList())
            await DisconnectProjectorAsync(id);
        await _virtual.ConnectAsync();
        Log("Using Virtual Projector");
        OnPropertyChanged(nameof(ActiveDeviceName));
    }

    [RelayCommand]
    private async Task ToggleUdpAsync()
    {
        try
        {
            _hub.WorkFolder = WorkFolder;
            var binary = _hub.Endpoints.FirstOrDefault(e => e.Type == RemoteEndpointType.UdpBinary)
                         ?? _hub.AddEndpoint(RemoteEndpointType.UdpBinary, "0.0.0.0", UdpPort);
            binary.Port = UdpPort;
            if (UdpEnabled)
            {
                await _hub.StopEndpointAsync(binary);
                UdpEnabled = false;
                Log("UDP Binary stopped");
            }
            else
            {
                await _hub.StartEndpointAsync(binary);
                UdpEnabled = true;
                Log($"UDP Binary listening on {binary.Port}");
            }
            RefreshEndpointItems();
        }
        catch (Exception ex)
        {
            UdpEnabled = false;
            Log($"UDP failed: {ex.Message}");
        }
    }

    partial void OnWorkFolderChanged(string value)
    {
        _hub.WorkFolder = value;
        if (!_suppressWorkFolderSync)
            WorkFolderBrowser.SyncFromExternal(value);
    }

    [RelayCommand]
    private void AddAutomationEndpoint()
    {
        _hub.AddEndpoint(NewEndpointType, NewEndpointBindIp, NewEndpointPort);
        RefreshEndpointItems();
        Log($"Added {NewEndpointType} :{NewEndpointPort}");
    }

    private void RefreshEndpointItems()
    {
        EndpointItems.Clear();
        foreach (var ep in _hub.Endpoints)
            EndpointItems.Add(new EndpointItemViewModel(_hub, ep, () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RefreshEndpointItems();
                    SyncUdpEnabledFromHub();
                });
            }));
        SyncUdpEnabledFromHub();
    }

    private void SyncUdpEnabledFromHub()
    {
        var binary = _hub.Endpoints.FirstOrDefault(e => e.Type == RemoteEndpointType.UdpBinary);
        UdpEnabled = binary is not null && _hub.IsListening(binary);
    }

    private void OnRemoteCommand(object? sender, RemoteCommand e)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var cmds = e.Commands.Select(c => c.ToUpperInvariant()).ToHashSet();
                // Also fold Kind into flags for text/json single-command packets.
                if (e.Kind == RemoteCommandKind.Play) cmds.Add("PLAY");
                if (e.Kind == RemoteCommandKind.Stop) cmds.Add("OFF");
                if (e.Kind == RemoteCommandKind.Clear) cmds.Add("CLEAR");
                if (e.Kind == RemoteCommandKind.Align) cmds.Add("ALIGN");

                Log($"Auto {e.Transport} {e.Header} cmds=[{string.Join('&', e.Commands)}] path={e.Path ?? "-"}");

                if (cmds.Contains("ALIGN"))
                    Log("ALIGN received (not wired yet)");

                var clear = cmds.Contains("CLEAR");
                var play = cmds.Contains("PLAY") || cmds.Contains("SHOW");
                var stop = cmds.Contains("OFF") || cmds.Contains("STOP");

                if (e.Kind == RemoteCommandKind.LoadGeometry && e.Geometry is { } geo)
                {
                    await ApplyRemoteGeometryAsync(geo, clear, play);
                }
                else if ((e.Kind == RemoteCommandKind.LoadFile || e.Header is "Filename" or "Filepath")
                         && !string.IsNullOrWhiteSpace(e.Path))
                {
                    await ImportPathAsync(e.Path!, clear, play);
                }
                else
                {
                    if (clear && SelectedScene is not null)
                    {
                        SelectedScene.Drawables.Clear();
                        RefreshObjectNames();
                        MarkDirty();
                        BumpCanvas();
                    }
                    if (play)
                        await PlayAsync();
                }

                if (stop)
                    await StopAsync();

                if (e.ReplyRequested)
                    await _hub.SendReplyAsync(e, "OK");
            }
            catch (Exception ex)
            {
                Log($"Automation handle failed: {ex.Message}");
                if (e.ReplyRequested)
                    await _hub.SendReplyAsync(e, $"ERR:{ex.Message}");
            }
        });
    }

    private async Task ApplyRemoteGeometryAsync(RemoteGeometryPayload geo, bool clear, bool play)
    {
        var scene = SelectedScene ?? Project.ActiveScene;
        if (clear) scene.Drawables.Clear();

        var drawable = new Drawable
        {
            Name = geo.Name,
            Contours = geo.Contours.Select(c => c.ToList()).ToList()
        };
        ApplyDock(drawable, scene.Target, DockMode);
        scene.Drawables.Add(drawable);
        if (clear)
            scene.Mask.Bounds = new Rect2(0, 0, scene.Target.WidthMm, scene.Target.HeightMm);
        RefreshObjectNames();
        SelectedObjectIndex = scene.Drawables.Count - 1;
        if (clear) History.Clear();
        MarkDirty();
        BumpCanvas();
        if (clear) ViewResetRequested?.Invoke();
        Log($"Loaded geometry '{geo.Name}' ({geo.Contours.Count} paths) via Points");
        if (play) await PlayAsync();
    }

    [RelayCommand]
    private void ResetMesh()
    {
        if (ActiveMesh is null) return;
        ActiveMesh.ResetIdentity(MeshColumns, MeshRows);
        ActiveMesh.IsEnabled = true;
        MeshEnabled = true;
        SyncMeshPointFromSelection();
        BumpCanvas();
        Log($"Mesh identity on {SelectedProjector?.DisplayName}");
    }

    [RelayCommand]
    private void DistortMeshDemo()
    {
        if (ActiveMesh is null || ActiveMesh.Columns < 2 || ActiveMesh.Rows < 2) return;
        var cx = ActiveMesh.Columns / 2;
        var cy = ActiveMesh.Rows / 2;
        var p = ActiveMesh.GetPoint(cx, cy);
        ActiveMesh.SetPoint(cx, cy, new Point2(p.X + 0.05, p.Y + 0.03));
        ActiveMesh.IsEnabled = true;
        MeshEnabled = true;
        MeshPointCol = cx;
        MeshPointRow = cy;
        SyncMeshPointFromSelection();
        BumpCanvas();
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (SelectedScene is null || Projectors.Count == 0) return;
        try
        {
            PushDeviceFieldsToSelection();
            var sceneDevices = GetSceneProjectors();
            if (sceneDevices.Count == 0)
            {
                Log("No projectors bound to this scene");
                return;
            }

            if (SelectedScene.Target.WidthMm > 0)
            {
                if (sceneDevices.Count == 1)
                    sceneDevices[0].FitFovToScene(SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
                else
                    LayoutFovs();
            }
            RefreshFovOverlays();
            PullDeviceUiFromSelection();
            var bags = _pipeline.BuildPerDevice(SelectedScene, Project, sceneDevices);
            await SendBagsAsync(bags, "Play");
        }
        catch (Exception ex)
        {
            Log($"Play failed: {ex.Message}");
        }
    }

    private async Task SendBagsAsync(Dictionary<string, LinesCollection> bags, string label)
    {
        if (SelectedScene is null) return;
        var parts = new List<string>();
        var sentVirtual = false;

        foreach (var p in Projectors)
        {
            if (!bags.TryGetValue(p.Id, out var bag))
                continue;

            if (UseVlt && _vlts.TryGetValue(p.Id, out var vlt))
            {
                vlt.SendFrame(bag);
                await vlt.PlayAsync();
                parts.Add($"{p.DisplayName} bytes={vlt.LastByteCount} pts={bag.Points.Count}");
            }
            else if (!sentVirtual)
            {
                _virtual.SendFrame(bag);
                await _virtual.PlayAsync();
                parts.Add($"Virtual({p.DisplayName}) pts={bag.Points.Count}");
                sentVirtual = true;
            }
            else
                parts.Add($"{p.DisplayName} segs={bag.SegmentCount} (no link)");
        }

        FrameInfo = parts.Count == 0 ? "No bags" : string.Join(" | ", parts);
        SelectedScene.IsPlaying = true;
        IsPlaying = true;
        Log($"{label} — {FrameInfo}");
        BumpCanvas();
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        foreach (var v in _vlts.Values)
            await v.StopAsync();
        await _virtual.StopAsync();
        if (SelectedScene is not null) SelectedScene.IsPlaying = false;
        IsPlaying = false;
        Log("Stop");
    }

    private async Task ImportPathAsync(string path, bool clear, bool play)
    {
        Log($"Importing {Path.GetFileName(path)}…");
        var result = await _import.ImportAsync(path);
        var scene = SelectedScene ?? Project.ActiveScene;
        if (clear) scene.Drawables.Clear();

        // Keep the plane size; park each imported object at the sticky dock corner/edge/center.
        var startIndex = scene.Drawables.Count;
        foreach (var d in result.Drawables)
            ApplyDock(d, scene.Target, DockMode);

        scene.Drawables.AddRange(result.Drawables);
        if (clear)
            scene.Mask.Bounds = new Rect2(0, 0, scene.Target.WidthMm, scene.Target.HeightMm);
        RefreshObjectNames();
        if (result.Drawables.Count > 0)
            SelectedObjectIndex = startIndex;
        if (clear) History.Clear();
        MarkDirty();
        BumpCanvas();
        if (clear) ViewResetRequested?.Invoke();
        Log($"Imported {result.Drawables.Count} from {result.Format} → dock {DockMode}");
        if (play) await PlayAsync();
    }

    private async Task ApplyLoadedDevicesAsync(ProjectDocument project)
    {
        foreach (var id in _vlts.Keys.ToList())
            await DisconnectProjectorAsync(id);

        Projectors.Clear();
        if (project.Devices.Count > 0)
        {
            foreach (var snap in project.Devices)
            {
                var p = ProjectorProfile.CreateDefault(snap.DisplayName);
                snap.ApplyTo(p);
                Projectors.Add(p);
            }
        }
        else
        {
            var p = ProjectorProfile.CreateDefault("VLT-1");
            if (project.CalibrationMesh is not null
                && ModuleTypes.FindMesh(p.ModuleChain) is { Mesh: { } grid })
                project.CalibrationMesh.ApplyTo(grid);
            Projectors.Add(p);
        }

        _suppressDeviceUi = true;
        SelectedProjector = Projectors[0];
        _suppressDeviceUi = false;
        PullDeviceUiFromSelection();
        PullSceneUiFromSelection();
        OnPropertyChanged(nameof(CanRemoveProjector));
        OnPropertyChanged(nameof(ActiveDeviceName));
    }

    private void LayoutFovs()
    {
        if (SelectedScene is null || Projectors.Count == 0) return;
        var devices = GetSceneProjectors();
        if (devices.Count == 0) devices = Projectors.ToList();
        var w = SelectedScene.Target.WidthMm;
        var h = SelectedScene.Target.HeightMm;
        if (devices.Count == 1)
        {
            devices[0].FitFovToScene(w, h);
        }
        else
        {
            var n = devices.Count;
            var slice = w / n * 1.15;
            for (var i = 0; i < n; i++)
            {
                var p = devices[i];
                p.FovWidthMm = slice;
                p.FovHeightMm = h;
                p.Pose.PositionMm = new Point3((i + 0.5) * (w / n), h * 0.5, 0);
            }
        }
        if (SelectedProjector is not null)
            PullDeviceUiFromSelection();
        RefreshFovOverlays();
        BumpCanvas();
    }

    private void RefreshFovOverlays()
    {
        FovOverlays.Clear();
        if (SelectedScene is null) return;
        var devices = GetSceneProjectors();
        var palette = new uint[] { 0xFF44AADD, 0xFFDD8844, 0xFF66CC88, 0xFFCC66AA, 0xFFDDBB33, 0xFF8899FF };
        for (var i = 0; i < devices.Count; i++)
        {
            var p = devices[i];
            var fov = GeometrySplitter.GetFovNormalized(p, SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
            FovOverlays.Add(new FovOverlay
            {
                Name = p.DisplayName,
                BoundsMm = new Rect2(
                    fov.X * SelectedScene.Target.WidthMm,
                    fov.Y * SelectedScene.Target.HeightMm,
                    fov.Width * SelectedScene.Target.WidthMm,
                    fov.Height * SelectedScene.Target.HeightMm),
                ColorArgb = palette[i % palette.Length]
            });
        }
    }

    private void RefreshModuleItems()
    {
        ModuleItems.Clear();
        if (SelectedProjector is not null)
        {
            foreach (var m in SelectedProjector.ModuleChain)
                ModuleItems.Add(new ModuleItemViewModel(m, OnModuleChanged));
        }
        RefreshModuleOverlays();
    }

    private void OnModuleChanged(DeviceModuleConfig cfg, ModuleState before)
    {
        RecordModuleEdit(cfg, before, $"{Hist("Edit", "Edit")} {cfg.DisplayName}");
        RefreshModuleOverlays();
        BumpCanvas();
    }

    private void RefreshModuleOverlays()
    {
        ModuleOverlays.Clear();
        var device = SelectedProjector ?? Projectors.FirstOrDefault();
        if (device is null || SelectedScene is null) return;

        var w = SelectedScene.Target.WidthMm;
        var h = SelectedScene.Target.HeightMm;
        var fov = GeometrySplitter.GetFovNormalized(device, w, h);
        var bounds = new Rect2(fov.X * w, fov.Y * h, fov.Width * w, fov.Height * h);
        var selectedId = SelectedModuleItem?.Model.Id;

        foreach (var cfg in device.ModuleChain)
        {
            if (!cfg.IsEnabled || !cfg.ShowOnTable)
                continue;
            if (ModuleRegistry.Materialize(cfg) is not IRenderableModule module)
                continue;

            var selected = cfg.Id == selectedId;
            ModuleOverlays.Add(new ModuleOverlay
            {
                ModuleId = cfg.Id,
                Name = cfg.DisplayName,
                BoundsMm = bounds,
                Geometry = module.GetGeometry(),
                Anchors = module.GetAnchors(),
                ColorArgb = cfg.TypeId == ModuleTypes.Mesh ? 0xFFFFC83C : 0xFF66CC88,
                IsSelected = selected,
                SelectedAnchor = selected ? _selectedAnchor : -1
            });
        }
    }

    private void PullDeviceUiFromSelection()
    {
        var p = SelectedProjector ?? Projectors.FirstOrDefault();
        if (p is null) return;
        _suppressDeviceUi = true;
        _suppressMeshUi = true;
        DeviceName = p.DisplayName;
        DeviceHost = p.Host;
        DevicePort = p.Port;
        FovWidthMm = p.FovWidthMm;
        FovHeightMm = p.FovHeightMm;
        PoseX = p.Pose.PositionMm.X;
        PoseY = p.Pose.PositionMm.Y;
        ColorR = p.Red;
        ColorG = p.Green;
        ColorB = p.Blue;
        if (ModuleTypes.FindMesh(p.ModuleChain) is { Mesh: { } grid } meshModule)
        {
            MeshEnabled = meshModule.IsEnabled;
            ShowMeshOverlay = meshModule.ShowOnTable;
            MeshColumns = grid.Columns;
            MeshRows = grid.Rows;
            MeshPointCol = Math.Clamp(MeshPointCol, 0, grid.Columns);
            MeshPointRow = Math.Clamp(MeshPointRow, 0, grid.Rows);
            var pt = grid.GetPoint(MeshPointCol, MeshPointRow);
            MeshPointX = pt.X;
            MeshPointY = pt.Y;
            _selectedAnchor = CalibrationMeshModule.AnchorIndex(MeshPointCol, MeshPointRow, grid.Columns);
        }
        else
        {
            MeshEnabled = false;
        }
        _suppressMeshUi = false;
        _suppressDeviceUi = false;
        RefreshModuleItems();
        OnPropertyChanged(nameof(ActiveMesh));
        OnPropertyChanged(nameof(MeshOwnerLabel));
        OnPropertyChanged(nameof(CanRemoveProjector));
    }

    private void PushDeviceFieldsToSelection()
    {
        if (SelectedProjector is null) return;
        SelectedProjector.DisplayName = DeviceName;
        SelectedProjector.Host = DeviceHost;
        SelectedProjector.Port = DevicePort;
        SelectedProjector.FovWidthMm = FovWidthMm;
        SelectedProjector.FovHeightMm = FovHeightMm;
        SelectedProjector.Pose.PositionMm = new Point3(PoseX, PoseY, SelectedProjector.Pose.PositionMm.Z);
        SelectedProjector.Red = (byte)Math.Clamp(ColorR, 0, 255);
        SelectedProjector.Green = (byte)Math.Clamp(ColorG, 0, 255);
        SelectedProjector.Blue = (byte)Math.Clamp(ColorB, 0, 255);
        if (ModuleTypes.FindMesh(SelectedProjector.ModuleChain) is { } meshModule)
        {
            meshModule.IsEnabled = MeshEnabled;
            meshModule.ShowOnTable = ShowMeshOverlay;
            if (meshModule.Mesh is not null)
                meshModule.Mesh.IsEnabled = MeshEnabled;
        }
    }

    private void PushPoseFovToSelection()
    {
        if (_suppressDeviceUi || SelectedProjector is null) return;
        SelectedProjector.FovWidthMm = FovWidthMm;
        SelectedProjector.FovHeightMm = FovHeightMm;
        SelectedProjector.Pose.PositionMm = new Point3(PoseX, PoseY, SelectedProjector.Pose.PositionMm.Z);
        RefreshFovOverlays();
        BumpCanvas();
    }

    private void PushColorToSelection()
    {
        if (_suppressDeviceUi || SelectedProjector is null) return;
        SelectedProjector.Red = (byte)Math.Clamp(ColorR, 0, 255);
        SelectedProjector.Green = (byte)Math.Clamp(ColorG, 0, 255);
        SelectedProjector.Blue = (byte)Math.Clamp(ColorB, 0, 255);
    }

    private void SyncMeshPointFromSelection()
    {
        if (ActiveMesh is null) return;
        _suppressMeshUi = true;
        var col = Math.Clamp(MeshPointCol, 0, ActiveMesh.Columns);
        var row = Math.Clamp(MeshPointRow, 0, ActiveMesh.Rows);
        MeshPointCol = col;
        MeshPointRow = row;
        ActiveMesh.SelectPoint(col, row);
        var p = ActiveMesh.GetPoint(col, row);
        MeshPointX = p.X;
        MeshPointY = p.Y;
        MeshColumns = ActiveMesh.Columns;
        MeshRows = ActiveMesh.Rows;
        _selectedAnchor = CalibrationMeshModule.AnchorIndex(col, row, ActiveMesh.Columns);
        _suppressMeshUi = false;
        RefreshModuleOverlays();
    }

    private void PushSelectedMeshPoint()
    {
        if (_suppressMeshUi || ActiveMeshModule is not { Mesh: { } grid } meshModule) return;
        var before = meshModule.CaptureState();
        grid.SetPoint(MeshPointCol, MeshPointRow, new Point2(MeshPointX, MeshPointY));
        grid.SelectPoint(MeshPointCol, MeshPointRow);
        grid.IsEnabled = true;
        grid.CalculateMorph();
        MeshEnabled = true;
        RecordModuleEdit(meshModule, before, Hist("MeshPoint", "Mesh point"));
        RefreshModuleOverlays();
        BumpCanvas();
    }

    private async Task DisconnectProjectorAsync(string id)
    {
        if (!_vlts.TryGetValue(id, out var vlt)) return;
        try { await vlt.DisconnectAsync(); } catch { /* ignore */ }
        vlt.Dispose();
        _vlts.Remove(id);
    }

    private void LoadTransformFromSelection()
    {
        var d = GetSelectedDrawable();
        if (d is null) return;
        Tx = d.Translation.X;
        Ty = d.Translation.Y;
        Tz = d.Translation.Z;
        Rot = d.RotationDeg;
        Scale = d.Scale;
    }

    private void ApplyTransformToSelection()
    {
        if (_suppressTransformUi) return;
        var d = GetSelectedDrawable();
        if (d is null) return;
        var before = DrawableTransform.Read(d);
        d.Translation = new Point3(Tx, Ty, Tz);
        d.RotationDeg = Rot;
        d.Scale = Scale <= 0 ? 0.001 : Scale;
        Record(
            new ValueEdit<DrawableTransform>(
                $"{Hist("Transform", "Transform")} {d.Name}",
                v => v.ApplyTo(d),
                before,
                DrawableTransform.Read(d)),
            $"drawable:{d.Id}:transform");
        BumpCanvas();
    }

    private Drawable? GetSelectedDrawable()
    {
        if (SelectedScene is null) return null;
        if (SelectedObjectIndex < 0 || SelectedObjectIndex >= SelectedScene.Drawables.Count)
            return null;
        return SelectedScene.Drawables[SelectedObjectIndex];
    }

    private void BumpCanvas() => CanvasRevision++;

    private void Log(string message)
    {
        StatusText = message;
        LogLines.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
        while (LogLines.Count > 300)
            LogLines.RemoveAt(LogLines.Count - 1);
    }

    private void RefreshObjectNames()
    {
        foreach (var old in ObjectItems)
            old.VisibilityChanged -= OnObjectVisibilityChanged;
        ObjectItems.Clear();
        if (SelectedScene is null) return;
        for (var i = 0; i < SelectedScene.Drawables.Count; i++)
        {
            var item = new ObjectListItem(SelectedScene.Drawables[i], i);
            item.VisibilityChanged += OnObjectVisibilityChanged;
            ObjectItems.Add(item);
        }
    }

    private void OnObjectVisibilityChanged(object? sender, EventArgs e) => BumpCanvas();
}
