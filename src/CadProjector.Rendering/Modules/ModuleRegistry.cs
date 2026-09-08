using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CadProjector.Core.Devices;

namespace CadProjector.Rendering.Modules;

public enum ModuleParamKind
{
    Double,
    Int,
    Bool,
    Text,
    Choice
}

/// <summary>One named value in a <see cref="ModuleParamKind.Choice"/> list.</summary>
public sealed class ModuleParamChoice
{
    public required string Value { get; init; }
    public required string Label { get; init; }

    public override string ToString() => Label;
}

public sealed class ModuleParamDescriptor
{
    /// <summary>Editors are decimal-based, so unbounded parameters use a wide but representable range.</summary>
    public const double NoLimit = 1e9;

    public required string Key { get; init; }
    public required string Label { get; init; }
    public required ModuleParamKind Kind { get; init; }
    public double Min { get; init; } = -NoLimit;
    public double Max { get; init; } = NoLimit;
    public double Increment { get; init; } = 1;
    public string Format { get; init; } = "0.######";
    public string DefaultValue { get; init; } = "";
    public IReadOnlyList<ModuleParamChoice> Choices { get; init; } = [];
}

public sealed class ModuleDescriptor
{
    public required string TypeId { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required Func<IFrameModule> Factory { get; init; }
    public IReadOnlyList<ModuleParamDescriptor> Parameters { get; init; } = [];

    /// <summary>True when the module draws geometry on the table.</summary>
    public bool HasGeometry { get; init; }
}

/// <summary>
/// Catalog of every render module. A chain is an ordered list of <see cref="DeviceModuleConfig"/>,
/// so any module can appear any number of times, in any order, on either side of the mesh.
/// </summary>
public static class ModuleRegistry
{
    private static readonly Dictionary<string, ModuleDescriptor> Map;

    public static IReadOnlyList<ModuleDescriptor> All { get; }

    static ModuleRegistry()
    {
        All =
        [
            Describe(ModuleTypes.Unduplicate, "Unduplicate", "Убирает наложенные коллинеарные отрезки",
                () => new UnduplicateModule()),
            Describe(ModuleTypes.ShortestPath, "Shortest Path", "Сшивает отрезки в цепочки и сокращает холостой ход",
                () => new ShortestPathModule()),
            Describe(ModuleTypes.BlankBridge, "Blank Bridge", "Вставляет холостой переход между разорванными концами",
                () => new BlankBridgeModule()),
            Describe(ModuleTypes.PointMass, "Point Mass", "Задаёт вес точки (задержку) по углу поворота луча",
                () => new PointMassModule()),
            Describe(ModuleTypes.ScanRateGradient, "Scan Rate Gradient", "Сгущает точки к краям отрезка по scan rate",
                () => new ScanRateGradientModule()),
            Describe(ModuleTypes.ScanRateSplit, "Scan Rate Split", "Равномерно дробит отрезки под scan rate",
                () => new ScanRateSplitModule()),
            Describe(ModuleTypes.Mesh, "Mesh", "Калибровочная сетка: до неё — сцена, после — координаты проектора",
                () => new CalibrationMeshModule()),
            Describe(ModuleTypes.BlankCircle, "Blank Circle", "Добавляет холостую окружность в кадр",
                () => new BlankCircleModule()),
            Describe(ModuleTypes.LinesSkipper, "Lines Skipper", "Берёт страницу из N отрезков",
                () => new LinesSkipperModule()),
            Describe(ModuleTypes.LinesGroupSplitter, "Lines Group Splitter", "Берёт страницу из связных групп отрезков",
                () => new LinesGroupSplitterModule()),
            Describe(ModuleTypes.LineGridSplitter, "Line Grid Splitter", "Режет отрезки по сетке и обрезает по кадру",
                () => new LineGridSplitterModule()),
            Describe(ModuleTypes.AroundZero, "Around Zero", "Сдвигает кадр в систему координат с центром в нуле",
                () => new AroundZeroModule()),
            Describe(ModuleTypes.ResolutionMultiplier, "Resolution", "Масштабирует кадр в половину разрешения",
                () => new ResolutionMultiplierModule()),
            Describe(ModuleTypes.Rotate2D, "Rotate 2D", "Поворот кадра вокруг точки",
                () => new Rotate2DModule()),
            Describe(ModuleTypes.MoveScale2D, "Move / Scale 2D", "Сдвиг и масштаб кадра",
                () => new MoveScale2DModule()),
            Describe(ModuleTypes.RectProportion, "Rect Proportion", "Обрезает по прямоугольнику и нормализует в 0..1",
                () => new RectProportionModule()),
            Describe(ModuleTypes.ArctanCorrector, "Arctan Corrector", "Коррекция геометрии гальвосканера (atan)",
                () => new ArctanCorrectorModule()),
            Describe(ModuleTypes.AxisGradient, "Axis Gradient", "Ручная 1D-коррекция по оси (таблица значений)",
                () => new AxisGradientModule()),
            Describe(ModuleTypes.DeepFrameCutter, "Deep Frame Cutter", "Обрезает кадр рамкой с учётом глубины",
                () => new DeepFrameCutterModule()),
            Describe(ModuleTypes.ZCorrector, "Z Corrector", "Перспективная поправка XY по глубине Z",
                () => new ZCorrectorModule())
        ];

        Map = All.ToDictionary(d => d.TypeId, StringComparer.OrdinalIgnoreCase);
    }

