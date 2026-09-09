using CadProjector.Geometry.Linear;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;
using CadProjector.Geometry.Transform;

namespace CadProjector.Core.Scene;

/// <summary>
/// STL surface as the scene projection target. Drawings are ray-cast along
/// <see cref="ProjectionAxis"/> (default −Z) onto the aligned mesh.
/// </summary>
public sealed class MeshTarget
{
    public const int TriangleWarnLimit = 500_000;
    /// <summary>Preview budget. STL vertices are not shared, so we stride across triangles.</summary>
    internal const int WireframeEdgeCap = 18_000;

    private TriangleMesh? _world;
    private TriangleBvh? _bvh;
    private IReadOnlyList<(Point3 A, Point3 B)>? _worldEdges;
    private bool _dirty = true;

    public required TriangleMesh Mesh { get; init; }
    public string SourcePath { get; set; } = "";
    public Point3 Translation { get; set; }
    public Point3 RotationDeg { get; set; }
    public Point3 Scale { get; set; } = new(1, 1, 1);
    public Point3 ProjectionAxis { get; set; } = new(0, 0, -1);
    public bool BreakOnMiss { get; set; } = true;

    public int TriangleCount => Mesh.TriangleCount;

    /// <summary>Local AABB centre — rotation and scale orbit around this, not the file origin.</summary>
    public Point3 Pivot => Mesh.Bounds.Center;

    public void Invalidate()
    {
        _dirty = true;
        _world = null;
        _bvh = null;
        _worldEdges = null;
    }

    public Point3 TransformPoint(Point3 p)
    {
        var c = Pivot;
        var q = p - c;
        var s = Scale;
        q = new Point3(q.X * s.X, q.Y * s.Y, q.Z * s.Z);
        q = Mat3.FromEulerXyzDeg(RotationDeg).Mul(q);
        return q + c + Translation;
    }

    public Point3 InverseTransformPoint(Point3 world)
    {
        var c = Pivot;
        var q = world - Translation - c;
        q = Mat3.FromEulerXyzDeg(RotationDeg).Transpose().Mul(q);
        var s = Scale;
        q = new Point3(
            s.X == 0 ? 0 : q.X / s.X,
            s.Y == 0 ? 0 : q.Y / s.Y,
            s.Z == 0 ? 0 : q.Z / s.Z);
        return q + c;
    }

    /// <summary>Uniform XY fit into the plane, Zmin on the table, identity rotation about the mesh centre.</summary>
    public void FitToPlane(double widthMm, double heightMm)
    {
        RotationDeg = Point3.Zero;
        Translation = Point3.Zero;
        Scale = new Point3(1, 1, 1);
        var b = Mesh.Bounds;
        var sx = b.Size.X < 1e-9 ? 1 : widthMm / b.Size.X;
        var sy = b.Size.Y < 1e-9 ? 1 : heightMm / b.Size.Y;
        var s = Math.Min(sx, sy);
        if (!double.IsFinite(s) || s <= 0)
            s = 1;
        Scale = new Point3(s, s, s);
        var c = Pivot;
        Translation = new Point3(
            widthMm * 0.5 - c.X,
            heightMm * 0.5 - c.Y,
            -(c.Z + s * (b.Min.Z - c.Z)));
        Invalidate();
    }

    public bool TryHitRay(Point3 origin, Point3 dir, out Point3 worldHit)
    {
        EnsureWorld();
        worldHit = default;
        if (_bvh is null)
            return false;
        if (!_bvh.TryHit(origin, dir, frontFacingOnly: false, out var hit))
            return false;
        worldHit = hit.Point;
        return true;
    }

    /// <summary>
    /// Snap local mesh points onto scene points (3 rigid, 4 least-squares).
    /// Uniform scale is optional; rotation stays around <see cref="Pivot"/>.
    /// </summary>
    public bool TryAlignFromPoints(
        IReadOnlyList<Point3> local,
        IReadOnlyList<Point3> scene,
        bool allowScale,
        out double rms)
    {
        rms = double.NaN;
        if (!Umeyama.TrySolve(local, scene, allowScale, out var solved))
            return false;

        var c = Pivot;
        var s = solved.Scale;
        RotationDeg = solved.Rotation.ToEulerXyzDeg();
        Scale = new Point3(s, s, s);
        Translation = solved.Translation - c + solved.Rotation.Mul(c) * s;
        rms = solved.Rms;
        Invalidate();
        return true;
    }

    public bool TryProject(double xMm, double yMm, double offsetAlongAxis, out Point3 hit)
    {
        EnsureWorld();
        hit = default;
        if (_bvh is null || _world is null)
            return false;

        var dir = ProjectionAxis.LengthSquared < 1e-12
            ? new Point3(0, 0, -1)
            : ProjectionAxis.Normalized();
        var far = Math.Max(10, _world.Bounds.Diagonal * 4 + Math.Abs(offsetAlongAxis));
        var origin = new Point3(xMm, yMm, 0) - dir * (far + offsetAlongAxis);
        if (!_bvh.TryHit(origin, dir, frontFacingOnly: true, out var meshHit))
            return false;
        hit = meshHit.Point;
        return true;
    }

