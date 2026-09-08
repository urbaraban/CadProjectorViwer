namespace CadProjector.Geometry.Primitives;

public readonly record struct Point3(double X, double Y, double Z)
{
    public static Point3 Zero => new(0, 0, 0);

    public Point2 ToPoint2() => new(X, Y);
}
