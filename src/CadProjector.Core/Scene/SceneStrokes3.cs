using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

public readonly record struct SceneStroke3(Point3 A, Point3 B, bool OnSurface);

/// <summary>
/// 3D preview of drawings: on a mesh, hit-hit segments sit on the surface;
/// a miss drops the point to the table (Z = 0) so D7 can color it.
/// </summary>
public static class SceneStrokes3
{
    public static List<SceneStroke3> FromScene(ProjectionScene scene)
    {
        var strokes = new List<SceneStroke3>();
        var mesh = scene.MeshTarget;

        foreach (var drawable in scene.Drawables)
        {
            DrawableSpace.VisitContours(drawable, (_, contour, worldZ) =>
            {
                if (contour.Count < 2)
                    return;

                bool Cast(Point2 p, out Point3 surface)
                {
                    if (mesh is null)
                    {
                        surface = new Point3(p.X, p.Y, worldZ);
                        return true;
                    }

                    if (mesh.TryProject(p.X, p.Y, worldZ, out surface))
                        return true;
                    surface = new Point3(p.X, p.Y, 0);
                    return false;
                }

                var prev = contour[0];
                var prevHit = Cast(prev, out var prevSurf);

                for (var i = 1; i < contour.Count; i++)
                {
                    var cur = contour[i];
                    var curHit = Cast(cur, out var curSurf);
                    strokes.Add(new SceneStroke3(prevSurf, curSurf, prevHit && curHit));
                    prevHit = curHit;
                    prevSurf = curSurf;
                }
            });
        }

        return strokes;
    }
}
