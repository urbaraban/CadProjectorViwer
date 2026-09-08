using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>2D projection target (virtual plane / table).</summary>
public sealed class PlaneTarget
{
    public double WidthMm { get; set; } = 1000;
    public double HeightMm { get; set; } = 1000;
    public Point3 Origin { get; set; } = Point3.Zero;
}
