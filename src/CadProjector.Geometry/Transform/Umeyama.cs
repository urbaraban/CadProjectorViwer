using CadProjector.Geometry.Linear;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Transform;

/// <summary>
/// Least-squares similarity (Umeyama / Kabsch): dst ≈ s · R · src + t.
/// Three non-collinear points determine a unique rigid pose; a fourth
/// over-constrains and reports residual. Used to snap an STL to the table.
/// </summary>
public static class Umeyama
{
    public readonly record struct Result(double Scale, Mat3 Rotation, Point3 Translation, double Rms);

    public static bool TrySolve(
        IReadOnlyList<Point3> src,
        IReadOnlyList<Point3> dst,
        bool allowScale,
        out Result result)
    {
        result = default;
        if (src.Count != dst.Count || src.Count < 3)
            return false;

        var n = src.Count;
        var muS = Mean(src);
        var muD = Mean(dst);
        var centeredS = new Point3[n];
        var centeredD = new Point3[n];
        var varS = 0.0;
        var cov = default(Mat3);
        for (var i = 0; i < n; i++)
        {
            centeredS[i] = src[i] - muS;
            centeredD[i] = dst[i] - muD;
            varS += centeredS[i].LengthSquared;
            cov += Mat3.Outer(centeredS[i], centeredD[i]);
        }

        if (varS < 1e-12)
            return false;

        Mat3.Svd(cov, out var u, out var sigma, out var v);
        var vt = v.Transpose();
        var r = v * u.Transpose();
        if (r.Det() < 0)
        {
            var d = new Mat3(1, 0, 0, 0, 1, 0, 0, 0, -1);
            r = v * d * u.Transpose();
            sigma = new Point3(sigma.X, sigma.Y, -sigma.Z);
        }

        if (r.Det() < 0)
            return false;

        var scale = 1.0;
        if (allowScale)
        {
            var trace = Math.Abs(sigma.X) + Math.Abs(sigma.Y) + Math.Abs(sigma.Z);
            scale = trace / varS;
            if (!double.IsFinite(scale) || scale <= 1e-12)
                return false;
        }

        var t = muD - r.Mul(muS) * scale;
        var rms = 0.0;
        for (var i = 0; i < n; i++)
        {
            var mapped = r.Mul(src[i]) * scale + t;
            rms += (mapped - dst[i]).LengthSquared;
        }

        rms = Math.Sqrt(rms / n);
        result = new Result(scale, r, t, rms);
        return true;
    }

    private static Point3 Mean(IReadOnlyList<Point3> pts)
    {
        var s = Point3.Zero;
        foreach (var p in pts)
            s += p;
        return s * (1.0 / pts.Count);
    }
}
