using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

public enum CalibrationPatternKind
{
    Dot,
    Rect,
    Grid
}

/// <summary>Builds laser calibration patterns in scene mm for Play / mesh tuning.</summary>
public static class CalibrationPatternBuilder
{
    public static ProjectionScene Create(
        PlaneTarget target,
        CalibrationPatternKind kind,
        int columns = 3,
        int rows = 3,
        double marginMm = 20)
    {
        columns = Math.Clamp(columns, 1, 16);
        rows = Math.Clamp(rows, 1, 16);
        var scene = new ProjectionScene
        {
            Name = $"Calib-{kind}"
        };
        scene.Target.WidthMm = target.WidthMm;
        scene.Target.HeightMm = target.HeightMm;
        scene.Mask.IsEnabled = false;

        var w = target.WidthMm;
        var h = target.HeightMm;
        var x0 = marginMm;
        var y0 = marginMm;
        var x1 = Math.Max(x0 + 1, w - marginMm);
        var y1 = Math.Max(y0 + 1, h - marginMm);

        switch (kind)
        {
            case CalibrationPatternKind.Rect:
                scene.Drawables.Add(new Drawable
                {
                    Name = "CalibRect",
                    Contours =
                    [
                        [
                            new Point2(x0, y0), new Point2(x1, y0), new Point2(x1, y1),
                            new Point2(x0, y1), new Point2(x0, y0)
                        ]
                    ]
                });
                break;

            case CalibrationPatternKind.Grid:
            {
                var d = new Drawable { Name = "CalibGrid" };
                for (var i = 0; i <= columns; i++)
                {
                    var x = x0 + (x1 - x0) * i / columns;
                    d.Contours.Add([new Point2(x, y0), new Point2(x, y1)]);
                }
                for (var j = 0; j <= rows; j++)
                {
                    var y = y0 + (y1 - y0) * j / rows;
                    d.Contours.Add([new Point2(x0, y), new Point2(x1, y)]);
                }
                scene.Drawables.Add(d);
                break;
            }

            default: // Dot — crosses at grid nodes
            {
                var d = new Drawable { Name = "CalibDots" };
                var arm = Math.Min(w, h) * 0.015;
                for (var i = 0; i <= columns; i++)
                for (var j = 0; j <= rows; j++)
                {
                    var x = x0 + (x1 - x0) * i / columns;
                    var y = y0 + (y1 - y0) * j / rows;
                    d.Contours.Add([new Point2(x - arm, y), new Point2(x + arm, y)]);
                    d.Contours.Add([new Point2(x, y - arm), new Point2(x, y + arm)]);
                }
                scene.Drawables.Add(d);
                break;
            }
        }

        return scene;
    }
}
