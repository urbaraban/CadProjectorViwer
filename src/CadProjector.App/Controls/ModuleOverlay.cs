using CadProjector.Geometry.Primitives;
using CadProjector.Rendering;
using CadProjector.Rendering.Modules;

namespace CadProjector.App.Controls;

/// <summary>
/// Geometry a module draws on the table. The module works in its device's normalized 0..1
/// space, so the overlay carries the device's field of view to place it in scene millimetres.
/// </summary>
public sealed class ModuleOverlay
{
    public required string ModuleId { get; init; }
    public required string Name { get; init; }
    public required Rect2 BoundsMm { get; init; }
    public LinesCollection Geometry { get; init; } = new();
    public IReadOnlyList<ModuleAnchor> Anchors { get; init; } = [];
    public uint ColorArgb { get; init; } = 0xFFFFC83C;
    public bool IsSelected { get; init; }
    public int SelectedAnchor { get; init; } = -1;

    public Point2 ToWorld(Point2 unit) => new(
        BoundsMm.X + unit.X * BoundsMm.Width,
        BoundsMm.Y + unit.Y * BoundsMm.Height);

    public Point2 ToUnit(Point2 world) => new(
        (world.X - BoundsMm.X) / Math.Max(1e-6, BoundsMm.Width),
        (world.Y - BoundsMm.Y) / Math.Max(1e-6, BoundsMm.Height));
}
