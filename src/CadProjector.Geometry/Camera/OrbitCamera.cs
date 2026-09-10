using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Camera;

/// <summary>Z-up orbit camera. Yaw around Z, pitch from the XY plane toward +Z.</summary>
public sealed class OrbitCamera : ISceneCamera
{
    public Point3 Target { get; set; } = new(500, 500, 0);
    public double YawDeg { get; set; } = 40;
    public double PitchDeg { get; set; } = 32;
    public double Distance { get; set; } = 1800;
    public double FovDeg { get; set; } = 45;

    public Point3 Eye
    {
        get
        {
            GetBasis(out var eye, out _, out _, out _);
            return eye;
        }
    }

    public void Orbit(double deltaYawDeg, double deltaPitchDeg)
    {
        YawDeg += deltaYawDeg;
        PitchDeg = Math.Clamp(PitchDeg + deltaPitchDeg, -89.0, 89.0);
    }

    public void Zoom(double factor)
    {
        if (factor <= 0 || !double.IsFinite(factor))
            return;
        Distance = Math.Clamp(Distance * factor, 1, 1e7);
    }

    public void Pan(double dxPx, double dyPx, double viewportWidth, double viewportHeight)
    {
        if (viewportHeight < 1)
            return;
        GetBasis(out _, out _, out var right, out var up);
        var half = Math.Tan(FovDeg * Math.PI / 360.0) * Distance;
        var scale = half / (viewportHeight * 0.5);
        Target += right * (-dxPx * scale) + up * (dyPx * scale);
    }

    public void Fit(Aabb3 box, double viewportAspect = 1)
    {
        if (!double.IsFinite(box.Diagonal) || box.Diagonal < 1e-9)
            return;
        Target = box.Center;
        var fov = Math.Max(5, FovDeg) * Math.PI / 180.0;
        var radius = box.Diagonal * 0.5;
        var dist = radius / Math.Max(1e-6, Math.Tan(fov * 0.5));
        if (viewportAspect > 1)
            dist *= viewportAspect;
        Distance = Math.Max(10, dist * 1.25);
        if (Math.Abs(PitchDeg) < 1)
            PitchDeg = 32;
    }

    public bool TryProject(Point3 world, double width, double height, out Point2 screen, out double depth)
    {
        screen = default;
        depth = 0;
        if (width < 2 || height < 2)
            return false;

        GetBasis(out var eye, out var forward, out var right, out var up);
        var rel = world - eye;
        depth = Point3.Dot(rel, forward);
        if (depth < 1e-3)
            return false;

        var x = Point3.Dot(rel, right);
        var y = Point3.Dot(rel, up);
        var f = height * 0.5 / Math.Tan(FovDeg * Math.PI / 360.0);
        screen = new Point2(width * 0.5 + x * f / depth, height * 0.5 - y * f / depth);
        return true;
    }

    public bool TryRay(double screenX, double screenY, double width, double height, out Point3 origin, out Point3 dir)
    {
        origin = default;
        dir = default;
        if (width < 2 || height < 2)
            return false;
        GetBasis(out origin, out var forward, out var right, out var up);
        var f = height * 0.5 / Math.Tan(FovDeg * Math.PI / 360.0);
        dir = (forward * f + right * (screenX - width * 0.5) + up * (height * 0.5 - screenY)).Normalized();
        return dir.LengthSquared > 1e-12;
    }

    public void GetBasis(out Point3 eye, out Point3 forward, out Point3 right, out Point3 up)
    {
        var pitch = PitchDeg * Math.PI / 180.0;
        var yaw = YawDeg * Math.PI / 180.0;
        var cp = Math.Cos(pitch);
        var sp = Math.Sin(pitch);
        var cy = Math.Cos(yaw);
        var sy = Math.Sin(yaw);
        var offset = new Point3(sy * cp, cy * cp, sp) * Distance;
        eye = Target + offset;
        forward = (Target - eye).Normalized();
        if (forward.LengthSquared < 1e-12)
            forward = new Point3(0, 0, -1);
        right = Point3.Cross(forward, Point3.UnitZ);
        if (right.LengthSquared < 1e-12)
            right = Point3.Cross(forward, Point3.UnitX);
        right = right.Normalized();
        up = Point3.Cross(right, forward).Normalized();
    }
}
