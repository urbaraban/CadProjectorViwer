namespace CadProjector.Geometry.Primitives;

public readonly record struct Point2(double X, double Y)
{
    public static Point2 Zero => new(0, 0);
}
