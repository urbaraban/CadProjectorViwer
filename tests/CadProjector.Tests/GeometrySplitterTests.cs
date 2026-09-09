using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;
using CadProjector.Rendering;

namespace CadProjector.Tests;

public class GeometrySplitterTests
{
    [Fact]
    public void FullFov_KeepsFrameInPlace()
    {
        var p = ProjectorProfile.CreateDefault("A");
        p.Id = "a";
        var frame = Segment(0.1, 0.1, 0.9, 0.9);
        var bags = GeometrySplitter.SplitByFov(frame, [p], 1000, 1000);
        Assert.Single(bags);
        var lit = bags["a"].Points.Where(pt => !pt.Blanked).ToList();
        Assert.Single(lit);
        Assert.InRange(lit[0].X, 0.89, 0.91);
        Assert.InRange(lit[0].Y, 0.89, 0.91);
    }

    [Fact]
    public void SingleProjector_ClipsAndRemapsToFov()
    {
        var p = ProjectorProfile.CreateDefault("A");
        p.Id = "a";
        p.FovWidthMm = 500;
        p.FovHeightMm = 1000;
        p.Pose.PositionMm = new Point3(250, 500, 0);

        var frame = Segment(0.1, 0.5, 0.9, 0.5);
        var bags = GeometrySplitter.SplitByFov(frame, [p], 1000, 1000);
        var pts = bags["a"].Points;
        Assert.True(pts.Count >= 2);

        var start = pts.First(pt => pt.Blanked);
        var end = pts.First(pt => !pt.Blanked);
        // Scene 0.1..0.5 (clip at FOV right edge) → local 0.2..1.0
        Assert.InRange(start.X, 0.19, 0.21);
        Assert.InRange(end.X, 0.99, 1.01);
        Assert.DoesNotContain(pts, pt => pt.X > 1.05);
    }

    [Fact]
    public void TwoProjectors_EachGetsRemappedSlice()
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

        var frame = new LinesCollection();
        frame.Points.Add(Rp(0.1, 0.5, blanked: true));
        frame.Points.Add(Rp(0.3, 0.5, blanked: false));
        frame.Points.Add(Rp(0.7, 0.5, blanked: true));
        frame.Points.Add(Rp(0.9, 0.5, blanked: false));

        var bags = GeometrySplitter.SplitByFov(frame, [left, right], 1000, 1000);
        var leftLit = bags["left"].Points.Where(pt => !pt.Blanked).ToList();
        var rightLit = bags["right"].Points.Where(pt => !pt.Blanked).ToList();
        Assert.Single(leftLit);
        Assert.Single(rightLit);
        // Left FOV 0..0.5: 0.3 → 0.6; right FOV 0.5..1: 0.9 → 0.8
        Assert.InRange(leftLit[0].X, 0.59, 0.61);
        Assert.InRange(rightLit[0].X, 0.79, 0.81);
    }

    [Fact]
    public void SpanningSegment_IsSplitAcrossFovs()
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

        var bags = GeometrySplitter.SplitByFov(Segment(0.4, 0.5, 0.6, 0.5), [left, right], 1000, 1000);
        var leftEnd = bags["left"].Points.First(pt => !pt.Blanked);
        var rightStart = bags["right"].Points.First(pt => pt.Blanked);
        Assert.InRange(leftEnd.X, 0.99, 1.01);
        Assert.InRange(rightStart.X, -0.01, 0.01);
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
