namespace CadProjector.Rendering.Modules;

/// <summary>Remove overlapping collinear stroke segments (legacy Unduplicated).</summary>
public sealed class UnduplicateModule : IFrameModule
{
    public string Name => "Unduplicate";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Tolerance", Min = 0, Increment = 1e-6, Format = "0.#######")]
    public double Tolerance { get; set; } = 1e-6;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || input.Points.Count < 2)
            return input;

        var tol = Math.Max(Tolerance, 1e-10);
        // Legacy Unduplicated sees only VectorLine geometry. Our point stream also
        // has blank hops between independent strokes; those hops often retrace a
        // real edge (square + X, hatch ends) and would delete the lit line.
        var current = StrokeSegmentOps.ToSegments(input).Where(s => !s.IsBlank).ToList();
        if (current.Count <= 1)
            return input;

        bool changed;
        var safety = 0;
        do
        {
            changed = false;
            if (++safety > Math.Max(10, current.Count * 2)) break;

            var removed = new bool[current.Count];
            var toAdd = new List<StrokeSegment>();

            for (var i = 0; i < current.Count; i++)
            {
                if (removed[i]) continue;
                var li = current[i];
                var vi = Vec(li.P1, li.P2);
                var lenVi = Len(vi);
                if (lenVi < tol) continue;
                var axis = Normalize(vi);
                var origin = li.P1;

                for (var j = i + 1; j < current.Count; j++)
                {
                    if (removed[j]) continue;
                    var lj = current[j];
                    var vj = Vec(lj.P1, lj.P2);
                    var lenVj = Len(vj);
                    if (lenVj < tol) continue;

                    var crossDirs = Math.Abs(Cross(vi, vj));
                    var maxSegLen = Math.Max(lenVi, lenVj);
                    var sinTol = Math.Min(1.0, tol / Math.Max(maxSegLen, tol));
                    if (crossDirs / (lenVi * lenVj + tol) > sinTol) continue;

                    var dJa = DistPointLine(lj.P1, li.P1, li.P2);
                    var dJb = DistPointLine(lj.P2, li.P1, li.P2);
                    var dIa = DistPointLine(li.P1, lj.P1, lj.P2);
                    var dIb = DistPointLine(li.P2, lj.P1, lj.P2);
                    if (!(dJa <= tol && dJb <= tol) && !(dIa <= tol && dIb <= tol))
                        continue;

                    var aMin = Math.Min(Project(li.P1, origin, axis), Project(li.P2, origin, axis));
                    var aMax = Math.Max(Project(li.P1, origin, axis), Project(li.P2, origin, axis));
                    var bMin = Math.Min(Project(lj.P1, origin, axis), Project(lj.P2, origin, axis));
                    var bMax = Math.Max(Project(lj.P1, origin, axis), Project(lj.P2, origin, axis));
                    var overlap = Math.Min(aMax, bMax) - Math.Max(aMin, bMin);
                    if (overlap <= tol) continue;

                    var lenA = aMax - aMin;
                    var lenB = bMax - bMin;
                    var smallLen = Math.Min(lenA, lenB);
                    var smallerIdx = lenA <= lenB ? i : j;
                    var sMin = lenA <= lenB ? aMin : bMin;
                    var sMax = lenA <= lenB ? aMax : bMax;
                    var lMin = lenA <= lenB ? bMin : aMin;
                    var lMax = lenA <= lenB ? bMax : aMax;
                    var smaller = current[smallerIdx];

                    if (overlap >= smallLen - tol || (smallLen > tol && overlap >= smallLen * (1.0 - 1e-5)))
                    {
                        removed[smallerIdx] = true;
                        changed = true;
                        continue;
                    }

                    var o1 = Math.Max(sMin, lMin);
                    var o2 = Math.Min(sMax, lMax);
                    var o1World = At(origin, axis, o1);
                    var o2World = At(origin, axis, o2);

                    RenderPoint sOrigin;
                    (double X, double Y) sAxis;
                    if (smallerIdx == i)
                    {
                        sOrigin = li.P1;
                        sAxis = axis;
                    }
                    else
                    {
                        sOrigin = lj.P1;
                        sAxis = Normalize(vj);
                    }

                    var ss1 = Project(smaller.P1, sOrigin, sAxis);
                    var ss2 = Project(smaller.P2, sOrigin, sAxis);
                    var ssMin = Math.Min(ss1, ss2);
                    var ssMax = Math.Max(ss1, ss2);
                    var ttMin = Math.Min(Project(o1World, sOrigin, sAxis), Project(o2World, sOrigin, sAxis));
                    var ttMax = Math.Max(Project(o1World, sOrigin, sAxis), Project(o2World, sOrigin, sAxis));

                    removed[smallerIdx] = true;
                    changed = true;

                    if (ttMin - ssMin > tol)
                        toAdd.Add(Seg(sOrigin, sAxis, ssMin, ttMin, smaller.IsBlank));
                    if (ssMax - ttMax > tol)
                        toAdd.Add(Seg(sOrigin, sAxis, ttMax, ssMax, smaller.IsBlank));
                }
            }

            if (changed)
            {
                var next = new List<StrokeSegment>();
                for (var i = 0; i < current.Count; i++)
                    if (!removed[i]) next.Add(current[i]);
                next.AddRange(toAdd);
                current = next;
            }
        } while (changed);

        return StrokeSegmentOps.FromSegments(current);
    }

    private static (double X, double Y) Vec(RenderPoint a, RenderPoint b) => (b.X - a.X, b.Y - a.Y);
    private static double Dot((double X, double Y) u, (double X, double Y) v) => u.X * v.X + u.Y * v.Y;
    private static double Cross((double X, double Y) u, (double X, double Y) v) => u.X * v.Y - u.Y * v.X;
    private static double Len((double X, double Y) v) => Math.Sqrt(v.X * v.X + v.Y * v.Y);
    private static (double X, double Y) Normalize((double X, double Y) v)
    {
        var l = Len(v);
        return l < 1e-15 ? (0, 0) : (v.X / l, v.Y / l);
    }

    private double DistPointLine(RenderPoint p, RenderPoint a, RenderPoint b)
    {
        var v = Vec(a, b);
        var w = Vec(a, p);
        var vl = Len(v);
        if (vl < Tolerance) return Len(w);
        return Math.Abs(Cross(v, w)) / vl;
    }

    private static double Project(RenderPoint p, RenderPoint origin, (double X, double Y) axis) =>
        Dot(Vec(origin, p), axis);

    private static RenderPoint At(RenderPoint origin, (double X, double Y) axis, double t) => new()
    {
        X = origin.X + axis.X * t,
        Y = origin.Y + axis.Y * t
    };

    private static StrokeSegment Seg(RenderPoint origin, (double X, double Y) axis, double a, double b, bool blank) =>
        new()
        {
            P1 = At(origin, axis, a),
            P2 = At(origin, axis, b),
            IsBlank = blank
        };
}
