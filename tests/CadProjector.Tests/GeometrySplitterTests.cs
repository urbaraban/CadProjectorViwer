using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;
using CadProjector.Rendering;

namespace CadProjector.Tests;

public class GeometrySplitterTests
{
    [Fact]
    public void SingleProjector_GetsEntireFrame()
    {
        var p = ProjectorProfile.CreateDefault("A");
        p.Id = "a";
        var frame = Segment(0.1, 0.1, 0.9, 0.9);
        var bags = GeometrySplitter.SplitByFov(frame, [p], 1000, 1000);
        Assert.Single(bags);
        Assert.Equal(frame.Points.Count, bags["a"].Points.Count);
    }

    [Fact]
    public void TwoProjectors_SplitByFovOwnership()
    {
        var left = ProjectorProfile.CreateDefault("L");
        left.Id = "left";
        left.FovWidthMm = 500;
        left.FovHeightMm = 1000;
        left.Pose.PositionMm = new Point3(250, 500, 0);

        var right = ProjectorProfile.CreateDefault("R");
        right.Id = "right";
        right.FovWidthMm = 500;
        right.FovHeightMm = 1000;
        right.Pose.PositionMm = new Point3(750, 500, 0);

        // Midpoints: left segment at nx≈0.2, right at nx≈0.8 (normalized 0..1 scene)
        var frame = new LinesCollection();
        frame.Points.Add(Rp(0.1, 0.5, blanked: true));
        frame.Points.Add(Rp(0.3, 0.5, blanked: false));
        frame.Points.Add(Rp(0.7, 0.5, blanked: true));
        frame.Points.Add(Rp(0.9, 0.5, blanked: false));

        var bags = GeometrySplitter.SplitByFov(frame, [left, right], 1000, 1000);
        Assert.True(bags["left"].Points.Count >= 2);
        Assert.True(bags["right"].Points.Count >= 2);
        Assert.Contains(bags["left"].Points, p => !p.Blanked && p.X < 0.5);
        Assert.Contains(bags["right"].Points, p => !p.Blanked && p.X > 0.5);
    }

    [Fact]
    public void GetFovNormalized_CenteredOnPose()
    {
        var p = ProjectorProfile.CreateDefault("A");
        p.FovWidthMm = 200;
        p.FovHeightMm = 100;
        p.Pose.PositionMm = new Point3(500, 500, 0);
        var fov = GeometrySplitter.GetFovNormalized(p, 1000, 1000);
        Assert.InRange(fov.X, 0.39, 0.41);
        Assert.InRange(fov.Y, 0.44, 0.46);
        Assert.InRange(fov.Width, 0.19, 0.21);
        Assert.InRange(fov.Height, 0.09, 0.11);
    }

    private static LinesCollection Segment(double x0, double y0, double x1, double y1)
    {
        var lines = new LinesCollection();
        lines.Points.Add(Rp(x0, y0, blanked: true));
        lines.Points.Add(Rp(x1, y1, blanked: false));
        return lines;
    }

    private static RenderPoint Rp(double x, double y, bool blanked) => new()
    {
        X = x,
        Y = y,
        Blanked = blanked
    };
}
