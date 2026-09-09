using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Mesh;

public readonly record struct Aabb3(Point3 Min, Point3 Max)
{
    public Point3 Center => (Min + Max) * 0.5;
    public Point3 Size => Max - Min;
    public double Diagonal => Size.Length;

    public static Aabb3 Empty => new(
        new Point3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity),
        new Point3(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity));

    public static Aabb3 FromPoints(ReadOnlySpan<Point3> points)
    {
        var box = Empty;
        foreach (var p in points)
            box = box.Encapsulate(p);
        return box;
    }

    public Aabb3 Encapsulate(Point3 p) => new(
        new Point3(Math.Min(Min.X, p.X), Math.Min(Min.Y, p.Y), Math.Min(Min.Z, p.Z)),
        new Point3(Math.Max(Max.X, p.X), Math.Max(Max.Y, p.Y), Math.Max(Max.Z, p.Z)));

    public Aabb3 Encapsulate(Aabb3 other) => Encapsulate(other.Min).Encapsulate(other.Max);

    public bool IntersectsRay(Point3 origin, Point3 dir, double tMin, double tMax)
    {
        if (!Slab(origin.X, dir.X, Min.X, Max.X, ref tMin, ref tMax)) return false;
        if (!Slab(origin.Y, dir.Y, Min.Y, Max.Y, ref tMin, ref tMax)) return false;
        if (!Slab(origin.Z, dir.Z, Min.Z, Max.Z, ref tMin, ref tMax)) return false;
        return tMax >= tMin;
    }

    private static bool Slab(double origin, double dir, double min, double max, ref double tMin, ref double tMax)
    {
        if (Math.Abs(dir) < 1e-15)
            return origin >= min && origin <= max;

        var inv = 1.0 / dir;
        var t1 = (min - origin) * inv;
        var t2 = (max - origin) * inv;
        if (t1 > t2)
            (t1, t2) = (t2, t1);
        tMin = Math.Max(tMin, t1);
        tMax = Math.Min(tMax, t2);
        return tMax >= tMin;
    }
}
