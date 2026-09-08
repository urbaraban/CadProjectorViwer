using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

public enum AlignH { Left, Center, Right }
public enum AlignV { Top, Middle, Bottom }

public static class ObjectAligner
{
    public static void Align(Drawable drawable, PlaneTarget plane, AlignH h, AlignV v)
    {
        if (!TryGetBounds(drawable, out var minX, out var minY, out var maxX, out var maxY))
            return;

        var objAnchorX = h switch
        {
            AlignH.Left => minX,
            AlignH.Right => maxX,
            _ => (minX + maxX) * 0.5
        };
        var objAnchorY = v switch
        {
            AlignV.Top => maxY,
            AlignV.Bottom => minY,
            _ => (minY + maxY) * 0.5
        };

        var planeAnchorX = h switch
        {
            AlignH.Left => 0,
            AlignH.Right => plane.WidthMm,
            _ => plane.WidthMm * 0.5
        };
        var planeAnchorY = v switch
        {
            AlignV.Top => plane.HeightMm,
            AlignV.Bottom => 0,
            _ => plane.HeightMm * 0.5
        };

        var dx = planeAnchorX - objAnchorX;
        var dy = planeAnchorY - objAnchorY;
        drawable.Translation = new Point3(
            drawable.Translation.X + dx,
            drawable.Translation.Y + dy,
            drawable.Translation.Z);
    }

    /// <summary>
    /// Translation to move <paramref name="bounds"/> so its edge/center matches the plane's.
    /// Used when several objects should keep their relative layout while docking as a group.
    /// </summary>
    public static Point2 DeltaToAlign(Rect2 bounds, PlaneTarget plane, AlignH h, AlignV v)
    {
        var objAnchorX = h switch
        {
            AlignH.Left => bounds.X,
            AlignH.Right => bounds.X + bounds.Width,
            _ => bounds.X + bounds.Width * 0.5
        };
        var objAnchorY = v switch
        {
            AlignV.Top => bounds.Y + bounds.Height,
            AlignV.Bottom => bounds.Y,
            _ => bounds.Y + bounds.Height * 0.5
        };
        var planeAnchorX = h switch
        {
            AlignH.Left => 0,
            AlignH.Right => plane.WidthMm,
            _ => plane.WidthMm * 0.5
        };
        var planeAnchorY = v switch
        {
            AlignV.Top => plane.HeightMm,
            AlignV.Bottom => 0,
            _ => plane.HeightMm * 0.5
        };
        return new Point2(planeAnchorX - objAnchorX, planeAnchorY - objAnchorY);
    }

    public static bool TryGetGroupBounds(IEnumerable<Drawable> drawables, out Rect2 bounds)
    {
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var any = false;
        foreach (var d in drawables)
        {
            if (!TryGetBounds(d, out var x0, out var y0, out var x1, out var y1))
                continue;
            any = true;
            minX = Math.Min(minX, x0);
            minY = Math.Min(minY, y0);
            maxX = Math.Max(maxX, x1);
            maxY = Math.Max(maxY, y1);
        }

        if (!any)
        {
            bounds = default;
            return false;
        }

        bounds = new Rect2(minX, minY, maxX - minX, maxY - minY);
        return true;
    }

    public static bool TryGetBounds(
        Drawable drawable,
        out double minX, out double minY, out double maxX, out double maxY)
    {
        minX = double.PositiveInfinity;
        minY = double.PositiveInfinity;
        maxX = double.NegativeInfinity;
        maxY = double.NegativeInfinity;
        var any = false;
        foreach (var c in drawable.Contours)
        foreach (var p in c)
        {
            var t = Transform(p, drawable);
            any = true;
            minX = Math.Min(minX, t.X);
            minY = Math.Min(minY, t.Y);
            maxX = Math.Max(maxX, t.X);
            maxY = Math.Max(maxY, t.Y);
        }
        return any;
    }

    private static Point2 Transform(Point2 local, Drawable d)
    {
        var s = d.Scale;
        var x = local.X * s;
        var y = local.Y * s;
        if (Math.Abs(d.RotationDeg) > 1e-9)
        {
            var rad = d.RotationDeg * Math.PI / 180.0;
            var c = Math.Cos(rad);
            var sn = Math.Sin(rad);
            var rx = x * c - y * sn;
            var ry = x * sn + y * c;
            x = rx;
            y = ry;
        }
        return new Point2(x + d.Translation.X, y + d.Translation.Y);
    }
}
