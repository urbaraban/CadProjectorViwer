using CadProjector.Core.Devices;
using CadProjector.Core.Scene;
using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Tests;

public class ProjectorCameraTests
{
    [Fact]
    public void Nadir_PutsTargetCentreAtFieldCentre()
    {
        var cam = new ProjectorCamera
        {
            Position = new Point3(500, 500, 3000),
            PitchDeg = 90,
            FovHDeg = 40,
            FovVDeg = 40
        };

        Assert.True(cam.TryProjectDevice(new Point3(500, 500, 0), out var uv, out var depth));
        Assert.Equal(0.5, uv.X, 6);
        Assert.Equal(0.5, uv.Y, 6);
        Assert.Equal(3000, depth, 3);
    }

    [Fact]
    public void Nadir_KeepsTableAxesUpright()
    {
        var cam = new ProjectorCamera { Position = new Point3(0, 0, 1000), PitchDeg = 90 };
        Assert.True(cam.TryProjectDevice(new Point3(100, 0, 0), out var right, out _));
        Assert.True(cam.TryProjectDevice(new Point3(0, 100, 0), out var forward, out _));

        // +X to the right of the field, +Y up it.
        Assert.True(right.X > 0.5);
        Assert.Equal(0.5, right.Y, 6);
        Assert.True(forward.Y > 0.5);
        Assert.Equal(0.5, forward.X, 6);
    }

    [Fact]
    public void BehindTheProjector_DoesNotProject()
    {
        var cam = new ProjectorCamera { Position = new Point3(0, 0, 1000), PitchDeg = 90 };
        Assert.False(cam.TryProjectDevice(new Point3(0, 0, 2000), out _, out _));
    }

    [Fact]
    public void AimAt_CentresAnObliqueTarget()
    {
        var cam = ProjectorCamera.Aimed(new Point3(-2000, -1500, 1200), new Point3(400, 300, 60));
        Assert.True(cam.TryProjectDevice(new Point3(400, 300, 60), out var uv, out var depth));
        Assert.Equal(0.5, uv.X, 5);
        Assert.Equal(0.5, uv.Y, 5);
        Assert.True(depth > 0);
    }

    [Fact]
    public void RayThroughAPixel_ProjectsBackToIt()
    {
        var cam = ProjectorCamera.Aimed(new Point3(1200, -900, 1500), new Point3(500, 500, 0));
        const double w = 800;
        const double h = 600;
        Assert.True(cam.TryRay(300, 220, w, h, out var origin, out var dir));

        var probe = origin + dir * 2500;
        Assert.True(cam.TryProject(probe, w, h, out var screen, out _));
        Assert.Equal(300, screen.X, 3);
        Assert.Equal(220, screen.Y, 3);
    }

    [Fact]
    public void FieldEdge_LandsOnDeviceBoundary()
    {
        var cam = new ProjectorCamera { Position = Point3.Zero, PitchDeg = 90, FovHDeg = 60, FovVDeg = 60 };
        // At 1000 mm below, half the field spans 1000 * tan(30°).
        var edge = 1000 * Math.Tan(30 * Math.PI / 180.0);
        Assert.True(cam.TryProjectDevice(new Point3(edge, 0, -1000), out var uv, out _));
        Assert.Equal(1.0, uv.X, 5);
    }
}

public class ProjectorPose3DTests
{
    [Fact]
    public void ParkAbove_LiftsAndLooksDown()
    {
        var pose = new ProjectorPose3D();
        pose.ParkAbove(new Point3(500, 400, 0), 1000);
        Assert.True(pose.PositionMm.Z > 200);
        Assert.Equal(500, pose.PositionMm.X, 6);
        Assert.Equal(90, pose.PitchDeg, 6);

        var cam = pose.ToCamera();
        Assert.True(cam.TryProjectDevice(new Point3(500, 400, 0), out var uv, out _));
        Assert.Equal(0.5, uv.X, 6);
        Assert.Equal(0.5, uv.Y, 6);
    }

    [Fact]
    public void ParkAbove_ClearsTheRoofOfATallPart()
    {
        var pose = new ProjectorPose3D { PositionMm = new Point3(500, 500, 0) };
        pose.ParkAbove(new Point3(500, 500, 5_000), spanMm: 40_000, roofZMm: 10_000);
        Assert.True(pose.PositionMm.Z > 10_000);
    }

    [Fact]
    public void Snapshot_RoundTripsPoseAndField()
    {
        var p = ProjectorProfile.CreateDefault("VLT-side");
        p.Pose.PositionMm = new Point3(-1500, 200, 900);
        p.Pose.PitchDeg = 12;
        p.Pose.YawDeg = -75;
        p.Pose.RollDeg = 3;
        p.Pose.FovHDeg = 52;
        p.Pose.FovVDeg = 31;

        var restored = ProjectorProfile.CreateDefault("blank");
        DeviceSnapshot.From(p).ApplyTo(restored);

        Assert.Equal(-1500, restored.Pose.PositionMm.X, 6);
        Assert.Equal(900, restored.Pose.PositionMm.Z, 6);
        Assert.Equal(12, restored.Pose.PitchDeg, 6);
        Assert.Equal(-75, restored.Pose.YawDeg, 6);
        Assert.Equal(3, restored.Pose.RollDeg, 6);
        Assert.Equal(52, restored.Pose.FovHDeg, 6);
        Assert.Equal(31, restored.Pose.FovVDeg, 6);
    }

