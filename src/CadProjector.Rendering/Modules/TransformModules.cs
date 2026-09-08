using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering.Modules;

/// <summary>Rotate the frame around a point (legacy RotateFrame2D).</summary>
public sealed class Rotate2DModule : PointModule
{
    public override string Name => "Rotate 2D";

    [ModuleParam(Label = "Center X", Increment = 0.01, Format = "0.###")]
    public double X { get; set; } = 0.5;

    [ModuleParam(Label = "Center Y", Increment = 0.01, Format = "0.###")]
    public double Y { get; set; } = 0.5;

    [ModuleParam(Label = "Angle°", Increment = 1, Format = "0.##")]
    public double Angle { get; set; } = 90;

    protected override RenderPoint Correct(RenderPoint point)
    {
        var rad = Angle * (Math.PI / 180);
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        var dx = point.X - X;
        var dy = point.Y - Y;
        point.X = cos * dx - sin * dy + X;
        point.Y = sin * dx + cos * dy + Y;
        return point;
    }
}

/// <summary>Scale around a center, then shift (legacy MoveScale2D).</summary>
public sealed class MoveScale2DModule : PointModule
{
    public override string Name => "Move / Scale 2D";

    [ModuleParam(Label = "Shift X", Increment = 0.01, Format = "0.###")]
    public double ShiftX { get; set; }

    [ModuleParam(Label = "Shift Y", Increment = 0.01, Format = "0.###")]
    public double ShiftY { get; set; }

    [ModuleParam(Label = "Scale X", Increment = 0.01, Format = "0.###")]
    public double ScaleX { get; set; } = 1;

    [ModuleParam(Label = "Scale Y", Increment = 0.01, Format = "0.###")]
    public double ScaleY { get; set; } = 1;

    [ModuleParam(Label = "Center X", Increment = 0.01, Format = "0.###")]
    public double CenterX { get; set; } = 0.5;

    [ModuleParam(Label = "Center Y", Increment = 0.01, Format = "0.###")]
    public double CenterY { get; set; } = 0.5;

    protected override RenderPoint Correct(RenderPoint point)
    {
        point.X = (point.X - CenterX) * ScaleX + CenterX + ShiftX;
        point.Y = (point.Y - CenterY) * ScaleY + CenterY + ShiftY;
        return point;
    }
}

/// <summary>Shift the 0..1 frame so its center becomes the origin (legacy AroundZero).</summary>
public sealed class AroundZeroModule : PointModule
{
    public override string Name => "Around Zero";

    protected override RenderPoint Correct(RenderPoint point)
    {
        point.X -= 0.5;
        point.Y -= 0.5;
        return point;
    }
}

/// <summary>Scale normalized coordinates to half the device resolution (legacy ResolutionMultiplier).</summary>
public sealed class ResolutionMultiplierModule : PointModule
{
    public override string Name => "Resolution";

    [ModuleParam(Label = "Width", Increment = 1, Format = "0.##")]
    public double Width { get; set; } = 1;

    [ModuleParam(Label = "Height", Increment = 1, Format = "0.##")]
    public double Height { get; set; } = 1;

    protected override RenderPoint Correct(RenderPoint point)
    {
        point.X *= Width * 0.5;
        point.Y *= Height * 0.5;
        return point;
    }
}

/// <summary>Clip to a rectangle and renormalize it to 0..1 (legacy RectProportion).</summary>
public sealed class RectProportionModule : IRenderableModule
{
    public string Name => "Rect Proportion";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "X", Increment = 0.01, Format = "0.###")]
    public double X { get; set; }

    [ModuleParam(Label = "Y", Increment = 0.01, Format = "0.###")]
    public double Y { get; set; }

    [ModuleParam(Label = "Width", Increment = 0.01, Format = "0.###")]
    public double Width { get; set; } = 1;

    [ModuleParam(Label = "Height", Increment = 0.01, Format = "0.###")]
    public double Height { get; set; } = 1;

    public LinesCollection GetGeometry()
    {
        var lines = new LinesCollection();
        GeometryBuilder.AddRect(lines, X, Y, Width, Height);
        return lines;
    }

    public IReadOnlyList<ModuleAnchor> GetAnchors() => GeometryBuilder.RectAnchors(X, Y, Width, Height);

    public bool MoveAnchor(int index, Point2 position)
    {
        (X, Y, Width, Height) = GeometryBuilder.DragRectCorner(index, position, X, Y, Width, Height);
        return true;
    }

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || input.Points.Count < 2)
            return input;

        var w = Math.Abs(Width) < 1e-12 ? 1 : Width;
        var h = Math.Abs(Height) < 1e-12 ? 1 : Height;
        var result = new List<StrokeSegment>();

        foreach (var seg in StrokeSegmentOps.ToSegments(input))
        {
            if (!TryClip(seg, out var clipped))
                continue;
            clipped.P1.X = (clipped.P1.X - X) / w;
            clipped.P1.Y = (clipped.P1.Y - Y) / h;
            clipped.P2.X = (clipped.P2.X - X) / w;
            clipped.P2.Y = (clipped.P2.Y - Y) / h;
            result.Add(clipped);
        }

        return StrokeSegmentOps.FromSegments(result);
    }

    private bool TryClip(StrokeSegment seg, out StrokeSegment clipped)
    {
        clipped = seg;
        var minX = Math.Min(X, X + Width);
        var maxX = Math.Max(X, X + Width);
        var minY = Math.Min(Y, Y + Height);
        var maxY = Math.Max(Y, Y + Height);

        double t0 = 0, t1 = 1;
        var dx = seg.P2.X - seg.P1.X;
        var dy = seg.P2.Y - seg.P1.Y;

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

        if (!Clip(-dx, seg.P1.X - minX) || !Clip(dx, maxX - seg.P1.X) ||
            !Clip(-dy, seg.P1.Y - minY) || !Clip(dy, maxY - seg.P1.Y))
            return false;

        clipped = new StrokeSegment
        {
            P1 = seg.PointAt(t0),
            P2 = seg.PointAt(t1),
            IsBlank = seg.IsBlank,
            T1 = seg.T1,
            T2 = seg.T2
        };
        clipped.P1.Color = seg.P1.Color;
        clipped.P2.Color = seg.P2.Color;
        return true;
    }
}
