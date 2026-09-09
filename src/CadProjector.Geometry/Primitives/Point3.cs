namespace CadProjector.Geometry.Primitives;

public readonly record struct Point3(double X, double Y, double Z)
{
    public static Point3 Zero => new(0, 0, 0);
    public static Point3 UnitX => new(1, 0, 0);
    public static Point3 UnitY => new(0, 1, 0);
    public static Point3 UnitZ => new(0, 0, 1);

    public Point2 ToPoint2() => new(X, Y);

    public double LengthSquared => X * X + Y * Y + Z * Z;
    public double Length => Math.Sqrt(LengthSquared);

    public Point3 Normalized()
    {
        var len = Length;
        return len < 1e-15 ? Zero : this * (1.0 / len);
    }

    public static Point3 operator +(Point3 a, Point3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Point3 operator -(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Point3 operator -(Point3 a) => new(-a.X, -a.Y, -a.Z);
    public static Point3 operator *(Point3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Point3 operator *(double s, Point3 a) => a * s;

    public static double Dot(Point3 a, Point3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Point3 Cross(Point3 a, Point3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public double this[int axis] => axis switch
    {
        0 => X,
        1 => Y,
        _ => Z
    };
}
