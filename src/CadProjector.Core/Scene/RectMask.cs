using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>MVP: single rectangular mask on the scene.</summary>
public sealed class RectMask
{
    public bool IsEnabled { get; set; }
    public Rect2 Bounds { get; set; } = new(0, 0, 1000, 1000);
}
