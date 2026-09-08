namespace CadProjector.Rendering.Modules;

/// <summary>Densify points to match the scanner's per-frame point budget (legacy ScanRate*DotInserter).</summary>
public abstract class ScanRateModule : IFrameModule
{
    public abstract string Name { get; }
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Scan rate", Min = 1, Max = 200000, Increment = 100)]
    public uint ScanRate { get; set; } = 40000;

    [ModuleParam(Label = "FPS", Min = 1, Max = 255)]
    public byte Fps { get; set; } = 30;

    [ModuleParam(Label = "Split lit lines")]
    public bool AddPointsToRegularLines { get; set; } = true;

    [ModuleParam(Label = "Split blank lines")]
    public bool AddPointsToBlank { get; set; } = true;

    public int PointsCount => (int)(ScanRate / Math.Max(Fps, (byte)1));

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || input.Points.Count < 2)
            return input;

        var segs = StrokeSegmentOps.ToSegments(input);
        if (segs.Count <= 1)
            return input;

        var budget = PointsCount - segs.Sum(s => (int)s.T1);
        if (budget <= 1)
            return input;

        var totalLen = StrokeSegmentOps.TotalLength(segs);
        if (totalLen <= 1e-12)
            return input;

        var step = totalLen / budget;
        if (step <= 0 || double.IsNaN(step))
            return input;

        var result = new List<StrokeSegment>(segs.Count);
        foreach (var line in segs)
        {
            var shouldSplit = line.IsBlank ? AddPointsToBlank : AddPointsToRegularLines;
            if (!shouldSplit)
            {
                result.Add(line);
                continue;
            }
            result.AddRange(Split(line, Math.Max(2, (int)(line.Length / step))));
        }

        return StrokeSegmentOps.FromSegments(result);
    }

    /// <summary>Maps a uniform 0..1 position to the position the new point should take.</summary>
    protected abstract double Remap(double u, StrokeSegment line);

    private IEnumerable<StrokeSegment> Split(StrokeSegment line, int count)
    {
        var prev = line.P1.Clone();
        for (var i = 1; i < count; i++)
        {
            var next = line.PointAt(Remap((double)i / count, line));
            yield return Make(prev, next, line.IsBlank, i == 1 ? line.T1 : (byte)1, 1);
            prev = next;
        }
        yield return Make(prev, line.P2.Clone(), line.IsBlank, count == 1 ? line.T1 : (byte)1, line.T2);
    }

    private static StrokeSegment Make(RenderPoint a, RenderPoint b, bool blank, byte t1, byte t2) => new()
    {
        P1 = a,
        P2 = b,
        IsBlank = blank,
        T1 = t1,
        T2 = t2
    };
}

/// <summary>Uniform densify (legacy ScanRateSplitDotInserter).</summary>
public sealed class ScanRateSplitModule : ScanRateModule
{
    public override string Name => "Scan Rate Split";

    protected override double Remap(double u, StrokeSegment line) => u;
}

/// <summary>Point-mass weighted densify that packs points toward corners (legacy ScanRateGradientDotInserter).</summary>
public sealed class ScanRateGradientModule : ScanRateModule
{
    public override string Name => "Scan Rate Gradient";

    public ScanRateGradientModule()
    {
        ScanRate = 900;
        Fps = 1;
    }

    protected override double Remap(double u, StrokeSegment line)
    {
        var w1 = Math.Max(line.T1, (byte)1);
        var w2 = Math.Max(line.T2, (byte)1);
        if (u <= 0.5)
            return Math.Pow(u * 2.0, w1) * 0.5;
        return 0.5 + (1.0 - Math.Pow(1.0 - (u - 0.5) * 2.0, w2)) * 0.5;
    }
}
