using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering.Modules;

/// <summary>Depth-dependent frame clip toward center (legacy DeepFrameCutter, simplified).</summary>
public sealed class DeepFrameCutterModule : IRenderableModule
{
    public string Name => "Deep Frame Cutter";
    public bool IsEnabled { get; set; }

    [ModuleParam(Label = "X", Increment = 0.01, Format = "0.###")]
    public double X { get; set; }

    [ModuleParam(Label = "Y", Increment = 0.01, Format = "0.###")]
    public double Y { get; set; }

    [ModuleParam(Label = "Width", Increment = 0.01, Format = "0.###")]
    public double Width { get; set; } = 1;

    [ModuleParam(Label = "Height", Increment = 0.01, Format = "0.###")]
    public double Height { get; set; } = 1;

    [ModuleParam(Label = "Center X", Increment = 0.01, Format = "0.###")]
    public double CenterX { get; set; } = 0.5;

    [ModuleParam(Label = "Center Y", Increment = 0.01, Format = "0.###")]
    public double CenterY { get; set; } = 0.5;

    [ModuleParam(Label = "Depth", Min = 0.01, Increment = 0.05, Format = "0.###")]
    public double Depth { get; set; } = 1;

    [ModuleParam(Label = "Ignore height")]
    public bool IgnoreHeight { get; set; }

    [ModuleParam(Label = "Cut left")]
    public bool CutLeft { get; set; } = true;

    [ModuleParam(Label = "Cut right")]
    public bool CutRight { get; set; } = true;

    [ModuleParam(Label = "Cut top")]
    public bool CutTop { get; set; } = true;

    [ModuleParam(Label = "Cut bottom")]
    public bool CutBottom { get; set; } = true;

    public LinesCollection GetGeometry()
    {
        var lines = new LinesCollection();
        GeometryBuilder.AddRect(lines, X, Y, Width, Height);
        const double tick = 0.02;
        GeometryBuilder.AddSegment(lines,
            new Point2(CenterX - tick, CenterY), new Point2(CenterX + tick, CenterY));
        GeometryBuilder.AddSegment(lines,
            new Point2(CenterX, CenterY - tick), new Point2(CenterX, CenterY + tick));
        return lines;
    }

    public IReadOnlyList<ModuleAnchor> GetAnchors() =>
    [
        .. GeometryBuilder.RectAnchors(X, Y, Width, Height),
        new ModuleAnchor(4, new Point2(CenterX, CenterY), "center")
    ];

    public bool MoveAnchor(int index, Point2 position)
    {
        if (index == 4)
        {
            CenterX = position.X;
            CenterY = position.Y;
            return true;
        }
        (X, Y, Width, Height) = GeometryBuilder.DragRectCorner(index, position, X, Y, Width, Height);
        return true;
    }

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled)
            return input;

        var output = new LinesCollection();
        for (var i = 0; i < input.Points.Count - 1; i++)
        {
            var a = input.Points[i];
            var b = input.Points[i + 1];
            if (b.Blanked)
                continue;

            var z = (a.Z + b.Z) * 0.5;
            var poly = BuildClipPolygon(z);
            if (!TryClipSegment(a, b, poly, out var c1, out var c2))
                continue;

            output.Points.Add(new RenderPoint
            {
                X = c1.X, Y = c1.Y, Z = c1.Z, Blanked = true, Mass = a.Mass, Color = a.Color
            });
            output.Points.Add(new RenderPoint
            {
                X = c2.X, Y = c2.Y, Z = c2.Z, Blanked = false, Mass = b.Mass, Color = b.Color
            });
        }
        return output;
    }

    private Point2[] BuildClipPolygon(double z)
    {
        const double eps = 1e-9;
        double deepProp;
        if (IgnoreHeight)
            deepProp = 1.0;
        else
        {
            var depthAbs = Math.Max(eps, Math.Abs(Depth));
            if (z < 0)
                deepProp = depthAbs / (depthAbs + Math.Abs(z));
            else
                deepProp = depthAbs / Math.Max(eps, Math.Abs(depthAbs - z));
            if (double.IsNaN(deepProp) || double.IsInfinity(deepProp))
                deepProp = 0;
        }

        double Project(double center, double edge, double factor, bool cut) =>
            cut ? center + (edge - center) * factor : edge;

        var left = Project(CenterX, X, deepProp, CutLeft);
        var right = Project(CenterX, X + Width, deepProp, CutRight);
        var top = Project(CenterY, Y, deepProp, CutTop);
        var bottom = Project(CenterY, Y + Height, deepProp, CutBottom);
        return
        [
            new Point2(left, top),
            new Point2(right, top),
            new Point2(right, bottom),
            new Point2(left, bottom)
        ];
    }

    private static bool TryClipSegment(RenderPoint a, RenderPoint b, Point2[] poly, out Point3 c1, out Point3 c2)
    {
        c1 = default;
        c2 = default;
        // Liang-Barsky style against AABB of poly (axis-aligned for our DFC)
        var minX = poly.Min(p => p.X);
        var maxX = poly.Max(p => p.X);
        var minY = poly.Min(p => p.Y);
        var maxY = poly.Max(p => p.Y);

        double x0 = a.X, y0 = a.Y, z0 = a.Z;
        double x1 = b.X, y1 = b.Y, z1 = b.Z;
        double dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
        double t0 = 0, t1 = 1;

        bool Clip(double p, double q)
        {
            if (Math.Abs(p) < 1e-12)
                return q >= 0;
            var r = q / p;
            if (p < 0)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }

        if (!Clip(-dx, x0 - minX) || !Clip(dx, maxX - x0) ||
            !Clip(-dy, y0 - minY) || !Clip(dy, maxY - y0))
            return false;

        c1 = new Point3(x0 + t0 * dx, y0 + t0 * dy, z0 + t0 * dz);
        c2 = new Point3(x0 + t1 * dx, y0 + t1 * dy, z0 + t1 * dz);
        return true;
    }
}
