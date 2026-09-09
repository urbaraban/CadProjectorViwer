using System.Buffers.Binary;
using CadProjector.Core.Project;
using CadProjector.Core.Scene;
using CadProjector.FileFormats.ProjectJson;
using CadProjector.FileFormats.Stl;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;
using CadProjector.Rendering;

namespace CadProjector.Tests;

public class StlAndMeshProjectionTests
{
    [Fact]
    public async Task AsciiAndBinary_ReadSameCube()
    {
        var ascii = WriteTemp(".stl", AsciiCube());
        var binary = WriteTemp(".stl", BinaryCube());
        try
        {
            var a = await StlImporter.LoadAsync(ascii);
            var b = await StlImporter.LoadAsync(binary);
            Assert.Equal(12, a.TriangleCount);
            Assert.Equal(12, b.TriangleCount);
            Assert.Equal(0, a.Bounds.Min.X, 3);
            Assert.Equal(100, a.Bounds.Max.X, 3);
            Assert.Equal(20, a.Bounds.Max.Z, 3);
            Assert.Equal(a.Bounds.Max.Z, b.Bounds.Max.Z, 3);
        }
        finally
        {
            File.Delete(ascii);
            File.Delete(binary);
        }
    }

    [Fact]
    public void OrthoMinusZ_HitsCubeTop()
    {
        var target = CubeTarget();
        Assert.True(target.TryProject(50, 25, 0, out var hit));
        Assert.Equal(50, hit.X, 4);
        Assert.Equal(25, hit.Y, 4);
        Assert.Equal(20, hit.Z, 4);
        Assert.False(target.TryProject(500, 500, 0, out _));
    }

    [Fact]
    public void BvhMatchesBruteForceOnCube()
    {
        var mesh = CubeMesh();
        var bvh = TriangleBvh.Build(mesh);
        var origin = new Point3(40, 10, 100);
        var dir = new Point3(0, 0, -1);
        Assert.True(bvh.TryHit(origin, dir, true, out var bvhHit));

        var best = double.PositiveInfinity;
        Point3 brute = default;
        for (var i = 0; i < mesh.TriangleCount; i++)
        {
            mesh.GetTriangle(i, out var a, out var b, out var c);
            if (!RayTriangle.Intersect(origin, dir, a, b, c, true, out var t, out _))
                continue;
            if (t >= best) continue;
            best = t;
            brute = origin + dir * t;
        }

        Assert.True(best < double.PositiveInfinity);
        Assert.Equal(brute.Z, bvhHit.Point.Z, 5);
    }

    [Fact]
    public void SceneFrame_ProjectsRectOntoCubeAndBreaksMiss()
    {
        var scene = new ProjectionScene();
        scene.Target.WidthMm = 1000;
        scene.Target.HeightMm = 1000;
        scene.MeshTarget = CubeTarget();
        scene.Drawables.Add(new Drawable
        {
            Name = "Rect",
            Contours =
            [
                [
                    new Point2(10, 10),
                    new Point2(90, 10),
                    new Point2(90, 40),
                    new Point2(10, 40),
                    new Point2(10, 10),
                    new Point2(400, 400)
                ]
            ]
        });

        var lines = SceneFrameBuilder.Build(scene, new ProjectDocument());
        Assert.Equal(5, lines.Points.Count);
        Assert.True(lines.Points[0].Blanked);
        foreach (var p in lines.Points)
            Assert.Equal(20, p.Z, 3);
        Assert.True(lines.Points[^1].X < 0.15);
    }

    [Fact]
    public async Task ProjectJson_RoundTripsMeshPathAndTransform()
    {
        var stl = WriteTemp(".stl", AsciiCube());
        var cproj = Path.Combine(Path.GetTempPath(), $"2cut-stl-{Guid.NewGuid():N}.cproj");
        try
        {
            var project = new ProjectDocument { Name = "Stl" };
            var scene = project.Scenes[0];
            scene.MeshTarget = new MeshTarget
            {
                Mesh = await StlImporter.LoadAsync(stl),
                SourcePath = stl,
                Translation = new Point3(1, 2, 3),
                Scale = new Point3(2, 2, 2)
            };

            await ProjectJsonStore.SaveAsync(project, cproj);
            var loaded = await ProjectJsonStore.LoadAsync(cproj);
            var mesh = loaded.Scenes[0].MeshTarget;
            Assert.NotNull(mesh);
            Assert.Equal(stl, mesh.SourcePath);
            Assert.Equal(12, mesh.TriangleCount);
            Assert.Equal(1, mesh.Translation.X, 5);
            Assert.Equal(2, mesh.Scale.Y, 5);
        }
        finally
        {
            File.Delete(stl);
            File.Delete(cproj);
        }
    }

