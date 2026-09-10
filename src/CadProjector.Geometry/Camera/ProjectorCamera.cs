using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Camera;

/// <summary>
/// A galvo laser projector is a pinhole camera in reverse: a point source with an
/// angular field. That makes one type serve two jobs — "what does this projector
/// see" for the viewport, and "where does this world point land" in device 0..1.
/// </summary>
public sealed class ProjectorCamera : ISceneCamera
{
    public Point3 Position { get; set; }

    /// <summary>Elevation of the beam axis above the XY plane. 90° points straight down.</summary>
    public double PitchDeg { get; set; } = 90;

    /// <summary>Heading of the beam axis around Z, same sense as the orbit camera.</summary>
    public double YawDeg { get; set; }

    /// <summary>Rotation of the frame about the beam axis.</summary>
    public double RollDeg { get; set; }

    public double FovHDeg { get; set; } = 40;
    public double FovVDeg { get; set; } = 40;

    public Point3 Eye => Position;

    public static ProjectorCamera Aimed(Point3 position, Point3 target, double fovHDeg = 40, double fovVDeg = 40)
    {
        var cam = new ProjectorCamera { Position = position, FovHDeg = fovHDeg, FovVDeg = fovVDeg };
        cam.AimAt(target);
        return cam;
    }

    /// <summary>Point the beam axis at a scene point, keeping position and field.</summary>
    public void AimAt(Point3 target)
    {
        var forward = (target - Position).Normalized();
        if (forward.LengthSquared < 1e-12)
            return;
        var d = -forward;
        PitchDeg = Math.Asin(Math.Clamp(d.Z, -1, 1)) * 180.0 / Math.PI;
        YawDeg = Math.Atan2(d.X, d.Y) * 180.0 / Math.PI;
    }

    public void GetBasis(out Point3 forward, out Point3 right, out Point3 up)
    {
        var pitch = PitchDeg * Math.PI / 180.0;
        var yaw = YawDeg * Math.PI / 180.0;
        var cp = Math.Cos(pitch);
        var sp = Math.Sin(pitch);
        var cy = Math.Cos(yaw);
        var sy = Math.Sin(yaw);

        forward = new Point3(-sy * cp, -cy * cp, -sp).Normalized();
        if (forward.LengthSquared < 1e-12)
            forward = new Point3(0, 0, -1);

        right = Point3.Cross(forward, Point3.UnitZ);
        // Straight down is the common rig, and there the cross product collapses.
        // Falling back to +X keeps the table upright: right = +X, up = +Y.
        right = right.LengthSquared < 1e-12 ? Point3.UnitX : right.Normalized();
        up = Point3.Cross(right, forward).Normalized();

        if (RollDeg is not 0)
        {
            var r = RollDeg * Math.PI / 180.0;
            var c = Math.Cos(r);
            var s = Math.Sin(r);
            var nr = right * c + up * s;
            var nu = up * c - right * s;
            right = nr.Normalized();
            up = nu.Normalized();
        }
    }

    /// <summary>
    /// World point to device space, 0..1 across the field with Y up — the same
    /// convention <see cref="Primitives.Rect2"/>-based FOV split already feeds the ILDA stage.
    /// </summary>
    public bool TryProjectDevice(Point3 world, out Point2 uv, out double depth)
    {
        uv = default;
        GetBasis(out var forward, out var right, out var up);
        var rel = world - Position;
        depth = Point3.Dot(rel, forward);
        if (depth < 1e-6)
            return false;

        var th = Math.Tan(FovHDeg * Math.PI / 360.0);
        var tv = Math.Tan(FovVDeg * Math.PI / 360.0);
        if (th < 1e-9 || tv < 1e-9)
            return false;

        var x = Point3.Dot(rel, right) / depth;
        var y = Point3.Dot(rel, up) / depth;
        if (!double.IsFinite(x) || !double.IsFinite(y))
            return false;

        uv = new Point2(0.5 + x / (2 * th), 0.5 + y / (2 * tv));
        return double.IsFinite(uv.X) && double.IsFinite(uv.Y);
    }

    public bool TryProject(Point3 world, double width, double height, out Point2 screen, out double depth)
    {
        screen = default;
        if (width < 2 || height < 2)
        {
            depth = 0;
            return false;
        }

        if (!TryProjectDevice(world, out var uv, out depth))
            return false;

        screen = new Point2(uv.X * width, (1 - uv.Y) * height);
        return true;
    }

    public bool TryRay(double screenX, double screenY, double width, double height, out Point3 origin, out Point3 dir)
    {
        origin = Position;
        dir = default;
        if (width < 2 || height < 2)
            return false;

        GetBasis(out var forward, out var right, out var up);
        var th = Math.Tan(FovHDeg * Math.PI / 360.0);
        var tv = Math.Tan(FovVDeg * Math.PI / 360.0);
        var dx = (screenX / width - 0.5) * 2 * th;
        var dy = (1 - screenY / height - 0.5) * 2 * tv;
        dir = (forward + right * dx + up * dy).Normalized();
        return dir.LengthSquared > 1e-12;
    }
}
