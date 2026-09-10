using CadProjector.Core.Scene;
using CadProjector.FileFormats.Dxf;
using CadProjector.Geometry.Primitives;
using IxMilia.Dxf;

namespace CadProjector.Tests;

public class DrawableGroupTests
{
    [Fact]
    public void GroupThenUngroup_RestoresWorldPositions()
    {
        var a = new Drawable
        {
            Name = "A",
            Translation = new Point3(10, 20, 1),
            Contours = [[new Point2(0, 0), new Point2(10, 0)]]
        };
        var b = new Drawable
        {
            Name = "B",
            Translation = new Point3(40, 20, 0),
            RotationDeg = 90,
            Contours = [[new Point2(0, 0), new Point2(5, 0)]]
        };

        var beforeA = WorldFirst(a);
        var beforeB = WorldFirst(b);

        var group = Drawable.CreateGroup([a, b], "G");
        var promoted = group.Ungroup();

        Assert.Equal(2, promoted.Count);
        Assert.Equal(beforeA.X, WorldFirst(promoted[0]).X, 6);
        Assert.Equal(beforeA.Y, WorldFirst(promoted[0]).Y, 6);
        Assert.Equal(beforeB.X, WorldFirst(promoted[1]).X, 6);
        Assert.Equal(beforeB.Y, WorldFirst(promoted[1]).Y, 6);
        Assert.Equal(1, promoted[0].Translation.Z, 6);
    }

    private static Point2 WorldFirst(Drawable d)
    {
        Point2? hit = null;
        DrawableSpace.VisitContours(d, (_, contour, _) =>
        {
            hit ??= contour[0];
        });
        Assert.NotNull(hit);
        return hit!.Value;
    }
}

public class DxfUnitScaleTests
{
    [Fact]
    public void Auto_UsesFileHeader()
    {
        Assert.Equal(25.4, DxfUnitScale.ToMm(DxfUnitPreference.Auto, DxfUnits.Inches), 6);
        Assert.Equal(1.0, DxfUnitScale.ToMm(DxfUnitPreference.Auto, DxfUnits.Millimeters), 6);
    }

    [Fact]
    public void Override_IgnoresFileHeader()
    {
        Assert.Equal(1.0, DxfUnitScale.ToMm(DxfUnitPreference.Millimeters, DxfUnits.Inches), 6);
        Assert.Equal(10.0, DxfUnitScale.ToMm(DxfUnitPreference.Centimeters, DxfUnits.Feet), 6);
        Assert.Equal(1000.0, DxfUnitScale.ToMm(DxfUnitPreference.Meters, DxfUnits.Millimeters), 6);
        Assert.Equal(304.8, DxfUnitScale.ToMm(DxfUnitPreference.Feet, DxfUnits.Millimeters), 6);
    }
}