    [Fact]
    public void Wireframe_SamplesAlongWholeLength_NotJustFilePrefix()
    {
        const int n = 9000;
        var verts = new Point3[n * 3];
        var idx = new int[n * 3];
        for (var i = 0; i < n; i++)
        {
            var x = i;
            verts[i * 3] = new Point3(x, 0, 0);
            verts[i * 3 + 1] = new Point3(x, 1, 0);
            verts[i * 3 + 2] = new Point3(x + 0.2, 0.5, 0);
            idx[i * 3] = i * 3;
            idx[i * 3 + 1] = i * 3 + 1;
            idx[i * 3 + 2] = i * 3 + 2;
        }

        var target = new MeshTarget { Mesh = new TriangleMesh(verts, idx), SourcePath = "strip.stl" };
        var edges = target.GetWorldEdges();
        Assert.NotEmpty(edges);
        var minX = edges.Min(e => Math.Min(e.A.X, e.B.X));
        var maxX = edges.Max(e => Math.Max(e.A.X, e.B.X));
        Assert.True(maxX - minX > 6000, $"wireframe span {maxX - minX} (expected whole 0..9000 strip)");
    }

    [Fact]
    public async Task CatiaAsciiFixture_WireframeCoversMostOfAabb()
    {
        const string path = @"c:\Users\urbar\Downloads\Telegram Desktop\Part1_3\Part1_3.stl";
        if (!File.Exists(path))
            return;

        var mesh = await StlImporter.LoadAsync(path);
        Assert.True(mesh.TriangleCount > 10_000);
        var target = new MeshTarget { Mesh = mesh, SourcePath = path };
        target.FitToPlane(1000, 1000);
        var edges = target.GetWorldEdges();
        Assert.True(edges.Count > 200);
        var box = Aabb3.Empty;
        foreach (var (a, b) in edges)
            box = box.Encapsulate(a).Encapsulate(b);
        var full = target.WorldBounds;
        Assert.True(box.Size.X > full.Size.X * 0.5, $"X {box.Size.X} vs {full.Size.X}");
        Assert.True(box.Size.Y > full.Size.Y * 0.5, $"Y {box.Size.Y} vs {full.Size.Y}");
    }

    private static MeshTarget CubeTarget() => new()
    {
        Mesh = CubeMesh(),
        SourcePath = "cube.stl"
    };

    private static TriangleMesh CubeMesh()
    {
        var faces = CubeFaces();
        var verts = new Point3[faces.Length * 3];
        var idx = new int[faces.Length * 3];
        for (var i = 0; i < faces.Length; i++)
        {
            verts[i * 3] = faces[i].A;
            verts[i * 3 + 1] = faces[i].B;
            verts[i * 3 + 2] = faces[i].C;
            idx[i * 3] = i * 3;
            idx[i * 3 + 1] = i * 3 + 1;
            idx[i * 3 + 2] = i * 3 + 2;
        }
        return new TriangleMesh(verts, idx);
    }

    private static (Point3 A, Point3 B, Point3 C)[] CubeFaces()
    {
        var p = new Point3[]
        {
            new(0, 0, 0), new(100, 0, 0), new(100, 50, 0), new(0, 50, 0),
            new(0, 0, 20), new(100, 0, 20), new(100, 50, 20), new(0, 50, 20)
        };
        return
        [
            (p[4], p[5], p[6]), (p[4], p[6], p[7]),
            (p[0], p[3], p[2]), (p[0], p[2], p[1]),
            (p[0], p[1], p[5]), (p[0], p[5], p[4]),
            (p[1], p[2], p[6]), (p[1], p[6], p[5]),
            (p[2], p[3], p[7]), (p[2], p[7], p[6]),
            (p[3], p[0], p[4]), (p[3], p[4], p[7])
        ];
    }

    private static string AsciiCube()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("solid cube");
        foreach (var (a, b, c) in CubeFaces())
        {
            sb.AppendLine("  facet normal 0 0 0");
            sb.AppendLine("    outer loop");
            sb.AppendLine($"      vertex {a.X} {a.Y} {a.Z}");
            sb.AppendLine($"      vertex {b.X} {b.Y} {b.Z}");
            sb.AppendLine($"      vertex {c.X} {c.Y} {c.Z}");
            sb.AppendLine("    endloop");
            sb.AppendLine("  endfacet");
        }
        sb.AppendLine("endsolid cube");
        return sb.ToString();
    }

    private static byte[] BinaryCube()
    {
        var faces = CubeFaces();
        var buf = new byte[84 + 50 * faces.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(80), (uint)faces.Length);
        var o = 84;
        foreach (var (a, b, c) in faces)
        {
            WriteF(buf, o + 12, a);
            WriteF(buf, o + 24, b);
            WriteF(buf, o + 36, c);
            o += 50;
        }
        return buf;
    }

    private static void WriteF(byte[] buf, int offset, Point3 p)
    {
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset), (float)p.X);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset + 4), (float)p.Y);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset + 8), (float)p.Z);
    }

    private static string WriteTemp(string ext, string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"2cut-{Guid.NewGuid():N}{ext}");
        File.WriteAllText(path, contents);
        return path;
    }

    private static string WriteTemp(string ext, byte[] contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"2cut-{Guid.NewGuid():N}{ext}");
        File.WriteAllBytes(path, contents);
        return path;
    }
}
