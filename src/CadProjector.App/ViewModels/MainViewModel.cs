using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CadProjector.App.Controls;
using CadProjector.App.Services;
using CadProjector.App.Views;
using CadProjector.Automation;
using CadProjector.Core.Devices;
using CadProjector.Core.Editing;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Devices;
using CadProjector.FileFormats;
using CadProjector.FileFormats.Dxf;
using CadProjector.FileFormats.Legacy;
using CadProjector.FileFormats.ProjectJson;
using CadProjector.FileFormats.Stl;
using CadProjector.Geometry.Primitives;
using CadProjector.Ilda;
using CadProjector.Logging;
using CadProjector.Logging.Services;
using CadProjector.Rendering;
using CadProjector.Rendering.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly VirtualProjector _virtual = new();
    private readonly DrawingImportService _import = new();
    private readonly LegacyImportService _legacy;
    private readonly AutomationHub _hub = new();
    private readonly DevicePipeline _pipeline = new();
    private readonly Dictionary<string, VltProjector> _vlts = new();
    private string? _projectPath;

    public string? ProjectPath => _projectPath;
    public bool HasProjectPath => !string.IsNullOrWhiteSpace(_projectPath);

    public string ProjectPathToolTip
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_projectPath))
                return "";
            var hint = UiLanguage.Text("Ui.ProjectPathHint", "Double-click to show in file manager");
            return $"{_projectPath}\n{hint}";
        }
    }

    private void SetProjectPath(string? path)
    {
        _projectPath = path;
        OnPropertyChanged(nameof(ProjectPath));
        OnPropertyChanged(nameof(HasProjectPath));
        OnPropertyChanged(nameof(ProjectPathToolTip));
        RefreshWindowTitle();
    }
    private bool _suppressMeshUi;
    private bool _suppressTransformUi;
    private bool _suppressMeshAlign;
    private int _selectedAnchor = -1;
    private bool _suppressDeviceUi;
    private CancellationTokenSource? _ildExportCts;
    private CancellationTokenSource? _importCts;
    private Guid? _busyProgressId;
    private bool _suppressWorkFolderSync;
    private bool _suppressObjectSelection;
    private bool _suppressObjectUi;
    private bool _suppressPrefs;
    private bool _firstPlayConfirmed;
    private IReadOnlyList<ObjectListItem> _treeSelection = [];
    private DispatcherTimer? _linkWatch;
    private readonly Dictionary<string, ProjectorPreviewWindow> _previewWindows = new();
    private readonly Dictionary<string, LinesCollection> _lastDeviceFrames = new();
    private DispatcherTimer? _previewRefreshTimer;

    /// <summary>Set while undo/redo replays a change, so the replay is not recorded again.</summary>
    private bool _restoring;
    private bool _suppressSceneUi;

    public MainViewModel()
    {
        _legacy = new LegacyImportService(_import);
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
        CadLogging.Instance.LogAdded += (_, msg) => StatusText = msg.Message;
        RefreshEndpointItems();
        NewEndpointType = RemoteEndpointType.UdpBinary;
        RefreshObjectNames();
        History.Changed += OnHistoryChanged;
        PullSceneUiFromSelection();
        UseLayerColor = Project.ColorMode == LaserColorMode.LayerColor;
        ApplyPrefs();
        InitHotkeys();
        StartLinkWatch();
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        Log("Ready — add projectors and modules in Devices");
    }

    public WorkspaceViewModel Workspace { get; } = new();

    private void OnWorkspacePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsTreeOpen));
        OnPropertyChanged(nameof(IsSceneOpen));
        OnPropertyChanged(nameof(IsWorkFolderOpen));
        OnPropertyChanged(nameof(IsDevicesOpen));
        OnPropertyChanged(nameof(IsTransformOpen));
        OnPropertyChanged(nameof(IsStlAlignOpen));
        OnPropertyChanged(nameof(IsCalibrateOpen));
        OnPropertyChanged(nameof(IsLogsOpen));
        OnPropertyChanged(nameof(IsAutomationOpen));
        OnPropertyChanged(nameof(IsLeftDockOpen));
        OnPropertyChanged(nameof(IsRightDockOpen));
        OnPropertyChanged(nameof(HasTransformEmpty));
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
                PullObjectUiFromSelection();
                PullDeviceUiFromSelection();
                RefreshObjectNames();
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

    private void Record(IEditAction action, string? mergeKey = null)
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
    public CadLogging Logs => CadLogging.Instance;
    public CadProgress Progress => CadProgress.Inst;
    public ObservableCollection<ModuleItemViewModel> ModuleItems { get; } = [];

    public CalibrationPatternKind[] CalibPatternOptions { get; } =
        [CalibrationPatternKind.Dot, CalibrationPatternKind.Rect, CalibrationPatternKind.Grid];

    public IReadOnlyList<ModuleDescriptor> AvailableModuleTypes { get; } = ModuleRegistry.All;

    [ObservableProperty] public partial ProjectorProfile? SelectedProjector { get; set; }
    [ObservableProperty] public partial ModuleDescriptor? ModuleTypeToAdd { get; set; } = ModuleRegistry.All[0];
    [ObservableProperty] public partial ModuleItemViewModel? SelectedModuleItem { get; set; }
    [ObservableProperty] public partial ProjectionScene? SelectedScene { get; set; }
    [ObservableProperty] public partial int SelectedObjectIndex { get; set; } = -1;
    [ObservableProperty] public partial ObjectListItem? SelectedObjectItem { get; set; }
    /// <summary>Sticky 3×3 docking used when opening/importing drawings (LT…RB, default center).</summary>
    [ObservableProperty] public partial string DockMode { get; set; } = "CM";
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    /// <summary>Raised when the canvas should re-fit zoom/pan (Clear, etc.).</summary>
    public event Action? ViewResetRequested;
    public bool IsTreeOpen
    {
        get => Workspace.ActiveLeft == PanelId.Objects;
        set
        {
            if (value) Workspace.Reveal(PanelId.Objects);
            else Workspace.Close(PanelId.Objects);
        }
    }

    public bool IsSceneOpen
    {
        get => Workspace.ActiveLeft == PanelId.Scene;
        set
        {
            if (value) Workspace.Reveal(PanelId.Scene);
            else Workspace.Close(PanelId.Scene);
        }
    }

    public bool IsWorkFolderOpen
    {
        get => Workspace.ActiveLeft == PanelId.WorkFolder;
        set
        {
            if (value) Workspace.Reveal(PanelId.WorkFolder);
            else Workspace.Close(PanelId.WorkFolder);
        }
    }

    public bool IsDevicesOpen
    {
        get => Workspace.ActiveRight is PanelId.Device or PanelId.Calibrate;
        set
        {
            if (value) Workspace.Reveal(PanelId.Device);
            else Workspace.Close(PanelId.Device);
        }
    }

    public bool IsTransformOpen
    {
        get => Workspace.ActiveRight == PanelId.Transform;
        set
        {
            if (value) Workspace.Reveal(PanelId.Transform);
            else Workspace.Close(PanelId.Transform);
        }
    }

    public bool IsStlAlignOpen
    {
        get => Workspace.ActiveRight == PanelId.StlAlign;
        set
        {
            if (value) Workspace.Reveal(PanelId.StlAlign);
            else Workspace.Close(PanelId.StlAlign);
        }
    }

    public bool IsCalibrateOpen => false;

    public bool IsLogsOpen
    {
        get => Workspace.IsOpen(PanelId.Logs);
        set
        {
            if (value) Workspace.Reveal(PanelId.Logs);
            else Workspace.Close(PanelId.Logs);
        }
    }

    public bool IsAutomationOpen
    {
        get => Workspace.IsOpen(PanelId.Endpoints) || Workspace.IsOpen(PanelId.Clients) || Workspace.IsOpen(PanelId.Commands);
        set
        {
            if (value) Workspace.Reveal(PanelId.Endpoints);
            else Workspace.CloseSlot(DockSlot.Bottom);
        }
    }

    public bool IsLeftDockOpen => Workspace.ActiveLeft is not null;
    public bool IsRightDockOpen => Workspace.ActiveRight is not null;
    public bool HasTransformEmpty => SelectedObjectIndex < 0 && !IsEditingModuleAnchor;

    [ObservableProperty] public partial string LanguageCode { get; set; } = "EN";
    [ObservableProperty] public partial string ObjectName { get; set; } = "";
    [ObservableProperty] public partial string DxfUnitChoice { get; set; } = "Auto";
    [ObservableProperty] public partial string DeviceLinkText { get; set; } = "";
    [ObservableProperty] public partial bool LaserAlert { get; set; }

    public IReadOnlyList<string> DxfUnitChoices { get; } = ["Auto", "mm", "cm", "m", "in", "ft"];
    [ObservableProperty] public partial CalibrationPatternKind CalibPattern { get; set; } = CalibrationPatternKind.Grid;
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsExportingIld { get; set; }
    [ObservableProperty] public partial bool IsImporting { get; set; }

    public bool IsBusy => IsImporting || IsExportingIld;
    [ObservableProperty] public partial bool IsDirty { get; set; }
    [ObservableProperty] public partial bool UseLayerColor { get; set; }
    [ObservableProperty] public partial int CanvasRevision { get; set; }
    [ObservableProperty] public partial bool MaskEnabled { get; set; }
    [ObservableProperty] public partial bool ShowMaskOverlay { get; set; }
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
    [ObservableProperty] public partial double PoseZ { get; set; }
    [ObservableProperty] public partial double PosePitch { get; set; } = 90;
    [ObservableProperty] public partial double PoseYaw { get; set; }
    [ObservableProperty] public partial double PoseRoll { get; set; }
    [ObservableProperty] public partial double FovHDeg { get; set; } = 40;
    [ObservableProperty] public partial double FovVDeg { get; set; } = 40;

    /// <summary>Pin the 3D view to the selected projector — see exactly what it reaches.</summary>
    [ObservableProperty] public partial bool ViewFromProjector { get; set; }

    /// <summary>Tint the STL by reachability from the selected projector.</summary>
    [ObservableProperty] public partial bool ShowCoverage { get; set; }

    [ObservableProperty] public partial string CoverageInfo { get; set; } = "";

    public ProjectorProfile? ViewProjector => ViewFromProjector ? SelectedProjector : null;

    public ProjectorProfile? CoverageProjector => ShowCoverage ? SelectedProjector : null;
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
    [ObservableProperty] public partial bool HasMeshTarget { get; set; }
    [ObservableProperty] public partial string MeshTargetInfo { get; set; } = "";
    [ObservableProperty] public partial bool IsViewport3D { get; set; }
    [ObservableProperty] public partial double MeshTx { get; set; }
    [ObservableProperty] public partial double MeshTy { get; set; }
    [ObservableProperty] public partial double MeshTz { get; set; }
    [ObservableProperty] public partial double MeshRx { get; set; }
    [ObservableProperty] public partial double MeshRy { get; set; }
    [ObservableProperty] public partial double MeshRz { get; set; }
    [ObservableProperty] public partial double MeshSx { get; set; } = 1;
    [ObservableProperty] public partial double MeshSy { get; set; } = 1;
    [ObservableProperty] public partial double MeshSz { get; set; } = 1;
    [ObservableProperty] public partial MeshAlignPickKind MeshAlignPick { get; set; }
    [ObservableProperty] public partial string MeshAlignPickHint { get; set; } = "";
    [ObservableProperty] public partial bool MeshAlignAllowScale { get; set; } = true;
    [ObservableProperty] public partial IReadOnlyList<MeshAlignMarker> MeshAlignMarkers { get; set; } = [];
    [ObservableProperty] public partial IReadOnlyList<string> MeshAlignPairLines { get; set; } = [];

    private readonly Point3?[] _alignMeshLocal = new Point3?[4];
    private readonly Point3?[] _alignTable = new Point3?[4];

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
        OnPropertyChanged(nameof(ViewProjector));
        OnPropertyChanged(nameof(CoverageProjector));
        Log($"Selected {value.DisplayName}");
        BumpCanvas();
    }

    partial void OnMaskEnabledChanged(bool value)
    {
        if (_suppressSceneUi || SelectedScene is null) return;
        SelectedScene.Mask.IsEnabled = value;
        EnsureMaskBounds();
        if (value)
            ShowMaskOverlay = true;
        MarkDirty();
        BumpCanvas();
    }

    partial void OnShowMaskOverlayChanged(bool value)
    {
        if (value)
            EnsureMaskBounds();
        BumpCanvas();
    }

    private void EnsureMaskBounds()
    {
        if (SelectedScene is null) return;
        if (SelectedScene.Mask.Bounds.Width > 0 && SelectedScene.Mask.Bounds.Height > 0)
            return;
        SelectedScene.Mask.Bounds = new Rect2(0, 0, SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
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
        {
            MaskEnabled = value.Mask.IsEnabled;
            ShowMaskOverlay = value.Mask.IsEnabled;
        }
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
        RefreshMeshTargetUi();
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
    partial void OnSelectedModuleItemChanged(ModuleItemViewModel? value)
    {
        RefreshModuleOverlays();
        if (value is not null
            && ModuleRegistry.Materialize(value.Model) is IRenderableModule renderable
            && renderable.GetAnchors().Count > 0)
            FocusKeyboardOnModule();
    }
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
    partial void OnPoseZChanged(double value) => PushPoseFovToSelection();
    partial void OnPosePitchChanged(double value) => PushPoseFovToSelection();
    partial void OnPoseYawChanged(double value) => PushPoseFovToSelection();
    partial void OnPoseRollChanged(double value) => PushPoseFovToSelection();
    partial void OnFovHDegChanged(double value) => PushPoseFovToSelection();
    partial void OnFovVDegChanged(double value) => PushPoseFovToSelection();

    partial void OnViewFromProjectorChanged(bool value)
    {
        if (value)
        {
            IsViewport3D = true;
            EnsureProjectorHasAView();
        }

        OnPropertyChanged(nameof(ViewProjector));
        OnPropertyChanged(nameof(CoverageProjector));
        if (!value && !ShowCoverage)
            CoverageInfo = "";
        BumpCanvas();
    }

    partial void OnShowCoverageChanged(bool value)
    {
        OnPropertyChanged(nameof(CoverageProjector));
        if (!value && !ViewFromProjector)
            CoverageInfo = "";
        BumpCanvas();
    }

    /// <summary>Called back by the viewport once it has classified the mesh facets.</summary>
    public void ReportCoverage(MeshCoverageStats stats, bool shadowsTested)
    {
        if (stats.Total == 0)
        {
            CoverageInfo = "";
            return;
        }

        CoverageInfo = string.Format(
            UiLanguage.Text("Ui.CoverageInfo", "Reached {0:0}% · grazing {1:0}% · shadowed {2:0}% · out of field {3:0}%"),
            stats.GoodFraction * 100,
            stats.Grazing * 100.0 / stats.Total,
            stats.Shadowed * 100.0 / stats.Total,
            (stats.OutOfView + stats.BackFacing) * 100.0 / stats.Total)
            + (shadowsTested ? "" : " " + UiLanguage.Text("Ui.CoverageNoShadows", "(shadows skipped — mesh too heavy)"));
    }

    [RelayCommand]
    private void AimProjectorAtTarget()
    {
        if (SelectedProjector is not { } p || SelectedScene is null)
            return;

        GetProjectorAim(out var target, out var span, out var roof);
        if (p.Pose.PositionMm.Z <= roof + 1)
            p.Pose.ParkAbove(target, span, roof);
        else
            p.Pose.AimAt(target);

        PullDeviceUiFromSelection();
        BumpCanvas();
        Log(string.Format(
            UiLanguage.Text("Ui.PoseAimed", "{0} aimed at the target"),
            p.DisplayName));
    }

    /// <summary>
    /// A projector parked on the table (Z≈0) is a pinhole in the drawing plane:
    /// some STL vertices land at ±1e20 px and used to overflow the rasterizer.
    /// Lift it above the part before looking through it.
    /// </summary>
    private void EnsureProjectorHasAView()
    {
        if (SelectedProjector is not { } p || SelectedScene is null)
            return;

        GetProjectorAim(out var target, out var span, out var roof);
        if (p.Pose.PositionMm.Z > roof + 1)
            return;

        p.Pose.ParkAbove(target, span, roof);
        PullDeviceUiFromSelection();
        Log(string.Format(
            UiLanguage.Text("Ui.PoseAimed", "{0} aimed at the target"),
            p.DisplayName));
    }

    private void GetProjectorAim(out Point3 target, out double span, out double roofZ)
    {
        var scene = SelectedScene!;
        span = Math.Max(scene.Target.WidthMm, scene.Target.HeightMm);
        if (scene.MeshTarget is { } mesh && double.IsFinite(mesh.WorldBounds.Diagonal))
        {
            var b = mesh.WorldBounds;
            target = b.Center;
            roofZ = b.Max.Z;
            span = Math.Max(span, b.Diagonal);
            return;
        }

        target = new Point3(scene.Target.WidthMm * 0.5, scene.Target.HeightMm * 0.5, 0);
        roofZ = 0;
    }
    partial void OnColorRChanged(int value) => PushColorToSelection();
    partial void OnColorGChanged(int value) => PushColorToSelection();
    partial void OnColorBChanged(int value) => PushColorToSelection();

    partial void OnSelectedObjectIndexChanged(int value)
    {
        if (!_suppressObjectSelection)
            SyncSelectedItemFromIndex(value);
        LoadTransformFromSelection();
        PullObjectUiFromSelection();
    }

    partial void OnSelectedObjectItemChanged(ObjectListItem? value)
    {
        if (!_suppressObjectSelection && value is not null)
        {
            _suppressObjectSelection = true;
            SelectedObjectIndex = value.RootIndex;
            _suppressObjectSelection = false;
        }
        LoadTransformFromSelection();
        PullObjectUiFromSelection();
        OnPropertyChanged(nameof(HasTransformEmpty));
        if (value is not null)
            Workspace.Reveal(PanelId.Transform);
    }

    private void SyncSelectedItemFromIndex(int index)
    {
        _suppressObjectSelection = true;
        SelectedObjectItem = index >= 0 && index < ObjectItems.Count ? ObjectItems[index] : null;
        _suppressObjectSelection = false;
    }

    partial void OnTxChanged(double value) => ApplyTransformToSelection();
    partial void OnTyChanged(double value) => ApplyTransformToSelection();
    partial void OnTzChanged(double value) => ApplyTransformToSelection();
    partial void OnRotChanged(double value) => ApplyTransformToSelection();
    partial void OnScaleChanged(double value) => ApplyTransformToSelection();

    [RelayCommand] private void OpenTree() => Workspace.Toggle(PanelId.Objects);
    [RelayCommand] private void OpenScenePanel() => Workspace.Toggle(PanelId.Scene);
    [RelayCommand] private void OpenDevices() => Workspace.Toggle(PanelId.Device);
    [RelayCommand] private void OpenAutomation() => Workspace.Toggle(PanelId.Endpoints);
    [RelayCommand] private void OpenWorkFolder() => Workspace.Toggle(PanelId.WorkFolder);
    [RelayCommand] private void OpenLogs() => Workspace.Toggle(PanelId.Logs);

    /// <summary>Select a projector and open the Devices inspector tab.</summary>
    [RelayCommand]
    private void OpenDeviceSettings(string? projectorId)
    {
        var device = string.IsNullOrWhiteSpace(projectorId)
            ? SelectedProjector ?? Projectors.FirstOrDefault()
            : Projectors.FirstOrDefault(p => p.Id == projectorId);
        if (device is null) return;

        SelectedProjector = device;
        Workspace.Reveal(PanelId.Device);
        Workspace.DeviceTabIndex = 0;
    }

    [RelayCommand]
    private void CloseOverlay(string? name)
    {
        switch (name)
        {
            case "Tree": Workspace.Close(PanelId.Objects); break;
            case "Scene": Workspace.Close(PanelId.Scene); break;
            case "StlAlign":
                Workspace.Close(PanelId.StlAlign);
                MeshAlignPick = MeshAlignPickKind.Off;
                MeshAlignPickHint = "";
                break;
            case "Devices": Workspace.Close(PanelId.Device); break;
            case "Transform": Workspace.Close(PanelId.Transform); break;
            case "Calibrate":
                break;
            case "Logs": Workspace.Close(PanelId.Logs); break;
            case "Automation": Workspace.CloseSlot(DockSlot.Bottom); break;
            case "WorkFolder": Workspace.Close(PanelId.WorkFolder); break;
            case "Hotkeys":
                CancelHotkeyCapture();
                break;
            case "Left": Workspace.CloseSlot(DockSlot.Left); break;
            case "Right": Workspace.CloseSlot(DockSlot.Right); break;
            case "Bottom": Workspace.CloseSlot(DockSlot.Bottom); break;
        }
    }

    [RelayCommand]
    private void OpenProjectorPreview(ProjectorProfile? device)
    {
        device ??= SelectedProjector;
        if (device is null || HostWindow is null) return;
        ShowProjectorPreview(device);
    }
    [RelayCommand] private void ClearLogs() => CadLogging.Instance.ClearAll();

    [RelayCommand]
    private void ToggleLanguage()
    {
        var code = UiLanguage.Toggle();
        LanguageCode = code.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? "RU" : "EN";
        OnHistoryChanged();
        RefreshHotkeyRows();
        PersistPrefs();
        Log($"Language: {LanguageCode}");
    }

    [RelayCommand]
    private void OpenStlAlign()
    {
        if (!HasMeshTarget) return;
        Workspace.Toggle(PanelId.StlAlign);
        if (IsStlAlignOpen)
            PullMeshAlignUi();
    }

    [RelayCommand]
    private void OpenCalibrate()
    {
        ShowMeshOverlay = true;
        MeshEnabled = true;
        if (ModuleItems.FirstOrDefault(i => i.TypeId == ModuleTypes.Mesh) is { } meshItem)
            SelectedModuleItem = meshItem;
        FocusKeyboardOnModule();
        Workspace.RevealDeviceModules();
        Log($"Calibrate mesh of {SelectedProjector?.DisplayName ?? "?"}");
    }

    [RelayCommand]
    private void OpenTransform()
    {
        Workspace.Toggle(PanelId.Transform);
        if (IsTransformOpen) LoadTransformFromSelection();
    }

    [RelayCommand]
    private void SetViewport2D() => IsViewport3D = false;

    [RelayCommand]
    private void SetViewport3D() => IsViewport3D = true;

    [RelayCommand]
    private void ResetView() => ViewResetRequested?.Invoke();

    [RelayCommand]
    private void ToggleBottomConsole()
    {
        if (Workspace.IsBottomOpen)
            Workspace.CloseSlot(DockSlot.Bottom);
        else
            Workspace.Reveal(PanelId.Logs);
    }

    [RelayCommand]
    private void ResetWorkspaceLayout()
    {
        Workspace.LeftWidth = 300;
        Workspace.RightWidth = 340;
        Workspace.BottomHeight = 200;
        Workspace.ActiveLeft = null;
        Workspace.ActiveRight = null;
        Workspace.IsBottomOpen = false;
        // Notify so MainWindow re-applies column sizes even if docks stay closed.
        OnPropertyChanged(nameof(IsLeftDockOpen));
        OnPropertyChanged(nameof(IsRightDockOpen));
    }

    [RelayCommand]
    private void SelectMeshModule()
    {
        var item = ModuleItems.FirstOrDefault(i => i.TypeId == ModuleTypes.Mesh);
        if (item is null)
        {
            Log("No Mesh module in this projector's chain — Add → Mesh");
            return;
        }

        SelectedModuleItem = item;
        item.IsExpanded = true;
        ShowMeshOverlay = true;
        MeshEnabled = true;
        if (ActiveMeshModule is { } meshModule)
        {
            meshModule.ShowOnTable = true;
            meshModule.IsEnabled = true;
        }
        RefreshModuleOverlays();
        BumpCanvas();
        FocusKeyboardOnModule();
        Workspace.RevealDeviceModules();
        Log($"Selected Mesh module on {SelectedProjector?.DisplayName}");
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

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenFileAsync()
    {
        if (HostWindow is null) return;
        var files = await HostWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open drawing",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Drawings / 2CUT") { Patterns = ["*.dxf", "*.svg", "*.2scn"] },
                new FilePickerFileType("DXF") { Patterns = ["*.dxf"] },
                new FilePickerFileType("SVG") { Patterns = ["*.svg"] },
                new FilePickerFileType(UiLanguage.Text("Ui.LegacyScene", "2CUT scene (.2scn)")) { Patterns = ["*.2scn"] }
            ]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path is null) return;
        await ImportPathAsync(path, clear: true, play: false);
    }

    [RelayCommand]
    private async Task LoadStlTargetAsync()
    {
        if (HostWindow is null) return;
        var files = await HostWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = UiLanguage.Text("Ui.StlTarget", "STL target"),
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("STL") { Patterns = ["*.stl"] }]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path is null) return;
        await ImportPathAsync(path, clear: false, play: false);
    }

    [RelayCommand]
    private void FitStlTarget()
    {
        if (SelectedScene?.MeshTarget is not { } mesh) return;
        var before = MeshAlignState.Read(mesh);
        mesh.FitToPlane(SelectedScene.Target.WidthMm, SelectedScene.Target.HeightMm);
        var after = MeshAlignState.Read(mesh);
        Record(
            new ValueEdit<MeshAlignState>(
                Hist("StlFit", "Fit STL"),
                ApplyMeshAlignState,
                before,
                after),
            "mesh-align");
        RefreshMeshTargetUi();
        MarkDirty();
        BumpCanvas();
        Log(UiLanguage.Text("Ui.StlFitted", "STL fitted to the scene plane"));
    }

    [RelayCommand]
    private void ClearStlTarget()
    {
        if (SelectedScene is null || SelectedScene.MeshTarget is null) return;
        SelectedScene.MeshTarget = null;
        RefreshMeshTargetUi();
        MarkDirty();
        BumpCanvas();
        Log(UiLanguage.Text("Ui.StlCleared", "STL target cleared — plane only"));
    }

    private void RefreshMeshTargetUi()
    {
        var mesh = SelectedScene?.MeshTarget;
        HasMeshTarget = mesh is not null;
        if (!HasMeshTarget)
            IsStlAlignOpen = false;
        if (!HasMeshTarget)
            ResetMeshPointAlign();
        MeshTargetInfo = mesh is null
            ? UiLanguage.Text("Ui.StlNone", "Plane (no STL)")
            : string.Format(
                UiLanguage.Text("Ui.StlInfo", "{0} — {1} triangles"),
                Path.GetFileName(mesh.SourcePath),
                mesh.TriangleCount);
        PullMeshAlignUi();
        RebuildMeshAlignMarkers();
    }

    private void PullMeshAlignUi()
    {
        _suppressMeshAlign = true;
        var mesh = SelectedScene?.MeshTarget;
        if (mesh is null)
        {
            MeshTx = MeshTy = MeshTz = 0;
            MeshRx = MeshRy = MeshRz = 0;
            MeshSx = MeshSy = MeshSz = 1;
        }
        else
        {
            MeshTx = mesh.Translation.X;
            MeshTy = mesh.Translation.Y;
            MeshTz = mesh.Translation.Z;
            MeshRx = mesh.RotationDeg.X;
            MeshRy = mesh.RotationDeg.Y;
            MeshRz = mesh.RotationDeg.Z;
            MeshSx = mesh.Scale.X;
            MeshSy = mesh.Scale.Y;
            MeshSz = mesh.Scale.Z;
        }
        _suppressMeshAlign = false;
    }

    private void ApplyMeshAlignState(MeshAlignState state)
    {
        if (SelectedScene?.MeshTarget is not { } mesh) return;
        state.ApplyTo(mesh);
        PullMeshAlignUi();
        RebuildMeshAlignMarkers();
        BumpCanvas();
    }

    private void ApplyMeshAlignFromUi()
    {
        if (_suppressMeshAlign) return;
        if (SelectedScene?.MeshTarget is not { } mesh) return;
        var before = MeshAlignState.Read(mesh);
        var sx = MeshSx == 0 ? 1e-6 : MeshSx;
        var sy = MeshSy == 0 ? 1e-6 : MeshSy;
        var sz = MeshSz == 0 ? 1e-6 : MeshSz;
        var after = new MeshAlignState(
            new Point3(MeshTx, MeshTy, MeshTz),
            new Point3(MeshRx, MeshRy, MeshRz),
            new Point3(sx, sy, sz));
        after.ApplyTo(mesh);
        Record(
            new ValueEdit<MeshAlignState>(
                Hist("StlAlign", "STL align"),
                ApplyMeshAlignState,
                before,
                after),
            "mesh-align");
        MarkDirty();
        RebuildMeshAlignMarkers();
        BumpCanvas();
    }

    partial void OnMeshTxChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshTyChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshTzChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshRxChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshRyChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshRzChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshSxChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshSyChanged(double value) => ApplyMeshAlignFromUi();
    partial void OnMeshSzChanged(double value) => ApplyMeshAlignFromUi();

    [RelayCommand]
    private void ArmMeshAlignPick()
    {
        if (!HasMeshTarget) return;
        MeshAlignPick = MeshAlignPickKind.OnMesh;
        MeshAlignPickHint = UiLanguage.Text("Ui.StlPickMeshHint", "LMB on the STL surface");
        Log(MeshAlignPickHint);
    }

    [RelayCommand]
    private void ArmTableAlignPick()
    {
        if (!HasMeshTarget) return;
        MeshAlignPick = MeshAlignPickKind.OnTable;
        MeshAlignPickHint = UiLanguage.Text("Ui.StlPickTableHint", "LMB on the table — where that point should sit");
        Log(MeshAlignPickHint);
    }

    [RelayCommand]
    private void ClearMeshPointAlign() => ResetMeshPointAlign();

    [RelayCommand(CanExecute = nameof(CanApplyMeshPointAlign))]
    private void ApplyMeshPointAlign()
    {
        if (SelectedScene?.MeshTarget is not { } mesh) return;
        var local = new List<Point3>();
        var scene = new List<Point3>();
        for (var i = 0; i < 4; i++)
        {
            if (_alignMeshLocal[i] is not { } a || _alignTable[i] is not { } b)
                continue;
            local.Add(a);
            scene.Add(b);
        }

        if (local.Count < 3)
            return;

        var before = MeshAlignState.Read(mesh);
        if (!mesh.TryAlignFromPoints(local, scene, MeshAlignAllowScale, out var rms))
        {
            Log(UiLanguage.Text("Ui.StlAlignFail", "Could not align: points are nearly collinear"));
            return;
        }

        var after = MeshAlignState.Read(mesh);
        Record(
            new ValueEdit<MeshAlignState>(
                Hist("StlAlign", "STL align"),
                ApplyMeshAlignState,
                before,
                after),
            "mesh-align");
        PullMeshAlignUi();
        RebuildMeshAlignMarkers();
        MarkDirty();
        BumpCanvas();
        MeshAlignPick = MeshAlignPickKind.Off;
        MeshAlignPickHint = "";
        Log(string.Format(
            UiLanguage.Text("Ui.StlAlignedPoints", "STL aligned by {0} points (RMS {1:0.##} mm)"),
            local.Count, rms));
    }

    public void OnMeshAlignPicked(Point3 world, bool meshHit)
    {
        if (SelectedScene?.MeshTarget is not { } mesh || MeshAlignPick == MeshAlignPickKind.Off)
            return;

        if (MeshAlignPick == MeshAlignPickKind.OnMesh)
        {
            if (!meshHit)
            {
                Log(UiLanguage.Text("Ui.StlNeedMeshHit", "Click the model, not empty space"));
                return;
            }

            var slot = FirstAlignSlot(needMesh: true);
            if (slot < 0)
            {
                Log(UiLanguage.Text("Ui.StlPointsFull", "Already have 4 pairs — Apply or reset"));
                MeshAlignPick = MeshAlignPickKind.Off;
                return;
            }

            _alignMeshLocal[slot] = mesh.InverseTransformPoint(world);
            MeshAlignPick = MeshAlignPickKind.OnTable;
            MeshAlignPickHint = UiLanguage.Text("Ui.StlPickTableHint", "LMB on the table — where that point should sit");
        }
        else
        {
            var slot = FirstAlignSlot(needMesh: false);
            if (slot < 0)
            {
                MeshAlignPick = MeshAlignPickKind.Off;
                return;
            }

            _alignTable[slot] = new Point3(world.X, world.Y, 0);
            var next = FirstAlignSlot(needMesh: true);
            if (next >= 0)
            {
                MeshAlignPick = MeshAlignPickKind.OnMesh;
                MeshAlignPickHint = UiLanguage.Text("Ui.StlPickMeshHint", "LMB on the STL surface");
            }
            else
            {
                MeshAlignPick = MeshAlignPickKind.Off;
                MeshAlignPickHint = "";
            }
        }

        RebuildMeshAlignMarkers();
        ApplyMeshPointAlignCommand.NotifyCanExecuteChanged();
        BumpCanvas();
    }

    private bool CanApplyMeshPointAlign()
    {
        var n = 0;
        for (var i = 0; i < 4; i++)
            if (_alignMeshLocal[i] is not null && _alignTable[i] is not null)
                n++;
        return n >= 3;
    }

    private int FirstAlignSlot(bool needMesh)
    {
        if (needMesh)
        {
            for (var i = 0; i < 4; i++)
                if (_alignMeshLocal[i] is null)
                    return i;
            return -1;
        }

        for (var i = 0; i < 4; i++)
            if (_alignMeshLocal[i] is not null && _alignTable[i] is null)
                return i;
        return -1;
    }

    private void ResetMeshPointAlign()
    {
        Array.Clear(_alignMeshLocal);
        Array.Clear(_alignTable);
        MeshAlignPick = MeshAlignPickKind.Off;
        MeshAlignPickHint = "";
        RebuildMeshAlignMarkers();
        ApplyMeshPointAlignCommand.NotifyCanExecuteChanged();
        BumpCanvas();
    }

    private void RebuildMeshAlignMarkers()
    {
        var mesh = SelectedScene?.MeshTarget;
        var marks = new List<MeshAlignMarker>();
        var lines = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var a = _alignMeshLocal[i];
            var b = _alignTable[i];
            if (a is null && b is null)
                continue;
            if (a is { } local && mesh is not null)
                marks.Add(new MeshAlignMarker(mesh.TransformPoint(local), OnMesh: true, i + 1));
            if (b is { } table)
                marks.Add(new MeshAlignMarker(table, OnMesh: false, i + 1));
            lines.Add(FormatAlignPair(i + 1, a, b));
        }

        MeshAlignMarkers = marks;
        MeshAlignPairLines = lines;
    }

    private static string FormatAlignPair(int n, Point3? mesh, Point3? table)
    {
        static string Fmt(Point3 p) => $"{p.X:0.#}, {p.Y:0.#}, {p.Z:0.#}";
        var left = mesh is { } m ? Fmt(m) : "—";
        var right = table is { } t ? Fmt(t) : "—";
        return $"{n}:  {left}  →  {right}";
    }

    private bool CanOpenFile() => !IsBusy;

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
            var chosen = file.TryGetLocalPath();
            if (chosen is null) return;
            SetProjectPath(chosen);
        }

        try
        {
            PushDeviceFieldsToSelection();
            Project.Devices = Projectors.Select(DeviceSnapshot.From).ToList();
            // Mesh lives only inside each device's module chain now.
            Project.CalibrationMesh = null;
            await ProjectJsonStore.SaveAsync(Project, _projectPath);
            ClearDirty();
            RememberLastProject(_projectPath);
            Log($"Saved {Path.GetFileName(_projectPath)}");
        }
        catch (Exception ex)
        {
            Log($"Save failed: {ex.Message}", LogMessageStatus.Error);
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
        BeginBusy(
            UiLanguage.Text("Ui.ExportIldBusy", "Exporting .ild…"),
            CancelBusy);
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

            UpdateBusy(UiLanguage.Text("Ui.ExportIldWriting", "Writing file…"));
            await IldaFileWriter.WriteAsync(path, ilda, ct);
            Log($"Exported ILDA via {deviceName} ({ilda.Points.Count} pts)", LogMessageStatus.Good);
        }
        catch (OperationCanceledException)
        {
            TryDeletePartialExport(path);
            Log("ILDA export cancelled", LogMessageStatus.Warning);
        }
        catch (Exception ex)
        {
            TryDeletePartialExport(path);
            Log($"ILDA export failed: {ex.Message}", LogMessageStatus.Error);
        }
        finally
        {
            IsExportingIld = false;
            _ildExportCts?.Dispose();
            _ildExportCts = null;
            ClearBusyUi();
        }
    }

    private bool CanExportIld() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanCancelBusy))]
    private void CancelBusy()
    {
        if (IsImporting)
        {
            _importCts?.Cancel();
            Log("Import cancel requested…");
        }
        if (IsExportingIld)
        {
            _ildExportCts?.Cancel();
            Log("ILDA export cancel requested…");
        }
    }

    private bool CanCancelBusy() => IsBusy;

    // Keep old command name for toolbar button that still binds CancelExportIld
    [RelayCommand(CanExecute = nameof(CanCancelBusy))]
    private void CancelExportIld() => CancelBusy();

    private bool CanCancelExportIld() => IsBusy;

    partial void OnIsExportingIldChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        ExportIldCommand.NotifyCanExecuteChanged();
        CancelExportIldCommand.NotifyCanExecuteChanged();
        CancelBusyCommand.NotifyCanExecuteChanged();
        OpenFileCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsImportingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        ExportIldCommand.NotifyCanExecuteChanged();
        CancelExportIldCommand.NotifyCanExecuteChanged();
        CancelBusyCommand.NotifyCanExecuteChanged();
        OpenFileCommand.NotifyCanExecuteChanged();
    }

    private void ClearBusyUi()
    {
        if (_busyProgressId is { } id)
        {
            CadProgress.End(id);
            _busyProgressId = null;
        }

        if (IsBusy) return;
        StatusText = UiLanguage.Text("Ui.Ready", "Ready");
    }

    private void BeginBusy(string name, Action? cancel = null, bool determinate = false)
    {
        if (_busyProgressId is { } old)
            CadProgress.End(old);

        _busyProgressId = determinate
            ? CadProgress.Start(name, 100, cancel)
            : CadProgress.Waiter(name, LogMessageStatus.Info, 0, cancel);
        StatusText = name;
    }

    private void UpdateBusy(string message, double? fraction = null)
    {
        if (_busyProgressId is not { } id) return;
        if (fraction is { } f)
            CadProgress.Set(id, (int)Math.Clamp(f * 100, 0, 100), message);
        else if (CadProgress.Inst.LastProgress is { } task && task.Uid == id)
            CadProgress.Set(id, task.Value, message);
        StatusText = message;
    }

    private void ReportBusy(ImportProgress p)
    {
        Dispatcher.UIThread.Post(() => UpdateBusy(p.Message, p.Fraction));
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
            FileTypeFilter =
            [
                new FilePickerFileType("2Cut project") { Patterns = ["*.cproj"] },
                new FilePickerFileType(UiLanguage.Text("Ui.LegacyHub", "2CUT hub (.2cfg/.mws)")) { Patterns = ["*.2cfg", "*.mws"] },
                new FilePickerFileType(UiLanguage.Text("Ui.LegacyScene", "2CUT scene (.2scn)")) { Patterns = ["*.2scn"] }
            ]
        });
        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (path is null) return;

        try
        {
            if (LegacyImportService.CanImport(path))
            {
                await ImportLegacyAsProjectAsync(path);
                return;
            }

            Project = await ProjectJsonStore.LoadAsync(path);
            SetProjectPath(path);
            await ApplyProjectDocumentAsync(clearDirty: true);
            RememberLastProject(path);
        }
        catch (Exception ex)
        {
            Log($"Load failed: {ex.Message}", LogMessageStatus.Error);
        }
    }

    /// <summary>Opens the last saved/loaded .cproj if the file still exists.</summary>
    public async Task TryOpenLastProjectAsync()
    {
        var path = AppPrefs.LoadLastProject();
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (!File.Exists(path))
        {
            AppPrefs.SaveLastProject(null);
            Log(string.Format(
                UiLanguage.Text("Ui.LastProjectMissing", "Last project not found: {0}"),
                Path.GetFileName(path)));
            return;
        }

        try
        {
            Project = await ProjectJsonStore.LoadAsync(path);
            SetProjectPath(path);
            await ApplyProjectDocumentAsync(clearDirty: true);
            RememberLastProject(path);
            Log(string.Format(
                UiLanguage.Text("Ui.LastProjectOpened", "Opened last project: {0}"),
                Path.GetFileName(path)));
        }
        catch (Exception ex)
        {
            Log($"Last project load failed: {ex.Message}", LogMessageStatus.Error);
        }
    }

    private static void RememberLastProject(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (!path.EndsWith(".cproj", StringComparison.OrdinalIgnoreCase))
            return;
        AppPrefs.SaveLastProject(Path.GetFullPath(path));
    }

    [RelayCommand]
    private void RevealProjectFolder()
    {
        if (string.IsNullOrWhiteSpace(_projectPath))
            return;
        ShellReveal.RevealInFileManager(_projectPath);
    }

    [RelayCommand]
    private void AlignObject(string? mode)
    {
        // Sticky dock setting from the Transform panel — used on the next import too.
        if (!string.IsNullOrEmpty(mode))
            DockMode = mode;

        var d = GetFocusedDrawable();
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
    private async Task NewProjectAsync()
    {
        if (!await ConfirmDiscardOrSaveAsync())
            return;

        if (IsPlaying)
            await StopAsync();

        Project = new ProjectDocument { Name = "2Cut" };
        SetProjectPath(null);
        IsViewport3D = false;
        await ApplyProjectDocumentAsync(clearDirty: true);
        Log(UiLanguage.Text("Ui.NewProjectReady", "New configuration created"));
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
        var selected = GetSelectedDrawables();
        if (selected.Count > 0)
            return selected.Where(d => d.IsVisible).ToList();
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
        var switched = _keyboardFocus != KeyboardFocusKind.Module;
        _keyboardFocus = KeyboardFocusKind.Module;
        if (switched)
            IsTransformOpen = true;
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
        NotifyTransformTarget();
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
        FocusKeyboardOnDrawables();
        if (index >= 0 && index < ObjectItems.Count)
            _treeSelection = [ObjectItems[index]];
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
        Record(
            new ValueEdit<Rect2>(Hist("Mask", "Mask bounds"), v => mask.Bounds = v, from, to),
            "mask:bounds");
        BumpCanvas();
    }

    [RelayCommand]
    private async Task PlayCalibrationPatternAsync()
    {
        if (SelectedScene is null) return;
        if (!await ConfirmFirstPlayIfNeededAsync()) return;
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
        }
        catch (Exception ex)
        {
            Log($"Profile save failed: {ex.Message}", LogMessageStatus.Error);
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
            OnPropertyChanged(nameof(ActiveDeviceName));
        }
        catch (Exception ex)
        {
            Log($"Profile load failed: {ex.Message}", LogMessageStatus.Error);
        }
    }

    [RelayCommand]
    private void ToggleSelectedLayerVisibility()
    {
        var focus = SelectedObjectItem;
        if (focus is null) return;

        var layer = focus.LayerName;
        var pool = ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants());
        List<ObjectListItem> targets = string.IsNullOrEmpty(layer)
            ? [focus]
            : pool.Where(x => string.Equals(x.LayerName, layer, StringComparison.OrdinalIgnoreCase)).ToList();
        if (targets.Count == 0) return;

        var turnOn = targets.Any(x => !x.IsVisible);
        foreach (var t in targets)
            t.IsVisible = turnOn;
        Log($"Layer visibility → {(turnOn ? "on" : "off")}");
        BumpCanvas();
    }

    public void SetTreeSelection(IReadOnlyList<ObjectListItem> items)
    {
        _treeSelection = items;
        FocusKeyboardOnDrawables();
        if (items.Count == 0) return;
        SelectedObjectItem = items[^1];
    }

    [RelayCommand]
    private void GroupObjects()
    {
        if (SelectedScene is null) return;
        var items = _treeSelection.Count >= 2
            ? _treeSelection.ToList()
            : [];
        if (items.Count < 2)
        {
            Log(UiLanguage.Text("Ui.GroupNeedSiblings", "Select two or more sibling objects (Ctrl+click) to group"),
                LogMessageStatus.Warning);
            return;
        }

        var parent = items[0].Parent;
        if (items.Any(i => i.Parent != parent))
        {
            Log(UiLanguage.Text("Ui.GroupSiblingsOnly", "Group only works on siblings"),
                LogMessageStatus.Warning);
            return;
        }

        var before = SnapshotTree();
        var host = parent is null ? SelectedScene.Drawables : parent.Drawable.Children;
        var members = items.Select(i => i.Drawable).Where(host.Contains).ToList();
        if (members.Count < 2) return;

        var insertAt = members.Min(m => host.IndexOf(m));
        foreach (var m in members)
            host.Remove(m);
        var group = Drawable.CreateGroup(members, UiLanguage.Text("Ui.GroupDefaultName", "Group"));
        host.Insert(insertAt, group);
        RecordTreeChange($"{Hist("Group", "Group")} {members.Count}", before);
        RefreshObjectNames();
        SelectedObjectItem = ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants())
            .FirstOrDefault(x => x.Drawable.Id == group.Id);
        MarkDirty();
        BumpCanvas();
        Log($"Grouped {members.Count} objects");
    }

    [RelayCommand]
    private void UngroupObject()
    {
        var focus = SelectedObjectItem;
        if (focus is null || SelectedScene is null) return;
        var d = focus.Drawable;
        if (!d.IsGroup) return;

        var before = SnapshotTree();
        var parent = focus.Parent;
        var host = parent is null ? SelectedScene.Drawables : parent.Drawable.Children;
        var index = host.IndexOf(d);
        if (index < 0) return;

        var name = d.Name;
        var promoted = d.Ungroup();
        if (promoted.Count == 0) return;

        host.RemoveAt(index);
        host.InsertRange(index, promoted);
        RecordTreeChange($"{Hist("Ungroup", "Ungroup")} {name}", before);
        RefreshObjectNames();
        var first = promoted[0];
        SelectedObjectItem = ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants())
            .FirstOrDefault(x => x.Drawable.Id == first.Id);
        MarkDirty();
        BumpCanvas();
        Log($"Ungrouped {name} → {promoted.Count} objects");
    }

    [RelayCommand]
    private void DeleteSelectedObjects()
    {
        if (SelectedScene is null) return;

        var items = _treeSelection.Count > 0
            ? _treeSelection.ToList()
            : SelectedObjectItem is { } one ? [one] : [];
        if (items.Count == 0) return;

        var selected = items.ToHashSet();
        var doomed = items
            .Where(i =>
            {
                for (var p = i.Parent; p is not null; p = p.Parent)
                    if (selected.Contains(p)) return false;
                return true;
            })
            .ToList();
        if (doomed.Count == 0) return;

        var before = SnapshotTree();
        foreach (var item in doomed)
        {
            var host = item.Parent is null ? SelectedScene.Drawables : item.Parent.Drawable.Children;
            host.Remove(item.Drawable);
        }

        RecordTreeChange($"{Hist("Delete", "Delete")} {doomed.Count}", before);
        _treeSelection = [];
        SelectedObjectItem = null;
        SelectedObjectIndex = -1;
        RefreshObjectNames();
        MarkDirty();
        BumpCanvas();
        Log($"Removed {doomed.Count} object(s)");
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
            vlt.ConnectionLost += OnVltConnectionLost;
            await vlt.ConnectAsync();
            _vlts[SelectedProjector.Id] = vlt;
            OnPropertyChanged(nameof(ActiveDeviceName));
            RefreshDeviceLink();
        }
        catch
        {
            // Already logged in VltProjector.
        }
    }

    [RelayCommand]
    private async Task UseVirtualAsync()
    {
        UseVlt = false;
        foreach (var id in _vlts.Keys.ToList())
            await DisconnectProjectorAsync(id);
        await _virtual.ConnectAsync();
        OnPropertyChanged(nameof(ActiveDeviceName));
        RefreshDeviceLink();
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
            }
            else
            {
                await _hub.StartEndpointAsync(binary);
                UdpEnabled = true;
            }
            RefreshEndpointItems();
        }
        catch (Exception ex)
        {
            UdpEnabled = false;
            Log($"UDP failed: {ex.Message}", LogMessageStatus.Error);
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

                if (cmds.Contains("ALIGN"))
                    Log("ALIGN received (not wired yet)", LogMessageStatus.Warning);

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
                        await PlayCoreAsync();
                }

                if (stop)
                    await StopAsync();

                if (e.ReplyRequested)
                    await _hub.SendReplyAsync(e, "OK");
            }
            catch (Exception ex)
            {
                Log($"Automation handle failed: {ex.Message}", LogMessageStatus.Error);
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
        if (play) await PlayCoreAsync();
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
        if (!await ConfirmFirstPlayIfNeededAsync()) return;
        await PlayCoreAsync();
    }

    private async Task PlayCoreAsync()
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

            var bags = _pipeline.BuildPerDevice(SelectedScene, Project, sceneDevices);
            await SendBagsAsync(bags, "Play");
        }
        catch (Exception ex)
        {
            Log($"Play failed: {ex.Message}", LogMessageStatus.Error);
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

            _lastDeviceFrames[p.Id] = bag;
            PushFrameToPreview(p, bag);

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

        FrameInfo = parts.Count == 0 ? "No frame" : string.Join(" | ", parts);
        SelectedScene.IsPlaying = true;
        IsPlaying = true;
        RefreshDeviceLink();
        Log($"{label} — {FrameInfo}");
        BumpCanvas();
    }

    private void ShowProjectorPreview(ProjectorProfile device)
    {
        if (HostWindow is null) return;

        if (_previewWindows.TryGetValue(device.Id, out var existing))
        {
            existing.Activate();
            RefreshPreviewForDevice(device);
            return;
        }

        var vm = new ProjectorPreviewViewModel(
            device.Id,
            device.DisplayName,
            refresh: async () =>
            {
                RefreshPreviewForDevice(device);
                await Task.CompletedTask;
            },
            openInViewer: async () => await OpenPreviewInIldaViewerAsync(device));

        var win = new ProjectorPreviewWindow { DataContext = vm };
        win.Closed += (_, _) => _previewWindows.Remove(device.Id);
        _previewWindows[device.Id] = win;
        win.Show(HostWindow);
        RefreshPreviewForDevice(device);
    }

    private async Task OpenPreviewInIldaViewerAsync(ProjectorProfile device)
    {
        try
        {
            if (!_lastDeviceFrames.TryGetValue(device.Id, out var bag))
            {
                RefreshPreviewForDevice(device);
                _lastDeviceFrames.TryGetValue(device.Id, out bag);
            }

            if (bag is null || bag.Points.Count == 0)
            {
                Log("Preview: empty frame — nothing to open in ILDAViewer");
                return;
            }

            var ilda = DeviceIldaEncoder.FromDeviceBag(bag, device);
            var dir = Path.Combine(Path.GetTempPath(), "2Cut");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"preview-{SanitizeFilePart(device.DisplayName)}.ild");
            await IldaFileWriter.WriteAsync(path, ilda);

            if (IldaViewerLauncher.TryOpen(path, out var error))
                Log($"Opened ILDAViewer: {path}");
            else
                Log($"ILDAViewer: {error}");
        }
        catch (Exception ex)
        {
            Log($"ILDAViewer failed: {ex.Message}");
        }
    }

    private static string SanitizeFilePart(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "device" : name;
    }

    private void RefreshPreviewForDevice(ProjectorProfile device)
    {
        if (SelectedScene is null) return;
        try
        {
            PushDeviceFieldsToSelection();
            var sceneDevices = GetSceneProjectors();
            if (sceneDevices.Count == 0)
                sceneDevices = Projectors.ToList();

            Dictionary<string, LinesCollection> bags;
            if (sceneDevices.Any(d => d.Id == device.Id) && sceneDevices.Count > 0)
                bags = _pipeline.BuildPerDevice(SelectedScene, Project, sceneDevices);
            else
                bags = new Dictionary<string, LinesCollection>
                {
                    [device.Id] = _pipeline.BuildFrame(SelectedScene, Project, device)
                };

            if (!bags.TryGetValue(device.Id, out var bag))
                bag = new LinesCollection();

            _lastDeviceFrames[device.Id] = bag;
            PushFrameToPreview(device, bag);
        }
        catch (Exception ex)
        {
            Log($"Preview failed: {ex.Message}");
        }
    }

    private void PushFrameToPreview(ProjectorProfile device, LinesCollection bag)
    {
        if (!_previewWindows.TryGetValue(device.Id, out var win))
            return;

        var ilda = DeviceIldaEncoder.FromDeviceBag(bag, device);
        win.ApplyIldaFrame(
            ilda,
            $"ILDA pts={ilda.Points.Count}  (same encode as VLT, res={device.WidthResolution}×{device.HeightResolution})");
    }

    private void SchedulePreviewRefresh()
    {
        if (_previewWindows.Count == 0) return;
        _previewRefreshTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _previewRefreshTimer.Tick -= OnPreviewRefreshTick;
        _previewRefreshTimer.Tick += OnPreviewRefreshTick;
        _previewRefreshTimer.Stop();
        _previewRefreshTimer.Start();
    }

    private void OnPreviewRefreshTick(object? sender, EventArgs e)
    {
        _previewRefreshTimer?.Stop();
        if (_previewWindows.Count == 0 || SelectedScene is null) return;
        try
        {
            PushDeviceFieldsToSelection();
            var sceneDevices = GetSceneProjectors();
            if (sceneDevices.Count == 0) return;
            var bags = _pipeline.BuildPerDevice(SelectedScene, Project, sceneDevices);
            foreach (var p in sceneDevices)
            {
                if (!bags.TryGetValue(p.Id, out var bag)) continue;
                _lastDeviceFrames[p.Id] = bag;
                PushFrameToPreview(p, bag);
            }
        }
        catch (Exception ex)
        {
            Log($"Preview refresh: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        foreach (var v in _vlts.Values)
            await v.StopAsync();
        await _virtual.StopAsync();
        if (SelectedScene is not null) SelectedScene.IsPlaying = false;
        IsPlaying = false;
        RefreshDeviceLink();
        Log("Stop");
    }

    private async Task ImportPathAsync(string path, bool clear, bool play)
    {
        if (IsBusy)
        {
            Log("Busy — finish or cancel the current operation first");
            return;
        }

        var name = Path.GetFileName(path);
        _importCts?.Cancel();
        _importCts?.Dispose();
        _importCts = new CancellationTokenSource();
        var ct = _importCts.Token;
        var progress = new Progress<ImportProgress>(ReportBusy);

        IsImporting = true;
        var busyName = string.Format(
            UiLanguage.Text("Ui.ImportBusy", "Importing {0}…"), name);
        BeginBusy(busyName, CancelBusy, determinate: true);

        try
        {
            if (path.EndsWith(".stl", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyStlTargetAsync(path, ct, progress);
                return;
            }

            if (path.EndsWith(".cproj", StringComparison.OrdinalIgnoreCase))
            {
                Project = await ProjectJsonStore.LoadAsync(path, ct);
                SetProjectPath(path);
                await ApplyProjectDocumentAsync(clearDirty: true);
                RememberLastProject(path);
                Log($"Loaded {Path.GetFileName(path)}");
                if (play) await PlayCoreAsync();
                return;
            }

            if (LegacyImportService.CanImport(path))
            {
                await ImportLegacyPathAsync(path, clear, play, ct, progress);
                return;
            }

            var result = await _import.ImportAsync(path, ct, progress);
            ct.ThrowIfCancellationRequested();

            ReportBusy(new ImportProgress(
                UiLanguage.Text("Ui.ImportApplying", "Applying to scene…"), 0.98));

            var scene = SelectedScene ?? Project.ActiveScene;
            if (clear) scene.Drawables.Clear();

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
            Log($"Applied to scene → dock {DockMode}", LogMessageStatus.Info);
            if (play) await PlayCoreAsync();
        }
        catch (OperationCanceledException)
        {
            // DrawingImportService already logs cancel.
        }
        catch (Exception ex)
        {
            // DrawingImportService CadLog.Error's before rethrow; legacy/cproj do not.
            if (!path.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                Log($"Import failed: {ex.Message}", LogMessageStatus.Error);
        }
        finally
        {
            IsImporting = false;
            _importCts?.Dispose();
            _importCts = null;
            ClearBusyUi();
        }
    }

    private async Task ApplyStlTargetAsync(
        string path,
        CancellationToken ct,
        IProgress<ImportProgress>? progress)
    {
        var scene = SelectedScene ?? Project.ActiveScene;
        var mesh = await StlImporter.LoadAsync(path, ct, progress);
        if (mesh.TriangleCount > MeshTarget.TriangleWarnLimit)
        {
            Log(string.Format(
                    UiLanguage.Text("Ui.StlHeavy", "STL has {0} triangles (limit {1}) — viewport may slow down"),
                    mesh.TriangleCount,
                    MeshTarget.TriangleWarnLimit),
                LogMessageStatus.Warning);
        }

        var target = new MeshTarget { Mesh = mesh, SourcePath = path };
        target.FitToPlane(scene.Target.WidthMm, scene.Target.HeightMm);
        scene.MeshTarget = target;
        RefreshMeshTargetUi();
        History.Clear();
        MarkDirty();
        BumpCanvas();
        ViewResetRequested?.Invoke();
        IsStlAlignOpen = true;
        Log(string.Format(
            UiLanguage.Text("Ui.StlLoaded", "STL target {0} ({1} triangles)"),
            Path.GetFileName(path),
            mesh.TriangleCount));
    }

    private async Task ImportLegacyAsProjectAsync(string path)
    {
        var imported = await _legacy.ImportAsync(path);
        LogMigration(imported.Report);
        Project = imported.Project;
        SetProjectPath(null);
        await ApplyProjectDocumentAsync(clearDirty: false);
        Log(string.Format(
            UiLanguage.Text("Ui.LegacyReadOnly", "Imported {0} — save as .cproj (legacy files are read-only)"),
            Path.GetFileName(path)));
    }

    private async Task ImportLegacyPathAsync(
        string path,
        bool clear,
        bool play,
        CancellationToken ct,
        IProgress<ImportProgress>? progress)
    {
        var imported = await _legacy.ImportAsync(path, ct, progress);
        LogMigration(imported.Report);

        if (imported.Report.Kind == LegacyKind.Hub || imported.HasDevices)
        {
            Project = imported.Project;
            SetProjectPath(null);
            await ApplyProjectDocumentAsync(clearDirty: false);
            Log(string.Format(
                UiLanguage.Text("Ui.LegacyReadOnly", "Imported {0} — save as .cproj (legacy files are read-only)"),
                Path.GetFileName(path)));
            if (play) await PlayCoreAsync();
            return;
        }

        var incoming = imported.Project.Scenes[0];
        var scene = SelectedScene ?? Project.ActiveScene;
        var startIndex = scene.Drawables.Count;
        if (clear)
        {
            scene.Drawables.Clear();
            startIndex = 0;
            scene.Name = incoming.Name;
            scene.Target.WidthMm = incoming.Target.WidthMm;
            scene.Target.HeightMm = incoming.Target.HeightMm;
            scene.Mask.IsEnabled = incoming.Mask.IsEnabled;
            scene.Mask.Bounds = incoming.Mask.Bounds;
            MaskEnabled = scene.Mask.IsEnabled;
            ShowMaskOverlay = scene.Mask.IsEnabled;
            PullSceneUiFromSelection();
        }

        scene.Drawables.AddRange(incoming.Drawables);
        RefreshObjectNames();
        if (incoming.Drawables.Count > 0)
            SelectedObjectIndex = startIndex;
        if (clear) History.Clear();
        MarkDirty();
        BumpCanvas();
        if (clear) ViewResetRequested?.Invoke();
        Log(string.Format(
            UiLanguage.Text("Ui.LegacySceneImported", "Imported scene {0} ({1} objects)"),
            Path.GetFileName(path),
            incoming.Drawables.Count));
        if (play) await PlayCoreAsync();
    }

    private async Task ApplyProjectDocumentAsync(bool clearDirty)
    {
        Scenes.Clear();
        foreach (var s in Project.Scenes)
            Scenes.Add(s);
        SelectedScene = Project.ActiveScene;
        MaskEnabled = SelectedScene.Mask.IsEnabled;
        ShowMaskOverlay = SelectedScene.Mask.IsEnabled;
        UseLayerColor = Project.ColorMode == LaserColorMode.LayerColor;
        await ApplyLoadedDevicesAsync(Project);
        RefreshObjectNames();
        RefreshFovOverlays();
        History.Clear();
        if (clearDirty)
            ClearDirty();
        else
            MarkDirty();
        BumpCanvas();
        ViewResetRequested?.Invoke();
        RefreshWindowTitle();
    }

    private void LogMigration(LegacyMigrationReport report)
    {
        foreach (var line in report.AllLines())
        {
            var status = line.StartsWith("ok", StringComparison.Ordinal)
                ? LogMessageStatus.Info
                : LogMessageStatus.Warning;
            Log(line, status);
        }
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
        PoseZ = p.Pose.PositionMm.Z;
        PosePitch = p.Pose.PitchDeg;
        PoseYaw = p.Pose.YawDeg;
        PoseRoll = p.Pose.RollDeg;
        FovHDeg = p.Pose.FovHDeg;
        FovVDeg = p.Pose.FovVDeg;
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
        ApplyPoseFields(SelectedProjector);
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
        ApplyPoseFields(SelectedProjector);
        RefreshFovOverlays();
        BumpCanvas();
    }

    private void ApplyPoseFields(ProjectorProfile p)
    {
        p.Pose.PositionMm = new Point3(PoseX, PoseY, PoseZ);
        p.Pose.PitchDeg = PosePitch;
        p.Pose.YawDeg = PoseYaw;
        p.Pose.RollDeg = PoseRoll;
        p.Pose.FovHDeg = Math.Clamp(FovHDeg, 1, 170);
        p.Pose.FovVDeg = Math.Clamp(FovVDeg, 1, 170);
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
        vlt.ConnectionLost -= OnVltConnectionLost;
        try { await vlt.DisconnectAsync(); } catch { /* ignore */ }
        vlt.Dispose();
        _vlts.Remove(id);
        RefreshDeviceLink();
    }

    private void LoadTransformFromSelection()
    {
        if (ShouldEditModule() && TryGetSelectedAnchor(out _, out _, out var anchor, out var bounds))
        {
            var world = AnchorToWorld(anchor, bounds);
            Tx = world.X;
            Ty = world.Y;
            Tz = 0;
            return;
        }

        var d = GetFocusedDrawable();
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

        // Legacy ProjectorMesh.MX/MY: the transform panel writes the selected mesh (or module) point.
        if (ShouldEditModule() && TryGetSelectedAnchor(out var cfg, out _, out var anchor, out var bounds))
        {
            ApplyModuleAnchor(cfg.Id, anchor.Index, WorldToAnchorUnit(Tx, Ty, bounds));
            return;
        }

        var d = GetFocusedDrawable();
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

    private Drawable? GetFocusedDrawable() =>
        SelectedObjectItem?.Drawable ?? GetSelectedDrawable();

    private Drawable? GetSelectedDrawable()
    {
        if (SelectedScene is null) return null;
        if (SelectedObjectIndex < 0 || SelectedObjectIndex >= SelectedScene.Drawables.Count)
            return null;
        return SelectedScene.Drawables[SelectedObjectIndex];
    }

    private void BumpCanvas()
    {
        CanvasRevision++;
        SchedulePreviewRefresh();
    }

    private void Log(string message, LogMessageStatus status = LogMessageStatus.Regular)
        => CadLogging.Post?.Invoke(message, status);

    private void RefreshObjectNames()
    {
        var keepId = SelectedObjectItem?.Drawable.Id;
        foreach (var old in ObjectItems)
            old.UnwireTreeEvents(OnObjectVisibilityChanged, OnObjectNameEdited);
        ObjectItems.Clear();
        if (SelectedScene is null)
        {
            SelectedObjectItem = null;
            PullObjectUiFromSelection();
            return;
        }

        for (var i = 0; i < SelectedScene.Drawables.Count; i++)
        {
            var item = new ObjectListItem(SelectedScene.Drawables[i], i);
            item.WireTreeEvents(OnObjectVisibilityChanged, OnObjectNameEdited);
            ObjectItems.Add(item);
        }

        ObjectListItem? match = null;
        if (keepId is Guid id)
            match = ObjectItems.SelectMany(r => r.EnumerateSelfAndDescendants())
                .FirstOrDefault(x => x.Drawable.Id == id);
        if (match is null && SelectedObjectIndex >= 0 && SelectedObjectIndex < ObjectItems.Count)
            match = ObjectItems[SelectedObjectIndex];

        _suppressObjectSelection = true;
        SelectedObjectItem = match;
        _suppressObjectSelection = false;
        PullObjectUiFromSelection();
    }

    private void OnObjectVisibilityChanged(object? sender, EventArgs e) => BumpCanvas();

    private void OnObjectNameEdited(object? sender, EventArgs e)
    {
        if (sender is not ObjectListItem item) return;
        PullObjectUiFromSelection();
        MarkDirty();
    }

    private void PullObjectUiFromSelection()
    {
        _suppressObjectUi = true;
        var d = GetFocusedDrawable();
        ObjectName = d?.Name ?? "";
        _suppressObjectUi = false;
    }

    partial void OnObjectNameChanged(string value)
    {
        if (_suppressObjectUi) return;
        var d = GetFocusedDrawable();
        if (d is null) return;
        var before = d.Name;
        if (string.Equals(before, value, StringComparison.Ordinal)) return;
        d.Name = value;
        if (SelectedObjectItem is { } item)
            item.Name = value;
        Record(
            new ValueEdit<string>(
                $"{Hist("Rename", "Rename")} {value}",
                v =>
                {
                    d.Name = v;
                    if (SelectedObjectItem is { } row)
                        row.Name = v;
                    PullObjectUiFromSelection();
                },
                before,
                value),
            $"drawable:{d.Id}:name");
        MarkDirty();
    }

    partial void OnDxfUnitChoiceChanged(string value)
    {
        _import.DxfUnits = ParseDxfUnits(value);
        if (!_suppressPrefs)
            PersistPrefs();
    }

    partial void OnUdpPortChanged(int value)
    {
        if (value is < 1 or > 65535) return;
        var binary = _hub.Endpoints.FirstOrDefault(e => e.Type == RemoteEndpointType.UdpBinary);
        if (binary is not null && !_hub.IsListening(binary))
            binary.Port = value;
        if (!_suppressPrefs)
            PersistPrefs();
    }

    private void ApplyPrefs()
    {
        _suppressPrefs = true;
        try
        {
            var prefs = AppPrefs.Load();
            LanguageCode = UiLanguage.Current.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? "RU" : "EN";
            if (prefs.UdpPort is > 0 and <= 65535)
                UdpPort = prefs.UdpPort;
            if (!string.IsNullOrWhiteSpace(prefs.UdpBindIp))
                NewEndpointBindIp = prefs.UdpBindIp!;
            DxfUnitChoice = FormatDxfUnits(prefs.DxfUnitPreference);
            _import.DxfUnits = prefs.DxfUnitPreference;
            LoadHotkeysFromPrefs(prefs);

            var binary = _hub.Endpoints.FirstOrDefault(e => e.Type == RemoteEndpointType.UdpBinary);
            if (binary is not null)
            {
                binary.Port = UdpPort;
                if (!string.IsNullOrWhiteSpace(NewEndpointBindIp))
                    binary.BindIp = NewEndpointBindIp;
            }
        }
        finally
        {
            _suppressPrefs = false;
        }
        RefreshDeviceLink();
    }

    private void PersistPrefs()
    {
        AppPrefs.Update(s =>
        {
            s.Language = UiLanguage.Current;
            s.UdpPort = UdpPort;
            s.UdpBindIp = NewEndpointBindIp;
            s.DxfUnits = ParseDxfUnits(DxfUnitChoice).ToString();
            s.NudgeStepMm = NudgeStepMm;
            s.Hotkeys = Hotkeys.ToPrefs();
            s.Workspace = CaptureWorkspaceLayout();
        });
    }

    public void LoadWorkspaceLayout()
    {
        var layout = AppPrefs.Load().Workspace ?? new WorkspaceLayoutPrefs();
        Workspace.LeftWidth = Clamp(layout.LeftWidth, 220, 480, 300);
        Workspace.RightWidth = Clamp(layout.RightWidth, 280, 560, 340);
        Workspace.BottomHeight = Clamp(layout.BottomHeight, 120, 480, 200);
        Workspace.IsBottomOpen = layout.IsBottomOpen;
        if (Enum.TryParse<PanelId>(layout.ActiveLeft, out var left))
            Workspace.ActiveLeft = left;
        if (Enum.TryParse<PanelId>(layout.ActiveRight, out var right))
            Workspace.Reveal(right);
        if (Enum.TryParse<PanelId>(layout.ActiveBottom, out var bottom) && layout.IsBottomOpen)
            Workspace.Reveal(bottom);
    }

    public void SaveWorkspaceLayout(double windowWidth, double windowHeight, bool maximized)
    {
        AppPrefs.Update(s =>
        {
            s.Workspace = CaptureWorkspaceLayout();
            s.Workspace.WindowWidth = windowWidth;
            s.Workspace.WindowHeight = windowHeight;
            s.Workspace.IsMaximized = maximized;
        });
    }

    private WorkspaceLayoutPrefs CaptureWorkspaceLayout() => new()
    {
        ActiveLeft = Workspace.ActiveLeft?.ToString(),
        ActiveRight = Workspace.ActiveRight?.ToString(),
        ActiveBottom = Workspace.IsBottomOpen
            ? Workspace.BottomTabIndex switch
            {
                1 => PanelId.Endpoints.ToString(),
                2 => PanelId.Clients.ToString(),
                3 => PanelId.Commands.ToString(),
                _ => PanelId.Logs.ToString()
            }
            : null,
        LeftWidth = Workspace.LeftWidth,
        RightWidth = Workspace.RightWidth,
        BottomHeight = Workspace.BottomHeight,
        IsBottomOpen = Workspace.IsBottomOpen
    };

    private static double Clamp(double value, double min, double max, double fallback)
        => value >= min && value <= max ? value : fallback;

    private static DxfUnitPreference ParseDxfUnits(string? choice) => choice switch
    {
        "mm" => DxfUnitPreference.Millimeters,
        "cm" => DxfUnitPreference.Centimeters,
        "m" => DxfUnitPreference.Meters,
        "in" => DxfUnitPreference.Inches,
        "ft" => DxfUnitPreference.Feet,
        _ => DxfUnitPreference.Auto
    };

    private static string FormatDxfUnits(DxfUnitPreference pref) => pref switch
    {
        DxfUnitPreference.Millimeters => "mm",
        DxfUnitPreference.Centimeters => "cm",
        DxfUnitPreference.Meters => "m",
        DxfUnitPreference.Inches => "in",
        DxfUnitPreference.Feet => "ft",
        _ => "Auto"
    };

    private List<Drawable> SnapshotTree() =>
        SelectedScene is null ? [] : SelectedScene.Drawables.Select(d => d.CloneTree()).ToList();

    private void RecordTreeChange(string label, List<Drawable> before)
    {
        if (SelectedScene is null) return;
        var after = SnapshotTree();
        var scene = SelectedScene;
        Record(
            new ValueEdit<List<Drawable>>(
                label,
                list =>
                {
                    scene.Drawables.Clear();
                    foreach (var d in list)
                        scene.Drawables.Add(d.CloneTree());
                    RefreshObjectNames();
                    BumpCanvas();
                },
                before,
                after));
        History.Break();
    }

    private async Task<bool> ConfirmFirstPlayIfNeededAsync()
    {
        if (_firstPlayConfirmed) return true;
        var laserLive = UseVlt && _vlts.Values.Any(v => v.IsConnected);
        if (!laserLive)
        {
            _firstPlayConfirmed = true;
            return true;
        }

        if (HostWindow is null) return true;
        var ok = await ConfirmDialog.OkCancelAsync(
            HostWindow,
            UiLanguage.Text("Ui.FirstPlayTitle", "Laser output"),
            UiLanguage.Text("Ui.FirstPlayMessage", "The projector will start. Confirm the work area is clear."));
        if (!ok) return false;
        _firstPlayConfirmed = true;
        return true;
    }

    private void StartLinkWatch()
    {
        _linkWatch = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _linkWatch.Tick += (_, _) =>
        {
            if (_vlts.Count == 0) return;
            foreach (var vlt in _vlts.Values.ToList())
                vlt.ProbeAlive();
        };
        _linkWatch.Start();
    }

    private void OnVltConnectionLost(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            if (sender is VltProjector vlt)
            {
                vlt.ConnectionLost -= OnVltConnectionLost;
                _vlts.Remove(vlt.Id);
                try { vlt.Dispose(); } catch { /* ignore */ }
            }

            await StopAsync();
            LaserAlert = true;
            RefreshDeviceLink();
            OnPropertyChanged(nameof(ActiveDeviceName));
            Log(UiLanguage.Text("Ui.LinkLost", "Projector link lost — laser stopped"), LogMessageStatus.Error);
        });
    }

    private void RefreshDeviceLink()
    {
        if (_vlts.Count == 0)
        {
            LaserAlert = false;
            DeviceLinkText = UseVlt
                ? UiLanguage.Text("Ui.LinkNone", "VLT: not connected")
                : UiLanguage.Text("Ui.LinkVirtual", "Virtual");
            return;
        }

        var live = _vlts.Values.Where(v => v.IsConnected).Select(v => v.DisplayName).ToList();
        LaserAlert = live.Count == 0;
        DeviceLinkText = live.Count == 0
            ? UiLanguage.Text("Ui.LinkLostShort", "VLT: LOST")
            : string.Join(", ", live.Select(n => $"{n}*"));
    }
}
