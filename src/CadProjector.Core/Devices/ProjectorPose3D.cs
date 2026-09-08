using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Devices;

/// <summary>Spatial pose of a projector for FOV-based geometry split.</summary>
public sealed class ProjectorPose3D
{
    public Point3 PositionMm { get; set; } = Point3.Zero;
    /// <summary>Euler degrees (pitch, yaw, roll) — placeholder for MVP.</summary>
    public Point3 OrientationDeg { get; set; } = Point3.Zero;
}
