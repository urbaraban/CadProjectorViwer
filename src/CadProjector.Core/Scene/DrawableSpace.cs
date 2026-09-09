using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>Local↔world transforms and tree walks for drawable groups.</summary>
public static class DrawableSpace
{
    public static Point2 TransformLocal(Point2 local, Drawable d)
    {
        var s = d.Scale;
        var x = local.X * s;
        var y = local.Y * s;
        if (Math.Abs(d.RotationDeg) > 1e-9)
        {
            var rad = d.RotationDeg * Math.PI / 180.0;
            var c = Math.Cos(rad);
            var sn = Math.Sin(rad);
            var rx = x * c - y * sn;
            var ry = x * sn + y * c;
            x = rx;
            y = ry;
        }

        return new Point2(x + d.Translation.X, y + d.Translation.Y);
    }

    /// <summary>
    /// Visit every contour in world space. Skips invisible nodes (and their descendants).
    /// <paramref name="leaf"/> owns the contour (color); <paramref name="worldZ"/> is composed Z.
    /// </summary>
    public static void VisitContours(
        Drawable root,
        Action<Drawable, IReadOnlyList<Point2>, double> visitor)
    {
        Walk(root, static p => p, 0, visitor);
    }

    private static void Walk(
        Drawable d,
        Func<Point2, Point2> parentToWorld,
        double parentZ,
        Action<Drawable, IReadOnlyList<Point2>, double> visitor)
    {
        if (!d.IsVisible)
            return;

        Point2 ToWorld(Point2 local) => parentToWorld(TransformLocal(local, d));
        var z = parentZ + d.Translation.Z;

        foreach (var contour in d.Contours)
        {
            if (contour.Count == 0)
                continue;
            var world = new List<Point2>(contour.Count);
            foreach (var p in contour)
                world.Add(ToWorld(p));
            visitor(d, world, z);
        }

        foreach (var child in d.Children)
            Walk(child, ToWorld, z, visitor);
    }

    /// <summary>
    /// Bake <paramref name="parent"/>'s transform into <paramref name="child"/> (and nested),
    /// leaving the child tree with identity local transforms (Z summed).
    /// </summary>
    public static void BakeParentTransform(Drawable parent, Drawable child)
    {
        Bake(child, p => TransformLocal(p, parent), parent.Translation.Z);
    }

    private static void Bake(Drawable node, Func<Point2, Point2> parentToWorld, double parentZ)
    {
        Point2 ToWorld(Point2 local) => parentToWorld(TransformLocal(local, node));

        node.Contours = node.Contours
            .Select(c => c.Select(ToWorld).ToList())
            .ToList();

        var z = parentZ + node.Translation.Z;
        foreach (var ch in node.Children)
            Bake(ch, ToWorld, z);

        node.Translation = new Point3(0, 0, z);
        node.RotationDeg = 0;
        node.Scale = 1;
    }
}
