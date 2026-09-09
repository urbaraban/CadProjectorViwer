using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>STL alignment in scene millimetres / degrees. Undoable as one value.</summary>
public readonly record struct MeshAlignState(Point3 Translation, Point3 RotationDeg, Point3 Scale)
{
    public static MeshAlignState Identity { get; } = new(Point3.Zero, Point3.Zero, new Point3(1, 1, 1));

    public static MeshAlignState Read(MeshTarget mesh) =>
        new(mesh.Translation, mesh.RotationDeg, mesh.Scale);

    public void ApplyTo(MeshTarget mesh)
    {
        mesh.Translation = Translation;
        mesh.RotationDeg = RotationDeg;
        mesh.Scale = Scale;
        mesh.Invalidate();
    }
}
