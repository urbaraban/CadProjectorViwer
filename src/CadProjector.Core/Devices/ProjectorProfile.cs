using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Devices;

public sealed class ProjectorProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string DisplayName { get; set; } = "Projector";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 10000;
    public bool UseVlt { get; set; }
    public ProjectorPose3D Pose { get; } = new();

    /// <summary>Ordered render modifiers; the calibration mesh is one of them when present.</summary>
    public List<DeviceModuleConfig> ModuleChain { get; set; } = DeviceModuleConfig.DefaultChain();

    /// <summary>Field of view on scene plane (mm), centered at Pose.PositionMm X/Y.</summary>
    public double FovWidthMm { get; set; } = 1000;
    public double FovHeightMm { get; set; } = 1000;

    public byte Red { get; set; } = 255;
    public byte Green { get; set; } = 0;
    public byte Blue { get; set; } = 0;
    public byte Alpha { get; set; } = 255;
    public int WidthResolution { get; set; } = 65533;
    public int HeightResolution { get; set; } = 65533;

    public void FitFovToScene(double widthMm, double heightMm)
    {
        FovWidthMm = widthMm;
        FovHeightMm = heightMm;
        Pose.PositionMm = new Point3(widthMm * 0.5, heightMm * 0.5, Pose.PositionMm.Z);
    }

    public static ProjectorProfile CreateDefault(string name, string host = "127.0.0.1", int port = 10000)
    {
        var p = new ProjectorProfile
        {
            DisplayName = name,
            Host = host,
            Port = port,
            ModuleChain = DeviceModuleConfig.DefaultChain()
        };
        // Pose is the FOV centre, so an unplaced projector must start over its own field,
        // otherwise its frame straddles the origin and hangs off the table.
        p.FitFovToScene(p.FovWidthMm, p.FovHeightMm);
        ModuleTypes.FindMesh(p.ModuleChain)?.Mesh?.ResetIdentity(3, 3);
        return p;
    }
}
