using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Mesh;

public sealed class TriangleMesh
{
    public Point3[] Vertices { get; }
    public int[] Indices { get; }
    public Aabb3 Bounds { get; }

    public int TriangleCount => Indices.Length / 3;

    public TriangleMesh(Point3[] vertices, int[] indices)
    {
        if (indices.Length % 3 != 0)
            throw new ArgumentException("Index count must be a multiple of 3.", nameof(indices));
        Vertices = vertices;
        Indices = indices;
        Bounds = vertices.Length == 0 ? Aabb3.Empty : Aabb3.FromPoints(vertices);
    }

    public void GetTriangle(int index, out Point3 a, out Point3 b, out Point3 c)
    {
        var i = index * 3;
        a = Vertices[Indices[i]];
        b = Vertices[Indices[i + 1]];
        c = Vertices[Indices[i + 2]];
    }

    public Point3 TriangleCentroid(int index)
    {
        GetTriangle(index, out var a, out var b, out var c);
        return (a + b + c) * (1.0 / 3.0);
    }

    public Aabb3 TriangleBounds(int index)
    {
        GetTriangle(index, out var a, out var b, out var c);
        return Aabb3.Empty.Encapsulate(a).Encapsulate(b).Encapsulate(c);
    }

    public Point3 TriangleNormal(int index)
    {
        GetTriangle(index, out var a, out var b, out var c);
        return Point3.Cross(b - a, c - a).Normalized();
    }
}
