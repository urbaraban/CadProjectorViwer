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
        var surface = scene.MeshTarget;

        foreach (var drawable in scene.Drawables)
        {
            DrawableSpace.VisitContours(drawable, (leaf, contour, worldZ) =>
            {
                if (contour.Count < 2)
                    return;

                var color = project.ColorMode == LaserColorMode.LayerColor && leaf.ColorArgb is uint argb
                    ? RgbColor.FromArgb(argb)
                    : solid;

                var transformed = contour.ToList();
                if (mask.IsEnabled)
                    transformed = ClipPolylineToRect(transformed, mask.Bounds);

                var miss = false;
                for (var i = 0; i < transformed.Count; i++)
                {
                    var p = transformed[i];
                    var z = worldZ;
                    var nx = p.X / w;
                    var ny = p.Y / h;

                    if (surface is not null)
                    {
                        if (!surface.TryProject(p.X, p.Y, worldZ, out var hit))
                        {
                            if (surface.BreakOnMiss)
                                miss = true;
                            continue;
                        }

                        nx = hit.X / w;
                        ny = hit.Y / h;
                        z = hit.Z;
                    }

                    var unit = new Point2(nx, ny);
                    if (mesh is not null)
                        unit = mesh.Apply(unit);

                    lines.Points.Add(new RenderPoint
                    {
                        X = unit.X,
                        Y = unit.Y,
                        Z = z,
                        Blanked = i == 0 || miss,
                        Color = color
                    });
                    miss = false;
                }
            });
        }

        return lines;
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
