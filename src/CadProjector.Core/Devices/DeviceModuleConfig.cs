namespace CadProjector.Core.Devices;

/// <summary>Identifiers of the render modules ported from legacy MonchaSDK.</summary>
public static class ModuleTypes
{
    public const string Unduplicate = "Unduplicate";
    public const string ShortestPath = "ShortestPath";
    public const string BlankBridge = "BlankBridge";
    public const string PointMass = "PointMass";
    public const string ScanRateGradient = "ScanRateGradient";
    public const string ScanRateSplit = "ScanRateSplit";
    public const string BlankCircle = "BlankCircle";
    public const string LinesSkipper = "LinesSkipper";
    public const string LinesGroupSplitter = "LinesGroupSplitter";
    public const string LineGridSplitter = "LineGridSplitter";
    public const string AroundZero = "AroundZero";
    public const string ResolutionMultiplier = "ResolutionMultiplier";
    public const string Rotate2D = "Rotate2D";
    public const string MoveScale2D = "MoveScale2D";
    public const string RectProportion = "RectProportion";
    public const string ArctanCorrector = "ArctanCorrector";
    public const string AxisGradient = "AxisGradient";
    public const string ZCorrector = "ZCorrector";
    public const string DeepFrameCutter = "DeepFrameCutter";
    public const string Mesh = "Mesh";

    public static DeviceModuleConfig? FindMesh(IEnumerable<DeviceModuleConfig> chain) =>
        chain.FirstOrDefault(m => m.TypeId == Mesh);
}

/// <summary>Undo snapshot of a module instance; the grid is copied, not shared.</summary>
public sealed class ModuleState(
    bool isEnabled,
    bool showOnTable,
    bool projectGeometry,
    Dictionary<string, string> parameters,
    CalibrationMesh? mesh) : IEquatable<ModuleState>
{
    public bool IsEnabled { get; } = isEnabled;
    public bool ShowOnTable { get; } = showOnTable;
    public bool ProjectGeometry { get; } = projectGeometry;
    public Dictionary<string, string> Params { get; } = parameters;
    public CalibrationMesh? Mesh { get; } = mesh;

    public bool Equals(ModuleState? other)
    {
        if (other is null) return false;
        if (IsEnabled != other.IsEnabled
            || ShowOnTable != other.ShowOnTable
            || ProjectGeometry != other.ProjectGeometry
            || Params.Count != other.Params.Count)
            return false;

        foreach (var (key, value) in Params)
        {
            if (!other.Params.TryGetValue(key, out var mine) || mine != value)
                return false;
        }

        return SameGrid(Mesh, other.Mesh);
    }

    public override bool Equals(object? obj) => Equals(obj as ModuleState);

    public override int GetHashCode() => HashCode.Combine(IsEnabled, ShowOnTable, ProjectGeometry, Params.Count);

    private static bool SameGrid(CalibrationMesh? a, CalibrationMesh? b)
    {
        if (a is null || b is null) return ReferenceEquals(a, b);
        if (a.IsEnabled != b.IsEnabled
            || a.Columns != b.Columns
            || a.Rows != b.Rows
            || a.Morph != b.Morph)
            return false;

        for (var i = 0; i <= a.Columns; i++)
        for (var j = 0; j <= a.Rows; j++)
        {
            if (a.Points[i, j] != b.Points[i, j])
                return false;
        }

        return true;
    }
}

/// <summary>
/// One modifier instance in a projector's render chain. The list order is the execution order —
/// the calibration mesh is itself a module, so there is no separate "stage". Scalar settings live
/// in <see cref="Params"/>; modules whose geometry is a grid of points carry it in <see cref="Mesh"/>.
/// </summary>
public sealed class DeviceModuleConfig
{
    public string Id { get; set; } = NewId();
    public string TypeId { get; set; } = ModuleTypes.ZCorrector;
    public string DisplayName { get; set; } = "";
    public bool IsEnabled { get; set; } = true;

    /// <summary>Draw the module's own geometry on the table.</summary>
    public bool ShowOnTable { get; set; } = true;

    /// <summary>Feed the module's own geometry into the projected frame.</summary>
    public bool ProjectGeometry { get; set; }

