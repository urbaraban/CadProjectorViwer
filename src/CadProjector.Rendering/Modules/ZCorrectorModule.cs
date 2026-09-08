namespace CadProjector.Rendering.Modules;

/// <summary>Perspective-like raise/lower toward a center (legacy ZCorrector semantics on normalized points).</summary>
public sealed class ZCorrectorModule : IFrameModule
{
    public string Name => "Z Corrector";
    public bool IsEnabled { get; set; } = true;

    [ModuleParam(Label = "Depth", Min = 0.01, Increment = 0.05, Format = "0.###")]
    public double Depth { get; set; } = 1;

    [ModuleParam(Label = "Center X", Increment = 0.01, Format = "0.###")]
    public double CenterX { get; set; } = 0.5;

    [ModuleParam(Label = "Center Y", Increment = 0.01, Format = "0.###")]
    public double CenterY { get; set; } = 0.5;

    public LinesCollection Apply(LinesCollection input)
    {
        if (!IsEnabled || Depth <= 0)
            return input;

        var output = new LinesCollection();
        foreach (var p in input.Points)
        {
            var prop = Depth / Math.Max(1e-9, Math.Abs(Depth - p.Z));
            var dx = p.X - CenterX;
            var dy = p.Y - CenterY;
            output.Points.Add(new RenderPoint
            {
                X = CenterX + dx * prop,
                Y = CenterY + dy * prop,
                Z = p.Z / Depth,
                Blanked = p.Blanked,
                Mass = p.Mass,
                Color = p.Color
            });
        }
        return output;
    }
}
