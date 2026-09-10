using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>
/// Scene crop rectangle in millimetres. Clips objects before the FOV split —
/// not a per-projector field mask.
/// </summary>
public sealed class RectMask
{
    public bool IsEnabled { get; set; }
    public Rect2 Bounds { get; set; } = new(0, 0, 1000, 1000);
}
