using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Mesh;

public readonly record struct MeshHit(Point3 Point, double T, int TriangleIndex, Point3 Normal);

public static class RayTriangle
{
    private const double Epsilon = 1e-9;

    /// <summary>
    /// Möller–Trumbore. <paramref name="frontFacingOnly"/> keeps hits where Dot(normal, dir) &lt; 0.
    /// </summary>
    public static bool Intersect(
        Point3 origin,
        Point3 dir,
        Point3 v0,
        Point3 v1,
        Point3 v2,
        bool frontFacingOnly,
        out double t,
        out Point3 normal)
    {
        t = 0;
        var e1 = v1 - v0;
        var e2 = v2 - v0;
        var pvec = Point3.Cross(dir, e2);
        var det = Point3.Dot(e1, pvec);
        if (Math.Abs(det) < Epsilon)
        {
            normal = default;
            return false;
        }

        if (frontFacingOnly && det < 0)
        {
            normal = default;
            return false;
        }

        var invDet = 1.0 / det;
        var tvec = origin - v0;
        var u = Point3.Dot(tvec, pvec) * invDet;
        if (u < 0 || u > 1)
        {
            normal = default;
            return false;
        }

        var qvec = Point3.Cross(tvec, e1);
        var v = Point3.Dot(dir, qvec) * invDet;
        if (v < 0 || u + v > 1)
        {
            normal = default;
            return false;
        }

        t = Point3.Dot(e2, qvec) * invDet;
        if (t < Epsilon)
        {
            normal = default;
            return false;
        }

        normal = Point3.Cross(e1, e2).Normalized();
        return true;
    }
}
