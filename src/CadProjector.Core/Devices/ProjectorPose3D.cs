using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Devices;

/// <summary>
/// Where the projector physically stands and which way its beam axis points.
/// Drives the "view from this projector" camera, mesh coverage and — once the
/// pipeline is pose-aware — the world-to-device mapping.
/// </summary>
public sealed class ProjectorPose3D
{
    public Point3 PositionMm { get; set; } = Point3.Zero;

    /// <summary>Elevation of the beam axis above the XY plane. 90° points straight down.</summary>
    public double PitchDeg { get; set; } = 90;

    /// <summary>Heading of the beam axis around Z.</summary>
    public double YawDeg { get; set; }

    /// <summary>Rotation of the frame about the beam axis.</summary>
    public double RollDeg { get; set; }

    public double FovHDeg { get; set; } = 40;
    public double FovVDeg { get; set; } = 40;

    public ProjectorCamera ToCamera() => new()
    {
        Position = PositionMm,
        PitchDeg = PitchDeg,
        YawDeg = YawDeg,
        RollDeg = RollDeg,
        FovHDeg = FovHDeg,
        FovVDeg = FovVDeg
    };

    public void AimAt(Point3 target)
    {
        var cam = ToCamera();
        cam.AimAt(target);
        PitchDeg = cam.PitchDeg;
        YawDeg = cam.YawDeg;
    }

    /// <summary>Starting pose when the real rig is unknown: overhead, looking down.</summary>
    public void ParkAbove(Point3 target, double spanMm, double roofZMm = 0)
    {
        var lift = Math.Max(200, spanMm * 0.5);
        PositionMm = new Point3(target.X, target.Y, Math.Max(target.Z, roofZMm) + lift);
        PitchDeg = 90;
        YawDeg = 0;
        RollDeg = 0;
    }
}
