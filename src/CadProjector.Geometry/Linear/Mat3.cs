using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Linear;

/// <summary>Row-major 3×3, acts on column vectors.</summary>
public readonly struct Mat3
{
    public readonly double M00, M01, M02, M10, M11, M12, M20, M21, M22;

    public Mat3(
        double m00, double m01, double m02,
        double m10, double m11, double m12,
        double m20, double m21, double m22)
    {
        M00 = m00; M01 = m01; M02 = m02;
        M10 = m10; M11 = m11; M12 = m12;
        M20 = m20; M21 = m21; M22 = m22;
    }

    public static Mat3 Identity => new(1, 0, 0, 0, 1, 0, 0, 0, 1);

    public Point3 Mul(Point3 p) => new(
        M00 * p.X + M01 * p.Y + M02 * p.Z,
        M10 * p.X + M11 * p.Y + M12 * p.Z,
        M20 * p.X + M21 * p.Y + M22 * p.Z);

    public Mat3 Transpose() => new(M00, M10, M20, M01, M11, M21, M02, M12, M22);

    public double Det() =>
        M00 * (M11 * M22 - M12 * M21)
        - M01 * (M10 * M22 - M12 * M20)
        + M02 * (M10 * M21 - M11 * M20);

    public static Mat3 operator +(Mat3 a, Mat3 b) => new(
        a.M00 + b.M00, a.M01 + b.M01, a.M02 + b.M02,
        a.M10 + b.M10, a.M11 + b.M11, a.M12 + b.M12,
        a.M20 + b.M20, a.M21 + b.M21, a.M22 + b.M22);

    public static Mat3 operator *(Mat3 a, double s) => new(
        a.M00 * s, a.M01 * s, a.M02 * s,
        a.M10 * s, a.M11 * s, a.M12 * s,
        a.M20 * s, a.M21 * s, a.M22 * s);

    public static Mat3 operator *(Mat3 a, Mat3 b) => new(
        a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20,
        a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21,
        a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22,
        a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20,
        a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21,
        a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22,
        a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20,
        a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21,
        a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22);

    public static Mat3 Outer(Point3 a, Point3 b) => new(
        a.X * b.X, a.X * b.Y, a.X * b.Z,
        a.Y * b.X, a.Y * b.Y, a.Y * b.Z,
        a.Z * b.X, a.Z * b.Y, a.Z * b.Z);

    /// <summary>Same convention as MeshTarget: Rx, then Ry, then Rz.</summary>
    public static Mat3 FromEulerXyzDeg(Point3 deg)
    {
        var r = Identity;
        if (deg.X is not 0)
            r = RotationX(deg.X * Math.PI / 180.0) * r;
        if (deg.Y is not 0)
            r = RotationY(deg.Y * Math.PI / 180.0) * r;
        if (deg.Z is not 0)
            r = RotationZ(deg.Z * Math.PI / 180.0) * r;
        return r;
    }

    public Point3 ToEulerXyzDeg()
    {
        var sy = Math.Clamp(-M20, -1, 1);
        var ry = Math.Asin(sy);
        var cy = Math.Cos(ry);
        double rx, rz;
        if (Math.Abs(cy) > 1e-8)
        {
            rx = Math.Atan2(M21, M22);
            rz = Math.Atan2(M10, M00);
        }
        else
        {
            rx = Math.Atan2(-M12, M11);
            rz = 0;
        }

        return new Point3(rx * 180.0 / Math.PI, ry * 180.0 / Math.PI, rz * 180.0 / Math.PI);
    }

    public static Mat3 RotationX(double rad)
    {
        var c = Math.Cos(rad);
        var s = Math.Sin(rad);
        return new(1, 0, 0, 0, c, -s, 0, s, c);
    }

    public static Mat3 RotationY(double rad)
    {
        var c = Math.Cos(rad);
        var s = Math.Sin(rad);
        return new(c, 0, s, 0, 1, 0, -s, 0, c);
    }

    public static Mat3 RotationZ(double rad)
    {
        var c = Math.Cos(rad);
        var s = Math.Sin(rad);
        return new(c, -s, 0, s, c, 0, 0, 0, 1);
    }

    /// <summary>Thin SVD via Jacobi eigen of AᵀA. Good enough for 3×3 Umeyama.</summary>
    public static void Svd(Mat3 a, out Mat3 u, out Point3 sigma, out Mat3 v)
    {
        JacobiEigen(a.Transpose() * a, out var eval, out v);
        var s0 = Math.Sqrt(Math.Max(0, eval.X));
        var s1 = Math.Sqrt(Math.Max(0, eval.Y));
        var s2 = Math.Sqrt(Math.Max(0, eval.Z));
        sigma = new Point3(s0, s1, s2);

        var u0 = Column(a * v, 0);
        var u1 = Column(a * v, 1);
        var u2 = Column(a * v, 2);
        if (s0 > 1e-12) u0 *= 1.0 / s0;
        else u0 = Point3.UnitX;
        if (s1 > 1e-12) u1 *= 1.0 / s1;
        else u1 = Point3.Cross(u0, Point3.UnitZ).Normalized();
        if (u1.LengthSquared < 1e-12)
            u1 = Point3.Cross(u0, Point3.UnitX).Normalized();
        if (s2 > 1e-12) u2 *= 1.0 / s2;
        else u2 = Point3.Cross(u0, u1).Normalized();
        u0 = u0.Normalized();
        u1 = u1.Normalized();
        u2 = u2.Normalized();
        if (Point3.Dot(u2, Point3.Cross(u0, u1)) < 0)
            u2 = -u2;
        u = FromColumns(u0, u1, u2);
    }

    public static Mat3 FromColumns(Point3 a, Point3 b, Point3 c) => new(
        a.X, b.X, c.X,
        a.Y, b.Y, c.Y,
        a.Z, b.Z, c.Z);

    public static Point3 Column(Mat3 m, int i) => i switch
    {
        0 => new Point3(m.M00, m.M10, m.M20),
        1 => new Point3(m.M01, m.M11, m.M21),
        _ => new Point3(m.M02, m.M12, m.M22)
    };

    private static void JacobiEigen(Mat3 symmetric, out Point3 eval, out Mat3 vec)
    {
        var a = new[,]
        {
            { symmetric.M00, symmetric.M01, symmetric.M02 },
            { symmetric.M10, symmetric.M11, symmetric.M12 },
            { symmetric.M20, symmetric.M21, symmetric.M22 }
        };
        var v = new[,] { { 1.0, 0, 0 }, { 0, 1.0, 0 }, { 0, 0, 1.0 } };

        for (var iter = 0; iter < 40; iter++)
        {
            var p = 0;
            var q = 1;
            var max = Math.Abs(a[0, 1]);
            if (Math.Abs(a[0, 2]) > max) { max = Math.Abs(a[0, 2]); p = 0; q = 2; }
            if (Math.Abs(a[1, 2]) > max) { max = Math.Abs(a[1, 2]); p = 1; q = 2; }
            if (max < 1e-15)
                break;

            var app = a[p, p];
            var aqq = a[q, q];
            var apq = a[p, q];
            var phi = 0.5 * Math.Atan2(2 * apq, aqq - app);
            var c = Math.Cos(phi);
            var s = Math.Sin(phi);

            for (var r = 0; r < 3; r++)
            {
                if (r == p || r == q)
                    continue;
                var arp = a[r, p];
                var arq = a[r, q];
                a[r, p] = a[p, r] = c * arp - s * arq;
                a[r, q] = a[q, r] = s * arp + c * arq;
            }

            a[p, p] = c * c * app - 2 * s * c * apq + s * s * aqq;
            a[q, q] = s * s * app + 2 * s * c * apq + c * c * aqq;
            a[p, q] = a[q, p] = 0;

            for (var r = 0; r < 3; r++)
            {
                var vrp = v[r, p];
                var vrq = v[r, q];
                v[r, p] = c * vrp - s * vrq;
                v[r, q] = s * vrp + c * vrq;
            }
        }

        eval = new Point3(a[0, 0], a[1, 1], a[2, 2]);
        vec = new Mat3(v[0, 0], v[0, 1], v[0, 2], v[1, 0], v[1, 1], v[1, 2], v[2, 0], v[2, 1], v[2, 2]);
    }
}