    public static ModuleDescriptor? Find(string? typeId) =>
        typeId is not null && Map.TryGetValue(typeId, out var d) ? d : null;

    /// <summary>New config for a chain entry, pre-filled with the module's own defaults.</summary>
    public static DeviceModuleConfig CreateConfig(string typeId)
    {
        var d = Find(typeId) ?? All[0];
        var cfg = new DeviceModuleConfig
        {
            TypeId = d.TypeId,
            DisplayName = d.DisplayName,
            IsEnabled = true,
            ProjectGeometry = false
        };
        foreach (var p in d.Parameters)
            cfg.Params[p.Key] = p.DefaultValue;
        cfg.EnsureMesh();
        return cfg;
    }

    /// <summary>
    /// Instantiate a module and apply the config's parameters. Mesh-shaped modules get the config's
    /// live grid, so edits made on the table are picked up without a copy back.
    /// </summary>
    public static IFrameModule? Materialize(DeviceModuleConfig cfg)
    {
        var descriptor = Find(cfg.TypeId);
        if (descriptor is null)
            return null;

        var module = descriptor.Factory();

        if (module is CalibrationMeshModule mesh)
        {
            cfg.EnsureMesh();
            if (cfg.Mesh is not null)
                mesh.Grid = cfg.Mesh;
        }

        var type = module.GetType();
        foreach (var p in descriptor.Parameters)
        {
            if (!cfg.Params.TryGetValue(p.Key, out var raw) || string.IsNullOrEmpty(raw))
                continue;
            var prop = type.GetProperty(p.Key, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
                continue;
            if (TryConvert(raw, prop.PropertyType, out var value))
                prop.SetValue(module, value);
        }

        module.IsEnabled = true;
        return module;
    }

    /// <summary>Write a live module's parameters back into its config (used after dragging anchors).</summary>
    public static void Capture(IFrameModule module, DeviceModuleConfig cfg)
    {
        if (Find(cfg.TypeId) is not { } descriptor)
            return;

        var type = module.GetType();
        foreach (var p in descriptor.Parameters)
        {
            var prop = type.GetProperty(p.Key, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanRead)
                continue;
            cfg.Params[p.Key] = ModuleValue.Format(prop.GetValue(module));
        }
    }

    private static ModuleDescriptor Describe(
        string typeId,
        string displayName,
        string description,
        Func<IFrameModule> factory)
    {
        var prototype = factory();
        return new ModuleDescriptor
        {
            TypeId = typeId,
            DisplayName = displayName,
            Description = description,
            Factory = factory,
            Parameters = DescribeParams(prototype),
            HasGeometry = prototype is IRenderableModule
        };
    }

    private static List<ModuleParamDescriptor> DescribeParams(IFrameModule prototype)
    {
        var result = new List<ModuleParamDescriptor>();
        foreach (var prop in prototype.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
                continue;
            if (prop.Name is nameof(IFrameModule.IsEnabled) or nameof(IFrameModule.Name))
                continue;
            if (prop.GetCustomAttribute<ModuleParamIgnoreAttribute>() is not null)
                continue;
            if (KindOf(prop.PropertyType) is not { } kind)
                continue;

            var meta = prop.GetCustomAttribute<ModuleParamAttribute>();
            var choices = kind == ModuleParamKind.Choice
                ? BuildChoices(prop.PropertyType)
                : Array.Empty<ModuleParamChoice>();
            result.Add(new ModuleParamDescriptor
            {
                Key = prop.Name,
                Label = meta?.Label ?? prop.Name,
                Kind = kind,
                Min = Pick(meta?.Min, kind == ModuleParamKind.Int ? 0 : -ModuleParamDescriptor.NoLimit),
                Max = Pick(meta?.Max, ModuleParamDescriptor.NoLimit),
                Increment = Pick(meta?.Increment, kind == ModuleParamKind.Int ? 1 : 0.01),
                Format = meta?.Format ?? (kind == ModuleParamKind.Int ? "0" : "0.######"),
                DefaultValue = ModuleValue.Format(prop.GetValue(prototype)),
                Choices = choices
            });
        }
        return result;
    }

    private static IReadOnlyList<ModuleParamChoice> BuildChoices(Type enumType)
    {
        var list = new List<ModuleParamChoice>();
        foreach (var name in Enum.GetNames(enumType))
        {
            var member = enumType.GetField(name);
            var label = member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
            var value = Convert.ToInt32(Enum.Parse(enumType, name));
            list.Add(new ModuleParamChoice
            {
                Value = value.ToString(CultureInfo.InvariantCulture),
                Label = label
            });
        }
        return list;
    }

    private static double Pick(double? candidate, double fallback) =>
        candidate is { } v && !double.IsNaN(v) ? v : fallback;

    private static ModuleParamKind? KindOf(Type t)
    {
        if (t == typeof(bool)) return ModuleParamKind.Bool;
        if (t == typeof(double) || t == typeof(float)) return ModuleParamKind.Double;
        if (t == typeof(byte) || t == typeof(short) || t == typeof(ushort) ||
            t == typeof(int) || t == typeof(uint) || t == typeof(long)) return ModuleParamKind.Int;
        if (t == typeof(string)) return ModuleParamKind.Text;
        if (t.IsEnum) return ModuleParamKind.Choice;
        return null;
    }

    private static bool TryConvert(string raw, Type target, out object? value)
    {
        value = null;
        if (target == typeof(string))
        {
            value = raw;
            return true;
        }
        if (target == typeof(bool))
        {
            if (!bool.TryParse(raw, out var b)) return false;
            value = b;
            return true;
        }
        if (target.IsEnum)
        {
            if (Enum.TryParse(target, raw, ignoreCase: true, out var named))
            {
                value = named;
                return true;
            }
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                value = Enum.ToObject(target, n);
                return Enum.IsDefined(target, value);
            }
            return false;
        }
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ||
            double.IsNaN(d) || double.IsInfinity(d))
            return false;

        if (target == typeof(double)) value = d;
        else if (target == typeof(float)) value = (float)d;
        else if (target == typeof(byte)) value = (byte)Math.Clamp(Math.Round(d), byte.MinValue, byte.MaxValue);
        else if (target == typeof(short)) value = (short)Math.Clamp(Math.Round(d), short.MinValue, short.MaxValue);
        else if (target == typeof(ushort)) value = (ushort)Math.Clamp(Math.Round(d), ushort.MinValue, ushort.MaxValue);
        else if (target == typeof(int)) value = (int)Math.Clamp(Math.Round(d), int.MinValue, int.MaxValue);
        else if (target == typeof(uint)) value = (uint)Math.Clamp(Math.Round(d), uint.MinValue, uint.MaxValue);
        else if (target == typeof(long)) value = (long)Math.Clamp(Math.Round(d), long.MinValue, long.MaxValue);
        else return false;

        return true;
    }
}
