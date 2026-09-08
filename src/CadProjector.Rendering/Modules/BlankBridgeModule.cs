namespace CadProjector.Rendering.Modules;

/// <summary>Insert blank jumps between distant segment ends (legacy BlankBridgeInserter).</summary>
public sealed class BlankBridgeModule : IFrameModule
{
    public string Name => "Blank Bridge";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Threshold", Min = 0, Increment = 1e-4, Format = "0.#######")]
    public double Threshold { get; set; } = 1e-4;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || input.Points.Count < 2)
            return input;

        var segs = StrokeSegmentOps.ToSegments(input);
        if (segs.Count == 0) return input;

        var result = new List<StrokeSegment>(segs.Count * 2);
        for (var i = 0; i < segs.Count; i++)
        {
            result.Add(segs[i]);
            var next = segs[(i + 1) % segs.Count];
            // only bridge sequential open path (not wrap last→first unless closed contour)
            if (i == segs.Count - 1) break;
            var dist = Math.Sqrt(StrokeSegmentOps.DistSq(segs[i].P2, next.P1));
            if (dist > Threshold)
            {
                result.Add(new StrokeSegment
                {
                    P1 = segs[i].P2.Clone(),
                    P2 = next.P1.Clone(),
                    IsBlank = true,
                    T1 = 1,
                    T2 = 1
                });
            }
        }

        return StrokeSegmentOps.FromSegments(result);
    }
}
