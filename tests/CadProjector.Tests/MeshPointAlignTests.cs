using CadProjector.Core.Scene;
using CadProjector.Geometry.Linear;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;
using CadProjector.Geometry.Transform;

namespace CadProjector.Tests;

public class MeshPointAlignTests
{
    [Fact]
    public void RotateAroundCenter_KeepsCentroid()
    {
        var mesh = CubeMesh();
        var target = new MeshTarget { Mesh = mesh, SourcePath = "cube.stl" };
        var c = target.Pivot;
        target.RotationDeg = new Point3(0, 0, 90);
        var moved = target.TransformPoint(c);
        Assert.Equal(c.X, moved.X, 5);
        Assert.Equal(c.Y, moved.Y, 5);
        Assert.Equal(c.Z, moved.Z, 5);

        var corner = target.TransformPoint(new Point3(100, 25, 10));
        Assert.Equal(50, corner.X, 4);
        Assert.Equal(75, corner.Y, 4);
        Assert.Equal(10, corner.Z, 4);
    }

    [Fact]
    public void Inverse_RoundTripsThroughTrs()
    {
        var target = new MeshTarget
        {
            Mesh = CubeMesh(),
            SourcePath = "cube.stl",
            Translation = new Point3(12, -4, 8),
            RotationDeg = new Point3(20, -15, 40),
            Scale = new Point3(0.5, 0.5, 0.5)
        };
        var p = new Point3(80, 10, 5);
        var world = target.TransformPoint(p);
        var back = target.InverseTransformPoint(world);
        Assert.Equal(p.X, back.X, 5);
        Assert.Equal(p.Y, back.Y, 5);
        Assert.Equal(p.Z, back.Z, 5);
    }

    [Fact]
    public void Umeyama_RecoversScaleRotationTranslation()
    {
        var src = new Point3[]
        {
            new(10, 0, 0),
            new(0, 20, 0),
            new(0, 0, 8),
            new(6, 7, 3)
        };
        var r = Mat3.FromEulerXyzDeg(new Point3(0, 0, 35));
        const double s = 2.5;
        var t = new Point3(40, -12, 5);
        var dst = src.Select(p => r.Mul(p) * s + t).ToArray();

        Assert.True(Umeyama.TrySolve(src, dst, allowScale: true, out var solved));
        Assert.Equal(s, solved.Scale, 4);
        Assert.Equal(t.X, solved.Translation.X, 3);
        Assert.Equal(t.Y, solved.Translation.Y, 3);
        Assert.Equal(t.Z, solved.Translation.Z, 3);
        Assert.True(solved.Rms < 1e-6);
    }

    [Fact]
    public void ThreePoints_AlignCubeOntoTable()
    {
        var target = new MeshTarget { Mesh = CubeMesh(), SourcePath = "cube.stl" };
        var local = new Point3[]
        {
            new(0, 0, 20),
            new(100, 0, 20),
            new(0, 50, 20)
        };
        var scene = new Point3[]
        {
            new(200, 200, 0),
            new(400, 200, 0),
            new(200, 300, 0)
        };

        Assert.True(target.TryAlignFromPoints(local, scene, allowScale: true, out var rms));
        Assert.True(rms < 1e-4);
        var a = target.TransformPoint(local[0]);
        var b = target.TransformPoint(local[1]);
        Assert.Equal(200, a.X, 3);
        Assert.Equal(200, a.Y, 3);
        Assert.Equal(0, a.Z, 3);
        Assert.Equal(400, b.X, 3);
        Assert.Equal(200, b.Y, 3);
    }

    [Fact]
    public void FourPoints_LeastSquaresBeatsCollinearFail()
    {
        var src = new Point3[]
        {
            new(0, 0, 0),
            new(10, 0, 0),
            new(0, 10, 0),
            new(10, 10, 0)
        };
        var dst = new Point3[]
        {
            new(0, 0, 0),
            new(10, 0, 0),
            new(0, 10, 0),
            new(10.2, 9.8, 0)
        };
        Assert.True(Umeyama.TrySolve(src, dst, allowScale: false, out var solved));
        Assert.True(solved.Rms < 0.2);
        Assert.True(solved.Rms > 0);
    }

    private static TriangleMesh CubeMesh()
    {
        var p = new Point3[]
        {
            new(0, 0, 0), new(100, 0, 0), new(100, 50, 0), new(0, 50, 0),
            new(0, 0, 20), new(100, 0, 20), new(100, 50, 20), new(0, 50, 20)
        };
        (Point3 A, Point3 B, Point3 C)[] faces =
        [
            (p[4], p[5], p[6]), (p[4], p[6], p[7]),
            (p[0], p[3], p[2]), (p[0], p[2], p[1]),
            (p[0], p[1], p[5]), (p[0], p[5], p[4]),
            (p[1], p[2], p[6]), (p[1], p[6], p[5]),
            (p[2], p[3], p[7]), (p[2], p[7], p[6]),
            (p[3], p[0], p[4]), (p[3], p[4], p[7])
        ];
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
}
