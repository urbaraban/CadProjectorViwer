using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

public enum MeshAlignPickKind
{
    Off,
    OnMesh,
    OnTable
}

public readonly record struct MeshAlignMarker(Point3 World, bool OnMesh, int Index);