    public IReadOnlyList<(Point3 A, Point3 B)> GetWorldEdges()
    {
        EnsureWorld();
        return _worldEdges ?? [];
    }

    public IReadOnlyList<(Point2 A, Point2 B)> GetXyEdges()
    {
        var edges = GetWorldEdges();
        var xy = new List<(Point2 A, Point2 B)>(edges.Count);
        foreach (var (a, b) in edges)
            xy.Add((a.ToPoint2(), b.ToPoint2()));
        return xy;
    }

    public Aabb3 WorldBounds
    {
        get
        {
            EnsureWorld();
            return _world?.Bounds ?? Aabb3.Empty;
        }
    }

    /// <summary>World-space triangles after T/R/S. Used by the shaded preview.</summary>
    public TriangleMesh? WorldMesh
    {
        get
        {
            EnsureWorld();
            return _world;
        }
    }

    public IReadOnlyList<(Point3 A, Point3 B)> GetBoundsEdges()
    {
        var b = WorldBounds;
        if (!double.IsFinite(b.Diagonal))
            return [];
        var n = b.Min;
        var x = b.Max;
        Point3 C(double px, double py, double pz) => new(px, py, pz);
        return
        [
            (C(n.X, n.Y, n.Z), C(x.X, n.Y, n.Z)),
            (C(x.X, n.Y, n.Z), C(x.X, x.Y, n.Z)),
            (C(x.X, x.Y, n.Z), C(n.X, x.Y, n.Z)),
            (C(n.X, x.Y, n.Z), C(n.X, n.Y, n.Z)),
            (C(n.X, n.Y, x.Z), C(x.X, n.Y, x.Z)),
            (C(x.X, n.Y, x.Z), C(x.X, x.Y, x.Z)),
            (C(x.X, x.Y, x.Z), C(n.X, x.Y, x.Z)),
            (C(n.X, x.Y, x.Z), C(n.X, n.Y, x.Z)),
            (C(n.X, n.Y, n.Z), C(n.X, n.Y, x.Z)),
            (C(x.X, n.Y, n.Z), C(x.X, n.Y, x.Z)),
            (C(x.X, x.Y, n.Z), C(x.X, x.Y, x.Z)),
            (C(n.X, x.Y, n.Z), C(n.X, x.Y, x.Z))
        ];
    }

    private void EnsureWorld()
    {
        if (!_dirty && _bvh is not null)
            return;

        var verts = new Point3[Mesh.Vertices.Length];
        for (var i = 0; i < verts.Length; i++)
            verts[i] = TransformPoint(Mesh.Vertices[i]);
        _world = new TriangleMesh(verts, Mesh.Indices);
        _bvh = _world.TriangleCount == 0 ? null : TriangleBvh.Build(_world);
        _worldEdges = BuildWorldEdges(_world);
        _dirty = false;
    }

    private static IReadOnlyList<(Point3 A, Point3 B)> BuildWorldEdges(TriangleMesh world)
    {
        var n = world.TriangleCount;
        if (n == 0)
            return [];

        // STL stores unique vertices per facet, so index-keys never merge. Take triangles
        // evenly through the file — CAD dumps are spatially ordered, and a prefix looks
        // like a broken-off chunk of the part.
        var targetTris = Math.Min(n, Math.Max(1, WireframeEdgeCap / 3));
        var step = n / (double)targetTris;
        var seen = new HashSet<long>(targetTris * 2);
        var edges = new List<(Point3 A, Point3 B)>(targetTris * 2);

        for (var k = 0; k < targetTris && edges.Count < WireframeEdgeCap; k++)
        {
            var t = (int)(k * step);
            if (t >= n)
                t = n - 1;
            var i = t * 3;
            TryAdd(world.Vertices[world.Indices[i]], world.Vertices[world.Indices[i + 1]]);
            TryAdd(world.Vertices[world.Indices[i + 1]], world.Vertices[world.Indices[i + 2]]);
            TryAdd(world.Vertices[world.Indices[i + 2]], world.Vertices[world.Indices[i]]);
        }

        return edges;

        void TryAdd(Point3 a, Point3 b)
        {
            var ha = Quantize(a);
            var hb = Quantize(b);
            if (ha == hb)
                return;
            var key = ha < hb ? ha * 31 ^ hb : hb * 31 ^ ha;
            if (!seen.Add(key))
                return;
            edges.Add((a, b));
        }
    }

    /// <summary>0.25 mm bins so coincident STL facet edges collapse in the preview.</summary>
    private static long Quantize(Point3 p)
    {
        unchecked
        {
            var x = (long)Math.Round(p.X * 4.0);
            var y = (long)Math.Round(p.Y * 4.0);
            var z = (long)Math.Round(p.Z * 4.0);
            return x * 73856093L ^ y * 19349663L ^ z * 83492791L;
        }
    }
}
