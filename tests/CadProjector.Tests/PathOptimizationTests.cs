using CadProjector.Rendering;
using CadProjector.Rendering.Modules;

namespace CadProjector.Tests;

public class PathOptimizationTests
{
    [Fact]
    public void FromSegments_KeepsDisconnectedStrokeStartsAsBlankHops()
    {
        var lines = StrokeSegmentOps.FromSegments(
        [
            Seg(0.1, 0.0, 0.9, 0.0),
            Seg(0.9, 0.1, 0.1, 0.1)
        ]);

        Assert.Equal(4, lines.Points.Count);
        Assert.True(lines.Points[0].Blanked);
        Assert.False(lines.Points[1].Blanked);
        Assert.True(lines.Points[2].Blanked);
        Assert.False(lines.Points[3].Blanked);

        Assert.Equal(0.1, lines.Points[0].X, 9);
        Assert.Equal(0.0, lines.Points[0].Y, 9);
        Assert.Equal(0.9, lines.Points[1].X, 9);
        Assert.Equal(0.0, lines.Points[1].Y, 9);
        Assert.Equal(0.9, lines.Points[2].X, 9);
        Assert.Equal(0.1, lines.Points[2].Y, 9);
        Assert.Equal(0.1, lines.Points[3].X, 9);
        Assert.Equal(0.1, lines.Points[3].Y, 9);
    }

    [Fact]
    public void FromSegments_DoesNotInsertHopOnContinuousPolyline()
    {
        var lines = StrokeSegmentOps.FromSegments(
        [
            Seg(0, 0, 1, 0),
            Seg(1, 0, 1, 1)
        ]);

        Assert.Equal(3, lines.Points.Count);
        Assert.True(lines.Points[0].Blanked);
        Assert.False(lines.Points[1].Blanked);
        Assert.False(lines.Points[2].Blanked);
    }

    [Fact]
    public void ShortestPath_HatchLinesStayHorizontalNotZigzag()
    {
        var input = new LinesCollection();
        for (var i = 0; i < 8; i++)
        {
            var y = 0.2 + i * 0.07;
            input.Points.Add(Rp(0.15, y, blanked: true));
            input.Points.Add(Rp(0.85, y, blanked: false));
        }

        var output = new ShortestPathModule().Apply(input);
        var lit = StrokeSegmentOps.ToSegments(output).Where(s => !s.IsBlank).ToList();

        Assert.Equal(8, lit.Count);
        foreach (var s in lit)
        {
            Assert.InRange(Math.Abs(s.P1.Y - s.P2.Y), 0, 1e-9);
            Assert.True(Math.Abs(s.P1.X - s.P2.X) > 0.5);
        }
    }

    [Fact]
    public void BlankBridge_InsertsEdgeJumpsAndClosesTheFrame()
    {
        var input = StrokeSegmentOps.FromSegments(
        [
            Seg(0.1, 0.2, 0.9, 0.2),
            Seg(0.9, 0.3, 0.1, 0.3),
            Seg(0.1, 0.4, 0.9, 0.4)
        ]);

        var output = new BlankBridgeModule { Threshold = 1e-4 }.Apply(input);
        var segs = StrokeSegmentOps.ToSegments(output);
        var blanks = segs.Where(s => s.IsBlank).ToList();
        var lit = segs.Where(s => !s.IsBlank).ToList();

        Assert.Equal(3, lit.Count);
        Assert.Equal(3, blanks.Count);
        Assert.Contains(blanks, s =>
            Nearly(s.P1.X, 0.9) && Nearly(s.P2.X, 0.9) && s.P1.Y < s.P2.Y);
        Assert.Contains(blanks, s =>
            Nearly(s.P1.X, 0.9) && Nearly(s.P2.X, 0.1) && s.P1.Y > s.P2.Y);
    }

    [Fact]
    public void Unduplicate_KeepsSquareWithDiagonals_DespiteBlankHopsAlongSides()
    {
        // Independent strokes as GeometrySplitter emits them: blanked start, lit end.
        // Hop from first diagonal's end (TR) to second diagonal's start (BR) retraces the right side.
        var input = new LinesCollection();
        AddStroke(input, 0.3, 0.3, 0.7, 0.3); // bottom
        AddStroke(input, 0.7, 0.3, 0.7, 0.7); // right
        AddStroke(input, 0.7, 0.7, 0.3, 0.7); // top
        AddStroke(input, 0.3, 0.7, 0.3, 0.3); // left
        AddStroke(input, 0.3, 0.3, 0.7, 0.7); // diag
        AddStroke(input, 0.7, 0.3, 0.3, 0.7); // diag

        var lit = StrokeSegmentOps.ToSegments(new UnduplicateModule().Apply(input))
            .Where(s => !s.IsBlank)
            .ToList();

        Assert.Equal(6, lit.Count);
        Assert.Equal(4, lit.Count(s =>
            Math.Abs(s.P1.Y - s.P2.Y) < 1e-9 || Math.Abs(s.P1.X - s.P2.X) < 1e-9));
        Assert.Equal(2, lit.Count(s =>
            Math.Abs(s.P1.Y - s.P2.Y) > 1e-9 && Math.Abs(s.P1.X - s.P2.X) > 1e-9));
    }

    [Fact]
    public void Unduplicate_StillRemovesOverlappingLitDuplicates()
    {
        var input = new LinesCollection();
        AddStroke(input, 0.1, 0.5, 0.9, 0.5);
        AddStroke(input, 0.1, 0.5, 0.9, 0.5);

        var lit = StrokeSegmentOps.ToSegments(new UnduplicateModule().Apply(input))
            .Where(s => !s.IsBlank)
            .ToList();

        Assert.Single(lit);
        Assert.InRange(lit[0].Length, 0.79, 0.81);
    }

    private static void AddStroke(LinesCollection lines, double x1, double y1, double x2, double y2)
    {
        lines.Points.Add(Rp(x1, y1, blanked: true));
        lines.Points.Add(Rp(x2, y2, blanked: false));
    }

    private static bool Nearly(double a, double b) => Math.Abs(a - b) < 1e-9;

    private static StrokeSegment Seg(double x1, double y1, double x2, double y2) => new()
    {
        P1 = Rp(x1, y1, blanked: true),
        P2 = Rp(x2, y2, blanked: false),
        IsBlank = false,
        T1 = 1,
        T2 = 1
    };

    private static RenderPoint Rp(double x, double y, bool blanked) => new()
    {
        X = x,
        Y = y,
        Blanked = blanked
    };
}
