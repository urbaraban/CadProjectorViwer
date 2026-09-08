using CadProjector.Core.Devices;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering;

public static class SceneFrameBuilder
{
    public static LinesCollection Build(
        ProjectionScene scene,
        ProjectDocument project,
        CalibrationMesh? mesh = null)
    {
        var lines = new LinesCollection();
        var mask = scene.Mask;
        var solid = RgbColor.FromArgb(project.SolidColorArgb);
        var w = Math.Max(1e-6, scene.Target.WidthMm);
        var h = Math.Max(1e-6, scene.Target.HeightMm);

        foreach (var drawable in scene.Drawables)
        {
            if (!drawable.IsVisible)
                continue;

            var color = project.ColorMode == LaserColorMode.LayerColor && drawable.ColorArgb is uint argb
                ? RgbColor.FromArgb(argb)
                : solid;

            foreach (var contour in drawable.Contours)
            {
                if (contour.Count < 2)
                    continue;

                var transformed = contour.Select(p => Transform(p, drawable)).ToList();
                if (mask.IsEnabled)
                    transformed = ClipPolylineToRect(transformed, mask.Bounds);

                for (var i = 0; i < transformed.Count; i++)
                {
                    var p = transformed[i];
                    var nx = p.X / w;
                    var ny = p.Y / h;
                    var unit = new Point2(nx, ny);
                    if (mesh is not null)
                        unit = mesh.Apply(unit);

                    lines.Points.Add(new RenderPoint
                    {
                        X = unit.X,
                        Y = unit.Y,
                        Z = drawable.Translation.Z,
                        Blanked = i == 0,
                        Color = color
                    });
                }
            }
        }

        return lines;
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

    private static List<Point2> ClipPolylineToRect(List<Point2> points, Rect2 rect)
    {
        var result = new List<Point2>();
        foreach (var p in points)
        {
            if (rect.Contains(p))
                result.Add(p);
        }
        return result;
    }
}
