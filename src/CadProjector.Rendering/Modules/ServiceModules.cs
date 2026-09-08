using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering.Modules;

/// <summary>Append a blank circle to keep the galvos moving (legacy BlankCircleInserter).</summary>
public sealed class BlankCircleModule : IRenderableModule
{
    public string Name => "Blank Circle";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Radius", Increment = 0.01, Format = "0.###")]
    public double Radius { get; set; } = 0.45;

    [ModuleParam(Label = "Points", Min = 3, Max = 1000)]
    public int PointsCount { get; set; } = 20;

    [ModuleParam(Label = "Center X", Increment = 0.01, Format = "0.###")]
    public double X { get; set; } = 0.5;

    [ModuleParam(Label = "Center Y", Increment = 0.01, Format = "0.###")]
    public double Y { get; set; } = 0.5;

    public LinesCollection GetGeometry()
    {
        var lines = new LinesCollection();
        GeometryBuilder.AddPolygon(lines, CirclePoints(0));
        return lines;
    }

    public IReadOnlyList<ModuleAnchor> GetAnchors() =>
    [
        new ModuleAnchor(0, new Point2(X, Y), "center"),
        new ModuleAnchor(1, new Point2(X + Radius, Y), "radius")
    ];

    public bool MoveAnchor(int index, Point2 position)
    {
        if (index == 0)
        {
            X = position.X;
            Y = position.Y;
            return true;
        }
        if (index != 1)
            return false;
        var dx = position.X - X;
        var dy = position.Y - Y;
        Radius = Math.Max(0.01, Math.Sqrt(dx * dx + dy * dy));
        return true;
    }

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || PointsCount < 3)
            return input;

        var segs = StrokeSegmentOps.ToSegments(input);
        var entry = input.Points.Count > 0 ? input.Points[0] : new RenderPoint { X = X, Y = Y };
        var startAngle = Math.Atan2(Y - entry.Y, X - entry.X);
        if (startAngle < 0)
            startAngle += 2 * Math.PI;

        var circle = CirclePoints(startAngle);
        for (var i = 0; i < circle.Count; i++)
        {
            segs.Add(new StrokeSegment
            {
                P1 = new RenderPoint { X = circle[i].X, Y = circle[i].Y },
                P2 = new RenderPoint { X = circle[(i + 1) % circle.Count].X, Y = circle[(i + 1) % circle.Count].Y },
                IsBlank = true
            });
        }

        return StrokeSegmentOps.FromSegments(segs);
    }

    private List<Point2> CirclePoints(double startAngle)
    {
        var count = Math.Max(3, PointsCount);
        var points = new List<Point2>(count);
        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + 2 * Math.PI * i / count;
            points.Add(new Point2(X + Radius * Math.Cos(angle), Y + Radius * Math.Sin(angle)));
        }
        return points;
    }
}

/// <summary>Take one page of segments (legacy LinesSkipper).</summary>
public sealed class LinesSkipperModule : IFrameModule
{
    public string Name => "Lines Skipper";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Max lines", Min = 1, Max = 65535)]
    public int MaxLinesCount { get; set; } = 1000;

    [ModuleParam(Label = "Page", Min = 0, Max = 65535)]
    public int LinesPart { get; set; }

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || MaxLinesCount < 1)
            return input;

        var segs = StrokeSegmentOps.ToSegments(input)
            .Skip(LinesPart * MaxLinesCount)
            .Take(MaxLinesCount)
            .ToList();
        return StrokeSegmentOps.FromSegments(segs);
    }
}

/// <summary>Take one page of connected segment groups (legacy LinesGroupSplitter).</summary>
public sealed class LinesGroupSplitterModule : IFrameModule
{
    public string Name => "Lines Group Splitter";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Groups per page", Min = 1, Max = 65535)]
    public int LinesGroupInPart { get; set; } = 1000;

    [ModuleParam(Label = "Page", Min = 0, Max = 65535)]
    public int Part { get; set; }

    [ModuleParam(Label = "Join tolerance", Increment = 1e-6, Format = "0.#######")]
    public double Tolerance { get; set; } = 1e-6;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || LinesGroupInPart < 1)
            return input;

        var selected = GroupByProximity(StrokeSegmentOps.ToSegments(input))
            .Skip(Part * LinesGroupInPart)
            .Take(LinesGroupInPart)
            .SelectMany(g => g)
            .ToList();
        return StrokeSegmentOps.FromSegments(selected);
    }

    private IEnumerable<List<StrokeSegment>> GroupByProximity(List<StrokeSegment> segs)
    {
        if (segs.Count == 0)
            yield break;

        var tolSq = Tolerance * Tolerance;
        var group = new List<StrokeSegment> { segs[0] };
        for (var i = 1; i < segs.Count; i++)
        {
            if (StrokeSegmentOps.DistSq(segs[i - 1].P2, segs[i].P1) <= tolSq)
            {
                group.Add(segs[i]);
                continue;
            }
            yield return group;
            group = [segs[i]];
        }
        yield return group;
    }
}

