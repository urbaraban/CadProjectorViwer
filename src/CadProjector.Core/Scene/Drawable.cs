using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

public sealed class Drawable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Object";
    public string? LayerName { get; set; }
    public uint? ColorArgb { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; }
    public Point3 Translation { get; set; } = Point3.Zero;
    public double RotationDeg { get; set; }
    public double Scale { get; set; } = 1;

    /// <summary>One or more polylines in local mm coordinates.</summary>
    public List<List<Point2>> Contours { get; set; } = [];
}