    [Fact]
    public void OldSnapshotWithoutAngles_ReadsAsStraightDown()
    {
        var snapshot = new DeviceSnapshot { Id = "legacy", PoseX = 500, PoseY = 500, PoseZ = 0 };
        var p = ProjectorProfile.CreateDefault("blank");
        snapshot.ApplyTo(p);
        Assert.Equal(90, p.Pose.PitchDeg, 6);
        Assert.Equal(40, p.Pose.FovHDeg, 6);
    }
}

public class MeshCoverageTests
{
    [Fact]
    public void FromAbove_TopFaceReachedBottomBackFacing()
    {
        var target = SlabTarget();
        var cam = ProjectorCamera.Aimed(new Point3(50, 25, 4000), new Point3(50, 25, 20), 60, 60);
        var coverage = MeshCoverage.Classify(target, cam);

        Assert.True(coverage.ShadowsTested);
        Assert.True(coverage.Stats.Good > 0);
        Assert.True(coverage.Stats.BackFacing > 0);
        Assert.Equal(target.WorldMesh!.TriangleCount, coverage.Facets.Length);

        var top = FacetOf(target, coverage, new Point3(50, 25, 20));
        Assert.Equal(FacetCoverage.Good, top);
    }

    [Fact]
    public void OutsideTheField_IsNotCountedAsReached()
    {
        var target = SlabTarget();
        // Narrow field aimed far away from the slab.
        var cam = ProjectorCamera.Aimed(new Point3(50, 25, 4000), new Point3(9000, 25, 20), 4, 4);
        var coverage = MeshCoverage.Classify(target, cam);

        Assert.Equal(0, coverage.Stats.Good);
        Assert.True(coverage.Stats.OutOfView > 0);
    }

    [Fact]
    public void SecondSlabInTheWay_CastsAShadow()
    {
        // Lower slab at z 0..20, upper slab at z 200..220 covering the same footprint.
        var mesh = Merge(Box(0, 0, 0, 100, 50, 20), Box(0, 0, 200, 100, 50, 220));
        var target = new MeshTarget { Mesh = mesh, SourcePath = "stack.stl" };
        var cam = ProjectorCamera.Aimed(new Point3(50, 25, 5000), new Point3(50, 25, 220), 60, 60);
        var coverage = MeshCoverage.Classify(target, cam);

        Assert.True(coverage.Stats.Shadowed > 0, "lower slab should sit in the upper slab's shadow");
        var lowerTop = FacetOf(target, coverage, new Point3(50, 25, 20));
        Assert.Equal(FacetCoverage.Shadowed, lowerTop);
    }

    [Fact]
    public void GrazingBeam_IsFlaggedNotCountedGood()
    {
        var target = SlabTarget();
        // Almost level with the top face: incidence far past the grazing limit.
        var cam = ProjectorCamera.Aimed(new Point3(50, -6000, 100), new Point3(50, 25, 20), 60, 60);
        var coverage = MeshCoverage.Classify(target, cam);

        var top = FacetOf(target, coverage, new Point3(50, 25, 20));
        Assert.Equal(FacetCoverage.Grazing, top);
        Assert.True(coverage.Stats.Grazing > 0);
    }

    private static FacetCoverage FacetOf(MeshTarget target, MeshCoverage coverage, Point3 near)
    {
        var world = target.WorldMesh!;
        var best = -1;
        var bestDist = double.PositiveInfinity;
        for (var t = 0; t < world.TriangleCount; t++)
        {
            var d = (world.TriangleCentroid(t) - near).LengthSquared;
            if (d >= bestDist)
                continue;
            bestDist = d;
            best = t;
        }

        return (FacetCoverage)coverage.Facets[best];
    }

    private static MeshTarget SlabTarget() => new()
    {
        Mesh = Box(0, 0, 0, 100, 50, 20),
        SourcePath = "slab.stl"
    };

    private static TriangleMesh Merge(TriangleMesh a, TriangleMesh b)
    {
        var verts = a.Vertices.Concat(b.Vertices).ToArray();
        var idx = a.Indices.Concat(b.Indices.Select(i => i + a.Vertices.Length)).ToArray();
        return new TriangleMesh(verts, idx);
    }

    private static TriangleMesh Box(
        double x0, double y0, double z0,
        double x1, double y1, double z1)
    {
        var p = new Point3[]
        {
            new(x0, y0, z0), new(x1, y0, z0), new(x1, y1, z0), new(x0, y1, z0),
            new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1)
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
