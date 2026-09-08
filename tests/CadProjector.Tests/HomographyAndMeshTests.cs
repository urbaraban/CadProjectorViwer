using CadProjector.Core.Devices;
using CadProjector.Geometry.Math2D;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Tests;

public class Homography2DTests
{
    [Fact]
    public void FromUnitSquare_MapsCorners()
    {
        var tl = new Point2(0.1, 0.2);
        var tr = new Point2(0.9, 0.15);
        var br = new Point2(0.85, 0.9);
        var bl = new Point2(0.05, 0.8);
        var h = Homography2D.FromUnitSquare(tl, tr, br, bl);

        AssertNear(tl, h.Transform(new Point2(0, 0)));
        AssertNear(tr, h.Transform(new Point2(1, 0)));
        AssertNear(br, h.Transform(new Point2(1, 1)));
        AssertNear(bl, h.Transform(new Point2(0, 1)));
    }

    [Fact]
    public void IdentityCorners_KeepInteriorOnGrid()
    {
        var h = Homography2D.FromUnitSquare(
            new Point2(0, 0), new Point2(1, 0), new Point2(1, 1), new Point2(0, 1));
        AssertNear(new Point2(0.5, 0.5), h.Transform(new Point2(0.5, 0.5)));
    }

    private static void AssertNear(Point2 expected, Point2 actual, double eps = 1e-9)
    {
        Assert.InRange(actual.X, expected.X - eps, expected.X + eps);
        Assert.InRange(actual.Y, expected.Y - eps, expected.Y + eps);
    }
}

public class CalibrationMeshTests
{
    [Fact]
    public void FullMorph_FillsInteriorFromCorners()
    {
        var mesh = new CalibrationMesh { Morph = MeshMorphType.Full };
        mesh.ResetIdentity(2, 2);
        mesh.CornerTL = new Point2(0.05, 0.05);
        mesh.CornerTR = new Point2(0.95, 0.1);
        mesh.CornerBR = new Point2(0.9, 0.95);
        mesh.CornerBL = new Point2(0.1, 0.9);
        mesh.CalculateMorph();

        var mid = mesh.GetPoint(1, 1);
        Assert.InRange(mid.X, 0.3, 0.7);
        Assert.InRange(mid.Y, 0.3, 0.7);
        Assert.NotEqual(new Point2(0.5, 0.5), mid);
    }

    [Fact]
    public void SingleMorph_DoesNotMoveInterior()
    {
        var mesh = new CalibrationMesh { Morph = MeshMorphType.Single };
        mesh.ResetIdentity(2, 2);
        mesh.SetPoint(1, 1, new Point2(0.42, 0.37));
        mesh.CornerTL = new Point2(0.1, 0.1);
        mesh.CalculateMorph();
        Assert.Equal(new Point2(0.42, 0.37), mesh.GetPoint(1, 1));
    }

    [Fact]
    public void Mirror_FlipsY()
    {
        var mesh = new CalibrationMesh();
        mesh.ResetIdentity(1, 1);
        mesh.CornerTL = new Point2(0.2, 0.3);
        mesh.Mirror();
        Assert.Equal(new Point2(0.2, 0.7), mesh.CornerTL);
    }

    [Fact]
    public void Apply_Disabled_ReturnsInput()
    {
        var mesh = new CalibrationMesh { IsEnabled = false };
        mesh.ResetIdentity(1, 1);
        mesh.CornerTL = new Point2(0.2, 0.2);
        Assert.Equal(new Point2(0.5, 0.5), mesh.Apply(new Point2(0.5, 0.5)));
    }

    [Fact]
    public void Clone_CopiesMorphAndPoints()
    {
        var mesh = new CalibrationMesh { Morph = MeshMorphType.Horizontal, IsEnabled = true };
        mesh.ResetIdentity(2, 1);
        mesh.SetPoint(1, 0, new Point2(0.7, 0.2));
        var copy = mesh.Clone();
        Assert.Equal(MeshMorphType.Horizontal, copy.Morph);
        Assert.Equal(new Point2(0.7, 0.2), copy.GetPoint(1, 0));
        copy.SetPoint(1, 0, new Point2(0, 0));
        Assert.Equal(new Point2(0.7, 0.2), mesh.GetPoint(1, 0));
    }
}
