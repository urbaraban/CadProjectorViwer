using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering;

/// <summary>Stroke segment between two points (legacy VectorLine analogue).</summary>
public sealed class StrokeSegment
{
    public RenderPoint P1 { get; set; } = new();
    public RenderPoint P2 { get; set; } = new();
    public bool IsBlank { get; set; }
    public byte T1 { get; set; } = 1;
    public byte T2 { get; set; } = 1;

    public double Length
    {
        get
        {
            var dx = P2.X - P1.X;
            var dy = P2.Y - P1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public StrokeSegment Reverse() => new()
    {
        P1 = P2.Clone(),
        P2 = P1.Clone(),
        IsBlank = IsBlank,
        T1 = T2,
        T2 = T1
    };

    public RenderPoint PointAt(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new RenderPoint
        {
            X = P1.X + (P2.X - P1.X) * t,
            Y = P1.Y + (P2.Y - P1.Y) * t,
            Z = P1.Z + (P2.Z - P1.Z) * t,
            Blanked = false,
            Mass = 1,
            Color = P2.Color
        };
    }
}

public static class StrokeSegmentOps
{
    public static List<StrokeSegment> ToSegments(LinesCollection lines)
    {
        var segs = new List<StrokeSegment>();
        var pts = lines.Points;
        for (var i = 1; i < pts.Count; i++)
        {
            segs.Add(new StrokeSegment
            {
                P1 = pts[i - 1].Clone(),
                P2 = pts[i].Clone(),
                IsBlank = pts[i].Blanked,
                T1 = pts[i - 1].Mass == 0 ? (byte)1 : pts[i - 1].Mass,
                T2 = pts[i].Mass == 0 ? (byte)1 : pts[i].Mass
            });
        }
        return segs;
    }

    public static LinesCollection FromSegments(IReadOnlyList<StrokeSegment> segs)
    {
        var lines = new LinesCollection();
        if (segs.Count == 0) return lines;

        var first = segs[0].P1.Clone();
        first.Blanked = true;
        first.Mass = segs[0].T1;
        lines.Points.Add(first);

        foreach (var s in segs)
        {
            var p = s.P2.Clone();
            p.Blanked = s.IsBlank;
            p.Mass = s.T2;
            lines.Points.Add(p);
        }
        return lines;
    }

    /// <summary>Liang–Barsky clip of a segment to an axis-aligned rectangle.</summary>
    public static bool TryClipToRect(StrokeSegment seg, Rect2 rect, out StrokeSegment clipped)
    {
        clipped = seg;
        var minX = Math.Min(rect.X, rect.X + rect.Width);
        var maxX = Math.Max(rect.X, rect.X + rect.Width);
        var minY = Math.Min(rect.Y, rect.Y + rect.Height);
        var maxY = Math.Max(rect.Y, rect.Y + rect.Height);

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

    public static double DistSq(RenderPoint a, RenderPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return dx * dx + dy * dy;
    }

    public static double TotalLength(IEnumerable<StrokeSegment> segs)
    {
        double sum = 0;
        foreach (var s in segs) sum += s.Length;
        return sum;
    }
}
