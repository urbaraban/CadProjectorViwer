using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering;

public sealed class RenderPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public bool Blanked { get; set; }
    /// <summary>Corner dwell / point-mass weight (legacy T1/T2), used by ScanRate densify.</summary>
    public byte Mass { get; set; } = 1;
    public RgbColor Color { get; set; } = RgbColor.Red;

    public RenderPoint Clone() => new()
    {
        X = X, Y = Y, Z = Z, Blanked = Blanked, Mass = Mass, Color = Color
    };
}
