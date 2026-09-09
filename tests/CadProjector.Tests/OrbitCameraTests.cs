using CadProjector.Core.Scene;
using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Tests;

public class OrbitCameraTests
{
    [Fact]
    public void Fit_ProjectsBoxCenterNearScreenCenter()
    {
        var cam = new OrbitCamera { FovDeg = 45, PitchDeg = 35, YawDeg = 40 };
        var box = Aabb3.Empty
            .Encapsulate(Point3.Zero)
            .Encapsulate(new Point3(100, 50, 20));
        cam.Fit(box);
        Assert.True(cam.TryProject(box.Center, 800, 600, out var s, out var depth));
        Assert.InRange(s.X, 300, 500);
        Assert.InRange(s.Y, 200, 400);
        Assert.True(depth > 0);
    }

    [Fact]
    public void Orbit_KeepsTargetInFront()
    {
        var cam = new OrbitCamera { Target = new Point3(10, 20, 5), Distance = 400, PitchDeg = 25 };
        cam.Orbit(90, 10);
        Assert.True(cam.TryProject(cam.Target, 400, 300, out var s, out var depth));
        Assert.InRange(s.X, 180, 220);
        Assert.InRange(s.Y, 130, 170);
        Assert.True(depth > 0);
    }
}

public class SceneStrokes3Tests
{
    [Fact]
    public void MeshHitsStayOnSurface_MissesDropToTable()
    {
        var scene = new ProjectionScene();
        scene.Target.WidthMm = 1000;
        scene.Target.HeightMm = 1000;
        scene.MeshTarget = new MeshTarget
        {
            Mesh = CubeMesh(),
            SourcePath = "cube.stl"
        };
        scene.Drawables.Add(new Drawable
        {
            Name = "Line",
            Contours =
            [
                [new Point2(20, 20), new Point2(80, 20), new Point2(400, 400)]
            ]
        });

        var strokes = SceneStrokes3.FromScene(scene);
        Assert.Equal(2, strokes.Count);
        Assert.True(strokes[0].OnSurface);
        Assert.Equal(20, strokes[0].A.Z, 3);
        Assert.Equal(20, strokes[0].B.Z, 3);
        Assert.False(strokes[1].OnSurface);
        Assert.Equal(0, strokes[1].B.Z, 3);
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
