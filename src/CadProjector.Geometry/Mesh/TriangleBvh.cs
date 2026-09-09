using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Mesh;

/// <summary>Binary median-split BVH over mesh triangles.</summary>
public sealed class TriangleBvh
{
    private const int LeafSize = 8;
    private readonly TriangleMesh _mesh;
    private readonly int[] _order;
    private readonly Node[] _nodes;
    private readonly int _root;

    private struct Node
    {
        public Aabb3 Box;
        public int Left;
        public int Right;
        public int First;
        public int Count;
    }

    private TriangleBvh(TriangleMesh mesh, int[] order, Node[] nodes, int root)
    {
        _mesh = mesh;
        _order = order;
        _nodes = nodes;
        _root = root;
    }

    public static TriangleBvh Build(TriangleMesh mesh)
    {
        var n = mesh.TriangleCount;
        var order = new int[n];
        for (var i = 0; i < n; i++)
            order[i] = i;
        var nodes = new List<Node>(Math.Max(4, n * 2));
        var centroids = new Point3[n];
        for (var i = 0; i < n; i++)
            centroids[i] = mesh.TriangleCentroid(i);

        var root = BuildRange(mesh, order, centroids, nodes, 0, n);
        return new TriangleBvh(mesh, order, nodes.ToArray(), root);
    }

    public bool TryHit(Point3 origin, Point3 dir, bool frontFacingOnly, out MeshHit hit)
    {
        hit = default;
        if (_nodes.Length == 0)
            return false;

        var bestT = double.PositiveInfinity;
        var bestTri = -1;
        Point3 bestN = default;
        Walk(_root, origin, dir, frontFacingOnly, ref bestT, ref bestTri, ref bestN);
        if (bestTri < 0)
            return false;

        hit = new MeshHit(origin + dir * bestT, bestT, bestTri, bestN);
        return true;
    }

    private void Walk(
        int index,
        Point3 origin,
        Point3 dir,
        bool frontFacingOnly,
        ref double bestT,
        ref int bestTri,
        ref Point3 bestN)
    {
        var node = _nodes[index];
        if (!node.Box.IntersectsRay(origin, dir, 0, bestT))
            return;

        if (node.Left < 0)
        {
            var end = node.First + node.Count;
            for (var i = node.First; i < end; i++)
            {
                var tri = _order[i];
                _mesh.GetTriangle(tri, out var a, out var b, out var c);
                if (!RayTriangle.Intersect(origin, dir, a, b, c, frontFacingOnly, out var t, out var n))
                    continue;
                if (t >= bestT)
                    continue;
                bestT = t;
                bestTri = tri;
                bestN = n;
            }
            return;
        }

        Walk(node.Left, origin, dir, frontFacingOnly, ref bestT, ref bestTri, ref bestN);
        Walk(node.Right, origin, dir, frontFacingOnly, ref bestT, ref bestTri, ref bestN);
    }

    private static int BuildRange(
        TriangleMesh mesh,
        int[] order,
        Point3[] centroids,
        List<Node> nodes,
        int start,
        int count)
    {
        var box = Aabb3.Empty;
        for (var i = 0; i < count; i++)
            box = box.Encapsulate(mesh.TriangleBounds(order[start + i]));

        if (count <= LeafSize)
        {
            nodes.Add(new Node { Box = box, Left = -1, Right = -1, First = start, Count = count });
            return nodes.Count - 1;
        }

        var size = box.Size;
        var axis = size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
        Array.Sort(order, start, count, Comparer<int>.Create((a, b) =>
            centroids[a][axis].CompareTo(centroids[b][axis])));

        var mid = Math.Max(1, count / 2);
        var self = nodes.Count;
        nodes.Add(default);
        var left = BuildRange(mesh, order, centroids, nodes, start, mid);
        var right = BuildRange(mesh, order, centroids, nodes, start + mid, count - mid);
        nodes[self] = new Node { Box = box, Left = left, Right = right, First = 0, Count = 0 };
        return self;
    }
}