/// <summary>Clip to the 0..1 frame and split segments at grid boundaries (legacy LineGridSplitter).</summary>
public sealed class LineGridSplitterModule : IRenderableModule
{
    private const double Epsilon = 1e-6;

    public string Name => "Line Grid Splitter";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Split along Y")]
    public bool IsVertical { get; set; }

    [ModuleParam(Label = "Parts", Min = 0, Max = 65535)]
    public int PartCount { get; set; }

    public LinesCollection GetGeometry()
    {
        var lines = new LinesCollection();
        GeometryBuilder.AddRect(lines, 0, 0, 1, 1);
        if (PartCount < 2)
            return lines;

        var step = 1.0 / (PartCount - 1.0);
        for (var i = 1; i < PartCount - 1; i++)
        {
            var v = i * step;
            GeometryBuilder.AddSegment(lines,
                IsVertical ? new Point2(0, v) : new Point2(v, 0),
                IsVertical ? new Point2(1, v) : new Point2(v, 1));
        }
        return lines;
    }

    /// <summary>The grid is uniform and fully described by <see cref="PartCount"/>, so nothing is draggable.</summary>
    public IReadOnlyList<ModuleAnchor> GetAnchors() => [];

    public bool MoveAnchor(int index, Point2 position) => false;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || PartCount < 2 || input.Points.Count < 2)
            return input;

        var result = new List<StrokeSegment>();
        foreach (var seg in StrokeSegmentOps.ToSegments(input))
            result.AddRange(Split(seg));
        return StrokeSegmentOps.FromSegments(result);
    }

    private IEnumerable<StrokeSegment> Split(StrokeSegment seg)
    {
        var step = 1.0 / (PartCount - 1.0);
        if (double.IsNaN(step) || double.IsInfinity(step) || step <= 0)
            return [seg];

        var a1 = IsVertical ? seg.P1.Y : seg.P1.X;
        var a2 = IsVertical ? seg.P2.Y : seg.P2.X;
        var da = a2 - a1;

        if (Math.Abs(da) < Epsilon)
            return a1 < -Epsilon || a1 > 1 + Epsilon ? [] : [seg];

        var tA = (0 - a1) / da;
        var tB = (1 - a1) / da;
        var tMin = Math.Max(0, Math.Min(tA, tB));
        var tMax = Math.Min(1, Math.Max(tA, tB));
        if (tMax - tMin <= Epsilon)
            return [];

        var clipped = seg;
        if (tMin > Epsilon || 1 - tMax > Epsilon)
        {
            clipped = new StrokeSegment
            {
                P1 = seg.PointAt(tMin),
                P2 = seg.PointAt(tMax),
                IsBlank = seg.IsBlank,
                T1 = seg.T1,
                T2 = seg.T2
            };
            a1 = IsVertical ? clipped.P1.Y : clipped.P1.X;
            a2 = IsVertical ? clipped.P2.Y : clipped.P2.X;
            da = a2 - a1;
            if (Math.Abs(da) < Epsilon)
                return [clipped];
        }

        var cuts = new List<double> { 0, 1 };
        for (var i = 1; i < PartCount - 1; i++)
        {
            var t = (i * step - a1) / da;
            if (t > Epsilon && t < 1 - Epsilon)
                cuts.Add(t);
        }
        if (cuts.Count <= 2)
            return [clipped];

        cuts.Sort();
        var pieces = new List<StrokeSegment>(cuts.Count - 1);
        for (var i = 1; i < cuts.Count; i++)
        {
            if (cuts[i] - cuts[i - 1] <= Epsilon)
                continue;
            pieces.Add(new StrokeSegment
            {
                P1 = clipped.PointAt(cuts[i - 1]),
                P2 = clipped.PointAt(cuts[i]),
                IsBlank = clipped.IsBlank,
                T1 = i == 1 ? clipped.T1 : (byte)1,
                T2 = i == cuts.Count - 1 ? clipped.T2 : (byte)1
            });
        }
        return pieces;
    }
}
