using System.Text.Json;
using System.Text.Json.Serialization;
using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;
using CadProjector.FileFormats.Stl;
using CadProjector.Logging;

namespace CadProjector.FileFormats.ProjectJson;

public static class ProjectJsonStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task SaveAsync(ProjectDocument project, string path, CancellationToken ct = default)
    {
        project.SchemaVersion = ProjectDocument.CurrentSchemaVersion;
        var dto = ProjectDto.From(project);
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, dto, Options, ct);
        CadLog.Good($"Project saved: {Path.GetFileName(path)} ({project.Devices.Count} devices)");
    }

    public static async Task<ProjectDocument> LoadAsync(string path, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(path);
        var dto = await JsonSerializer.DeserializeAsync<ProjectDto>(fs, Options, ct)
            ?? throw new InvalidDataException("Empty project file.");
        var model = dto.ToModel();
        await AttachMeshTargetsAsync(dto, model, ct);
        CadLog.Good($"Project loaded: {Path.GetFileName(path)}");
        return model;
    }

    private sealed class ProjectDto
    {
        public int SchemaVersion { get; set; }
        public string Name { get; set; } = "Untitled";
        public LaserColorMode ColorMode { get; set; }
        public uint SolidColorArgb { get; set; }
        public int ActiveSceneIndex { get; set; }
        public List<SceneDto> Scenes { get; set; } = [];
        public MeshSnapshotDto? CalibrationMesh { get; set; }
        public List<DeviceSnapshotDto> Devices { get; set; } = [];

        public static ProjectDto From(ProjectDocument p) => new()
        {
            SchemaVersion = p.SchemaVersion,
            Name = p.Name,
            ColorMode = p.ColorMode,
            SolidColorArgb = p.SolidColorArgb,
            ActiveSceneIndex = p.ActiveSceneIndex,
            Scenes = p.Scenes.Select(SceneDto.From).ToList(),
            CalibrationMesh = p.CalibrationMesh is null ? null : MeshSnapshotDto.From(p.CalibrationMesh),
            Devices = p.Devices.Select(DeviceSnapshotDto.From).ToList()
        };

        public ProjectDocument ToModel() => new()
        {
            SchemaVersion = SchemaVersion == 0 ? 1 : SchemaVersion,
            Name = Name,
            ColorMode = ColorMode,
            SolidColorArgb = SolidColorArgb,
            ActiveSceneIndex = ActiveSceneIndex,
            Scenes = Scenes.Count == 0 ? [new ProjectionScene()] : Scenes.Select(s => s.ToModel()).ToList(),
            CalibrationMesh = CalibrationMesh?.ToModel(),
            Devices = Devices.Select(d => d.ToModel()).ToList()
        };
    }

    private static async Task AttachMeshTargetsAsync(ProjectDto dto, ProjectDocument model, CancellationToken ct)
    {
        for (var i = 0; i < dto.Scenes.Count && i < model.Scenes.Count; i++)
        {
            var src = dto.Scenes[i];
            if (string.IsNullOrWhiteSpace(src.MeshPath))
                continue;
            if (!File.Exists(src.MeshPath))
            {
                CadLog.Warn($"STL missing: {src.MeshPath}");
                continue;
            }

            try
            {
                var mesh = await StlImporter.LoadAsync(src.MeshPath, ct);
                model.Scenes[i].MeshTarget = new MeshTarget
                {
                    Mesh = mesh,
                    SourcePath = src.MeshPath,
                    Translation = new Point3(src.MeshTx, src.MeshTy, src.MeshTz),
                    RotationDeg = new Point3(src.MeshRx, src.MeshRy, src.MeshRz),
                    Scale = new Point3(
                        src.MeshSx == 0 ? 1 : src.MeshSx,
                        src.MeshSy == 0 ? 1 : src.MeshSy,
                        src.MeshSz == 0 ? 1 : src.MeshSz),
                    BreakOnMiss = src.MeshBreakOnMiss
                };
            }
            catch (Exception ex)
            {
                CadLog.Warn($"STL load failed ({src.MeshPath}): {ex.Message}");
            }
        }
    }

    private sealed class DeviceSnapshotDto
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "Projector";
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 10000;
        public bool UseVlt { get; set; }
        public double FovWidthMm { get; set; } = 1000;
        public double FovHeightMm { get; set; } = 1000;
        public double PoseX { get; set; }
        public double PoseY { get; set; }
        public double PoseZ { get; set; }
        public byte Red { get; set; } = 255;
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte Alpha { get; set; } = 255;
        public int WidthResolution { get; set; } = 65533;
        public int HeightResolution { get; set; } = 65533;
        public MeshSnapshotDto? Mesh { get; set; }
        public ModuleSettingsDto? Modules { get; set; }
        public List<ModuleConfigDto> ModuleChain { get; set; } = [];

        public static DeviceSnapshotDto From(DeviceSnapshot d) => new()
        {
            Id = d.Id,
            DisplayName = d.DisplayName,
            Host = d.Host,
            Port = d.Port,
            UseVlt = d.UseVlt,
            FovWidthMm = d.FovWidthMm,
            FovHeightMm = d.FovHeightMm,
            PoseX = d.PoseX,
            PoseY = d.PoseY,
            PoseZ = d.PoseZ,
            Red = d.Red,
            Green = d.Green,
            Blue = d.Blue,
            Alpha = d.Alpha,
            WidthResolution = d.WidthResolution,
            HeightResolution = d.HeightResolution,
            Mesh = d.Mesh is null ? null : MeshSnapshotDto.From(d.Mesh),
            Modules = d.Modules is null ? null : ModuleSettingsDto.From(d.Modules),
            ModuleChain = d.ModuleChain.Select(ModuleConfigDto.From).ToList()
        };

        public DeviceSnapshot ToModel() => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            Host = Host,
            Port = Port,
            UseVlt = UseVlt,
            FovWidthMm = FovWidthMm,
            FovHeightMm = FovHeightMm,
            PoseX = PoseX,
            PoseY = PoseY,
            PoseZ = PoseZ,
            Red = Red,
            Green = Green,
            Blue = Blue,
            Alpha = Alpha,
            WidthResolution = WidthResolution,
            HeightResolution = HeightResolution,
            Mesh = Mesh?.ToModel(),
            Modules = Modules?.ToModel(),
            ModuleChain = BuildChain()
        };

        /// <summary>
        /// Chains written before the mesh became a module are split into two stages. Re-emit them
        /// as one ordered list with a mesh module in between; the device-level mesh fills it in.
        /// </summary>
        private List<DeviceModuleConfig> BuildChain()
        {
            var chain = ModuleChain.Select(m => m.ToModel()).ToList();
            var staged = ModuleChain.Any(m => m.Stage is not null);
            if (!staged || chain.Any(c => c.TypeId == ModuleTypes.Mesh))
                return chain;

            var ordered = new List<DeviceModuleConfig>(chain.Count + 1);
            ordered.AddRange(chain.Where((_, i) => ModuleChain[i].Stage != LegacyStage.AfterMesh));
            ordered.Add(DeviceModuleConfig.Of(ModuleTypes.Mesh, true, ("Columns", 3), ("Rows", 3)));
            ordered.AddRange(chain.Where((_, i) => ModuleChain[i].Stage == LegacyStage.AfterMesh));
            return ordered;
        }
    }

    /// <summary>Stage flag of schema v3 chains, kept only to order them on load.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<LegacyStage>))]
    private enum LegacyStage
    {
        BeforeMesh,
        AfterMesh
    }

    private sealed class ModuleConfigDto
    {
        public string Id { get; set; } = "";
        public string TypeId { get; set; } = ModuleTypes.ZCorrector;
        public string DisplayName { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public bool ShowOnTable { get; set; } = true;
        public bool ProjectGeometry { get; set; }
        public Dictionary<string, string> Params { get; set; } = [];
        public MeshSnapshotDto? Mesh { get; set; }

        /// <summary>Set only by schema v3, when the mesh was still a fixed step between two stages.</summary>
        public LegacyStage? Stage { get; set; }

        /// <summary>Flat parameters written by schema v3 before the chain became module-driven.</summary>
        public double? Depth { get; set; }
        public double? CenterX { get; set; }
        public double? CenterY { get; set; }
        public bool? IgnoreHeight { get; set; }
        public double? Tolerance { get; set; }
        public double? Threshold { get; set; }
        public byte? LightMass { get; set; }
        public byte? BlankMass { get; set; }
        public uint? ScanRate { get; set; }
        public byte? Fps { get; set; }
        public bool? AddPointsToRegularLines { get; set; }
        public bool? AddPointsToBlank { get; set; }

        public static ModuleConfigDto From(DeviceModuleConfig m) => new()
        {
            Id = m.Id,
            TypeId = m.TypeId,
            DisplayName = m.DisplayName,
            IsEnabled = m.IsEnabled,
            ShowOnTable = m.ShowOnTable,
            ProjectGeometry = m.ProjectGeometry,
            Params = new Dictionary<string, string>(m.Params),
            Mesh = m.Mesh is null ? null : MeshSnapshotDto.From(MeshSnapshot.From(m.Mesh))
        };

        public DeviceModuleConfig ToModel()
        {
            var model = new DeviceModuleConfig
            {
                Id = string.IsNullOrEmpty(Id) ? DeviceModuleConfig.NewId() : Id,
                TypeId = TypeId,
                DisplayName = string.IsNullOrEmpty(DisplayName) ? TypeId : DisplayName,
                IsEnabled = IsEnabled,
                ShowOnTable = ShowOnTable,
                ProjectGeometry = ProjectGeometry
            };

            foreach (var (key, value) in Params)
                model.Params[key] = value;

            model.EnsureMesh();
            if (Mesh is not null && model.Mesh is not null)
                Mesh.ToModel().ApplyTo(model.Mesh);

            Carry("Depth", Depth);
            Carry("CenterX", CenterX);
            Carry("CenterY", CenterY);
            Carry("IgnoreHeight", IgnoreHeight);
            Carry("Threshold", Threshold);
            Carry("LightMass", LightMass);
            Carry("BlankMass", BlankMass);
            Carry("ScanRate", ScanRate);
            Carry("Fps", Fps);
            Carry("AddPointsToRegularLines", AddPointsToRegularLines);
            Carry("AddPointsToBlank", AddPointsToBlank);
            // v3 stored ShortestPath's connection distance in the shared Tolerance field.
            Carry(TypeId == ModuleTypes.ShortestPath ? "ConnectionTolerance" : "Tolerance", Tolerance);

            return model;

            void Carry(string key, object? value)
            {
                if (value is not null && !model.Params.ContainsKey(key))
                    model.Params[key] = ModuleValue.Format(value);
            }
        }
    }

    private sealed class ModuleSettingsDto
    {
        public bool ZEnabled { get; set; } = true;
        public double ZDepth { get; set; } = 1;
        public double ZCenterX { get; set; } = 0.5;
        public double ZCenterY { get; set; } = 0.5;
        public bool DfcEnabled { get; set; }
        public bool DfcIgnoreHeight { get; set; }
        public double DfcDepth { get; set; } = 1;

        public static ModuleSettingsDto From(DeviceModuleSettings m) => new()
        {
            ZEnabled = m.ZEnabled,
            ZDepth = m.ZDepth,
            ZCenterX = m.ZCenterX,
            ZCenterY = m.ZCenterY,
            DfcEnabled = m.DfcEnabled,
            DfcIgnoreHeight = m.DfcIgnoreHeight,
            DfcDepth = m.DfcDepth
        };

        public DeviceModuleSettings ToModel() => new()
        {
            ZEnabled = ZEnabled,
            ZDepth = ZDepth,
            ZCenterX = ZCenterX,
            ZCenterY = ZCenterY,
            DfcEnabled = DfcEnabled,
            DfcIgnoreHeight = DfcIgnoreHeight,
            DfcDepth = DfcDepth
        };
    }

    private sealed class MeshSnapshotDto
    {
        public bool IsEnabled { get; set; }
        public int Columns { get; set; } = 1;
        public int Rows { get; set; } = 1;
        public int Morph { get; set; } = (int)MeshMorphType.Full;
        public List<double[]> Points { get; set; } = [];

        public static MeshSnapshotDto From(MeshSnapshot m) => new()
        {
            IsEnabled = m.IsEnabled,
            Columns = m.Columns,
            Rows = m.Rows,
            Morph = m.Morph,
            Points = m.Points
        };

        public MeshSnapshot ToModel() => new()
        {
            IsEnabled = IsEnabled,
            Columns = Columns,
            Rows = Rows,
            Morph = Morph,
            Points = Points
        };
    }

    private sealed class SceneDto
    {
        public string Name { get; set; } = "Scene";
        public double WidthMm { get; set; } = 1000;
        public double HeightMm { get; set; } = 1000;
        public bool MaskEnabled { get; set; }
        public double MaskX { get; set; }
        public double MaskY { get; set; }
        public double MaskW { get; set; } = 1000;
        public double MaskH { get; set; } = 1000;
        public List<DrawableDto> Drawables { get; set; } = [];
        public List<string> BoundProjectorIds { get; set; } = [];
        public string? MeshPath { get; set; }
        public double MeshTx { get; set; }
        public double MeshTy { get; set; }
        public double MeshTz { get; set; }
        public double MeshRx { get; set; }
        public double MeshRy { get; set; }
        public double MeshRz { get; set; }
        public double MeshSx { get; set; } = 1;
        public double MeshSy { get; set; } = 1;
        public double MeshSz { get; set; } = 1;
        public bool MeshBreakOnMiss { get; set; } = true;

        public static SceneDto From(ProjectionScene s)
        {
            var dto = new SceneDto
            {
                Name = s.Name,
                WidthMm = s.Target.WidthMm,
                HeightMm = s.Target.HeightMm,
                MaskEnabled = s.Mask.IsEnabled,
                MaskX = s.Mask.Bounds.X,
                MaskY = s.Mask.Bounds.Y,
                MaskW = s.Mask.Bounds.Width,
                MaskH = s.Mask.Bounds.Height,
                Drawables = s.Drawables.Select(DrawableDto.From).ToList(),
                BoundProjectorIds = [.. s.BoundProjectorIds]
            };
            if (s.MeshTarget is { } mesh)
            {
                dto.MeshPath = mesh.SourcePath;
                dto.MeshTx = mesh.Translation.X;
                dto.MeshTy = mesh.Translation.Y;
                dto.MeshTz = mesh.Translation.Z;
                dto.MeshRx = mesh.RotationDeg.X;
                dto.MeshRy = mesh.RotationDeg.Y;
                dto.MeshRz = mesh.RotationDeg.Z;
                dto.MeshSx = mesh.Scale.X;
                dto.MeshSy = mesh.Scale.Y;
                dto.MeshSz = mesh.Scale.Z;
                dto.MeshBreakOnMiss = mesh.BreakOnMiss;
            }
            return dto;
        }

        public ProjectionScene ToModel()
        {
            var scene = new ProjectionScene { Name = Name };
            scene.Target.WidthMm = WidthMm;
            scene.Target.HeightMm = HeightMm;
            scene.Mask.IsEnabled = MaskEnabled;
            scene.Mask.Bounds = new Rect2(MaskX, MaskY, MaskW, MaskH);
            scene.Drawables.AddRange(Drawables.Select(d => d.ToModel()));
            scene.BoundProjectorIds = BoundProjectorIds?.Count > 0 ? [.. BoundProjectorIds] : [];
            return scene;
        }
    }

    private sealed class DrawableDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Object";
        public string? LayerName { get; set; }
        public uint? ColorArgb { get; set; }
        public bool IsVisible { get; set; } = true;
        public double Tx { get; set; }
        public double Ty { get; set; }
        public double Tz { get; set; }
        public double RotationDeg { get; set; }
        public double Scale { get; set; } = 1;
        public List<List<double[]>> Contours { get; set; } = [];
        public List<DrawableDto> Children { get; set; } = [];

        public static DrawableDto From(Drawable d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            LayerName = d.LayerName,
            ColorArgb = d.ColorArgb,
            IsVisible = d.IsVisible,
            Tx = d.Translation.X,
            Ty = d.Translation.Y,
            Tz = d.Translation.Z,
            RotationDeg = d.RotationDeg,
            Scale = d.Scale,
            Contours = d.Contours.Select(c => c.Select(p => new[] { p.X, p.Y }).ToList()).ToList(),
            Children = d.Children.Select(From).ToList()
        };

        public Drawable ToModel() => new()
        {
            Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
            Name = Name,
            LayerName = LayerName,
            ColorArgb = ColorArgb,
            IsVisible = IsVisible,
            Translation = new Point3(Tx, Ty, Tz),
            RotationDeg = RotationDeg,
            Scale = Scale,
            Contours = Contours.Select(c => c.Select(xy => new Point2(xy[0], xy[1])).ToList()).ToList(),
            Children = Children?.Select(c => c.ToModel()).ToList() ?? []
        };
    }
}
