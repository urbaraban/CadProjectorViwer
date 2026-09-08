using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Math2D;

/// <summary>Perspective transform from unit square (0,0)-(1,0)-(1,1)-(0,1) to four corners.</summary>
public sealed class Homography2D
{
    private readonly double[] _h; // row-major 3x3, h[8]=1

    private Homography2D(double[] h) => _h = h;

    public static Homography2D FromUnitSquare(Point2 tl, Point2 tr, Point2 br, Point2 bl)
    {
        Point2[] src = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        Point2[] dst = [tl, tr, br, bl];
        return Solve(src, dst);
    }

    public Point2 Transform(Point2 p)
    {
        var w = _h[6] * p.X + _h[7] * p.Y + _h[8];
        if (Math.Abs(w) < 1e-12) w = 1e-12;
        var x = (_h[0] * p.X + _h[1] * p.Y + _h[2]) / w;
        var y = (_h[3] * p.X + _h[4] * p.Y + _h[5]) / w;
        return new Point2(x, y);
    }

    private static Homography2D Solve(IReadOnlyList<Point2> src, IReadOnlyList<Point2> dst)
    {
        // Direct linear transform, 8 unknowns (h22=1)
        var a = new double[8, 8];
        var b = new double[8];
        for (var i = 0; i < 4; i++)
        {
            var xs = src[i].X;
            var ys = src[i].Y;
            var xd = dst[i].X;
            var yd = dst[i].Y;
            var r = i * 2;
            a[r, 0] = xs; a[r, 1] = ys; a[r, 2] = 1;
            a[r, 3] = 0; a[r, 4] = 0; a[r, 5] = 0;
            a[r, 6] = -xs * xd; a[r, 7] = -ys * xd;
            b[r] = xd;
            a[r + 1, 0] = 0; a[r + 1, 1] = 0; a[r + 1, 2] = 0;
            a[r + 1, 3] = xs; a[r + 1, 4] = ys; a[r + 1, 5] = 1;
            a[r + 1, 6] = -xs * yd; a[r + 1, 7] = -ys * yd;
            b[r + 1] = yd;
        }

        var h8 = SolveLinear(a, b);
        return new Homography2D(
        [
            h8[0], h8[1], h8[2],
            h8[3], h8[4], h8[5],
            h8[6], h8[7], 1
        ]);
    }

    private static double[] SolveLinear(double[,] a, double[] b)
    {
        const int n = 8;
        var m = new double[n, n + 1];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
                m[i, j] = a[i, j];
            m[i, n] = b[i];
        }

        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var r = col + 1; r < n; r++)
            {
                if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col]))
                    pivot = r;
            }
            for (var c = 0; c <= n; c++)
                (m[col, c], m[pivot, c]) = (m[pivot, c], m[col, c]);

            var div = m[col, col];
            if (Math.Abs(div) < 1e-14)
                continue;
            for (var c = col; c <= n; c++)
                m[col, c] /= div;

            for (var r = 0; r < n; r++)
            {
                if (r == col) continue;
                var f = m[r, col];
                for (var c = col; c <= n; c++)
                    m[r, c] -= f * m[col, c];
            }
        }

        var x = new double[n];
        for (var i = 0; i < n; i++)
            x[i] = m[i, n];
        return x;
    }
}
