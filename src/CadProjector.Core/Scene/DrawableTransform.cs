using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>Everything the transform tools change on a drawable, in one undoable value.</summary>
public readonly record struct DrawableTransform(Point3 Translation, double RotationDeg, double Scale)
{
    public static DrawableTransform Read(Drawable d) => new(d.Translation, d.RotationDeg, d.Scale);

    public void ApplyTo(Drawable d)
    {
        d.Translation = Translation;
        d.RotationDeg = RotationDeg;
        d.Scale = Scale;
    }

    public DrawableTransform With(Point3 translation) => this with { Translation = translation };
}
