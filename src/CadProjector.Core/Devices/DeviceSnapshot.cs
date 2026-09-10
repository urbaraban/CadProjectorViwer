using CadProjector.Core.Project;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Devices;

/// <summary>Serializable projector card for .cproj schema v3+ / .cdev.</summary>
public sealed class DeviceSnapshot
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

    /// <summary>Beam axis. Files written before pose was real carry 0 — read as "straight down".</summary>
    public double? PosePitchDeg { get; set; }
    public double? PoseYawDeg { get; set; }
    public double? PoseRollDeg { get; set; }
    public double? FovHDeg { get; set; }
    public double? FovVDeg { get; set; }
    public byte Red { get; set; } = 255;
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; } = 255;
    public int WidthResolution { get; set; } = 65533;
    public int HeightResolution { get; set; } = 65533;

    /// <summary>Legacy device-level mesh from files before the mesh was a module. Migrated on ApplyTo.</summary>
    public MeshSnapshot? Mesh { get; set; }

    /// <summary>Dynamic module chain (preferred); the mesh lives in its own module here.</summary>
    public List<DeviceModuleConfig> ModuleChain { get; set; } = [];

    /// <summary>Legacy flat settings from early v3 — migrated into ModuleChain on load.</summary>
    public DeviceModuleSettings? Modules { get; set; }

    public static DeviceSnapshot From(ProjectorProfile p) => new()
    {
        Id = p.Id,
        DisplayName = p.DisplayName,
        Host = p.Host,
        Port = p.Port,
        UseVlt = p.UseVlt,
        FovWidthMm = p.FovWidthMm,
        FovHeightMm = p.FovHeightMm,
        PoseX = p.Pose.PositionMm.X,
        PoseY = p.Pose.PositionMm.Y,
        PoseZ = p.Pose.PositionMm.Z,
        PosePitchDeg = p.Pose.PitchDeg,
        PoseYawDeg = p.Pose.YawDeg,
        PoseRollDeg = p.Pose.RollDeg,
        FovHDeg = p.Pose.FovHDeg,
        FovVDeg = p.Pose.FovVDeg,
        Red = p.Red,
        Green = p.Green,
        Blue = p.Blue,
        Alpha = p.Alpha,
        WidthResolution = p.WidthResolution,
        HeightResolution = p.HeightResolution,
        ModuleChain = p.ModuleChain.Select(m => m.Clone()).ToList()
    };

    public void ApplyTo(ProjectorProfile p)
    {
        if (!string.IsNullOrWhiteSpace(Id)) p.Id = Id;
        p.DisplayName = DisplayName;
        p.Host = Host;
        p.Port = Port;
        p.UseVlt = UseVlt;
        p.FovWidthMm = FovWidthMm;
        p.FovHeightMm = FovHeightMm;
        p.Pose.PositionMm = new Point3(PoseX, PoseY, PoseZ);
        p.Pose.PitchDeg = PosePitchDeg ?? 90;
        p.Pose.YawDeg = PoseYawDeg ?? 0;
        p.Pose.RollDeg = PoseRollDeg ?? 0;
        p.Pose.FovHDeg = FovHDeg ?? 40;
        p.Pose.FovVDeg = FovVDeg ?? 40;
        p.Red = Red;
        p.Green = Green;
        p.Blue = Blue;
        p.Alpha = Alpha;
        p.WidthResolution = WidthResolution;
        p.HeightResolution = HeightResolution;

        if (ModuleChain.Count > 0)
            p.ModuleChain = ModuleChain.Select(m => m.Clone()).ToList();
        else if (Modules is not null)
            p.ModuleChain = DeviceModuleConfig.FromLegacy(Modules);
        else if (p.ModuleChain.Count == 0)
            p.ModuleChain = DeviceModuleConfig.DefaultChain();

        // Files written before the mesh became a module carry it on the device card.
        if (Mesh is not null)
        {
            var meshModule = ModuleTypes.FindMesh(p.ModuleChain);
            if (meshModule is null)
            {
                meshModule = DeviceModuleConfig.Of(ModuleTypes.Mesh);
                p.ModuleChain.Add(meshModule);
            }
            meshModule.EnsureMesh();
            Mesh.ApplyTo(meshModule.Mesh!);
            meshModule.Set("Columns", meshModule.Mesh!.Columns);
            meshModule.Set("Rows", meshModule.Mesh.Rows);
            meshModule.IsEnabled = meshModule.Mesh.IsEnabled;
        }
    }
}