    public Dictionary<string, string> Params { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Control-point grid for mesh-shaped modules; null for everything else.</summary>
    public CalibrationMesh? Mesh { get; set; }

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    public static DeviceModuleConfig Of(
        string typeId,
        bool enabled = true,
        params (string Key, object Value)[] parameters)
    {
        var cfg = new DeviceModuleConfig
        {
            TypeId = typeId,
            DisplayName = typeId,
            IsEnabled = enabled
        };
        foreach (var (key, value) in parameters)
            cfg.Params[key] = ModuleValue.Format(value);
        cfg.EnsureMesh();
        return cfg;
    }

    /// <summary>Creates the control-point grid if this module type needs one.</summary>
    public void EnsureMesh(int columns = 3, int rows = 3)
    {
        if (TypeId != ModuleTypes.Mesh)
            return;
        if (Mesh is not null)
            return;
        Mesh = new CalibrationMesh { IsEnabled = true, Morph = MeshMorphType.Full };
        Mesh.ResetIdentity(columns, rows);
    }

    public string? Get(string key) => Params.TryGetValue(key, out var v) ? v : null;

    public void Set(string key, object? value) => Params[key] = ModuleValue.Format(value);

    /// <summary>Legacy LProjector order, with the calibration mesh sitting in the middle.</summary>
    public static List<DeviceModuleConfig> DefaultChain() =>
    [
        Of(ModuleTypes.Unduplicate, true, ("Tolerance", 1e-6)),
        Of(ModuleTypes.ShortestPath, true, ("ConnectionTolerance", 1e-3)),
        Of(ModuleTypes.BlankBridge, true, ("Threshold", 1e-4)),
        Of(ModuleTypes.PointMass, true, ("LightMass", 8), ("BlankMass", 8)),
        Of(ModuleTypes.ScanRateGradient, true, ("ScanRate", 900), ("Fps", 1)),
        Of(ModuleTypes.DeepFrameCutter, false, ("Depth", 1.0), ("IgnoreHeight", false)),
        Of(ModuleTypes.Mesh, true,
            ("Columns", 3), ("Rows", 3),
            ("Morph", MeshMorphType.Full),
            ("Form", MeshCalibrationForm.Rect),
            ("MiniCrossSize", 0.02)),
        Of(ModuleTypes.ScanRateSplit, true, ("ScanRate", 40000), ("Fps", 30)),
        Of(ModuleTypes.ArctanCorrector, false, ("AngleX", 0.01), ("AngleY", 0.42)),
        Of(ModuleTypes.ZCorrector, true, ("Depth", 1.0), ("CenterX", 0.5), ("CenterY", 0.5))
    ];

    /// <summary>Migrate the early flat Z/DFC settings into a full chain.</summary>
    public static List<DeviceModuleConfig> FromLegacy(DeviceModuleSettings s)
    {
        var chain = DefaultChain();
        foreach (var m in chain)
        {
            if (m.TypeId == ModuleTypes.DeepFrameCutter)
            {
                m.IsEnabled = s.DfcEnabled;
                m.Set("Depth", s.DfcDepth);
                m.Set("IgnoreHeight", s.DfcIgnoreHeight);
            }
            else if (m.TypeId == ModuleTypes.ZCorrector)
            {
                m.IsEnabled = s.ZEnabled;
                m.Set("Depth", s.ZDepth);
                m.Set("CenterX", s.ZCenterX);
                m.Set("CenterY", s.ZCenterY);
            }
        }
        return chain;
    }

    /// <summary>Everything the user can change on this module instance, for undo/redo.</summary>
    public ModuleState CaptureState() => new(
        IsEnabled,
        ShowOnTable,
        ProjectGeometry,
        new Dictionary<string, string>(Params, StringComparer.OrdinalIgnoreCase),
        Mesh?.Clone());

    public void RestoreState(ModuleState state)
    {
        IsEnabled = state.IsEnabled;
        ShowOnTable = state.ShowOnTable;
        ProjectGeometry = state.ProjectGeometry;
        Params = new Dictionary<string, string>(state.Params, StringComparer.OrdinalIgnoreCase);
        Mesh = state.Mesh?.Clone();
    }

    public DeviceModuleConfig Clone() => new()
    {
        Id = NewId(),
        TypeId = TypeId,
        DisplayName = DisplayName,
        IsEnabled = IsEnabled,
        ShowOnTable = ShowOnTable,
        ProjectGeometry = ProjectGeometry,
        Params = new Dictionary<string, string>(Params, StringComparer.OrdinalIgnoreCase),
        Mesh = Mesh?.Clone()
    };
}
