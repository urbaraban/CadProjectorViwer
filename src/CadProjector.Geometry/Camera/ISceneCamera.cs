using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Camera;

/// <summary>
/// Anything the 3D viewport can look through: the free orbit camera or a projector
/// standing where the real device stands.
/// </summary>
public interface ISceneCamera
{
    Point3 Eye { get; }

    bool TryProject(Point3 world, double width, double height, out Point2 screen, out double depth);

    bool TryRay(double screenX, double screenY, double width, double height, out Point3 origin, out Point3 dir);
}
