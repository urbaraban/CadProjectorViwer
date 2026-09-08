using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;

namespace CadProjector.App.Controls;

public sealed class FovOverlay
{
    public string Name { get; init; } = "";
    public Rect2 BoundsMm { get; init; }
    public uint ColorArgb { get; init; } = 0xFF44AADD;
}
