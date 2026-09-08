namespace CadProjector.Geometry.Primitives;

/// <summary>Axis-aligned rectangle in mm (scene units).</summary>
public readonly record struct Rect2(double X, double Y, double Width, double Height)
{
    public bool Contains(Point2 p) =>
        p.X >= X && p.Y >= Y && p.X <= X + Width && p.Y <= Y + Height;
}
