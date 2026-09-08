using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering;

/// <summary>Assigns geometry to projectors by FOV rectangle on the scene plane (pose XY + FoV size).</summary>
public static class GeometrySplitter
{
    public static Dictionary<string, LinesCollection> SplitByFov(
        LinesCollection frame,
        IReadOnlyList<ProjectorProfile> projectors,
        double sceneWidthMm,
        double sceneHeightMm)
    {
        var result = projectors.ToDictionary(p => p.Id, _ => new LinesCollection());
        if (projectors.Count == 0)
            return result;

        if (projectors.Count == 1)
        {
            result[projectors[0].Id] = frame;
            return result;
        }

        var w = Math.Max(1e-6, sceneWidthMm);
        var h = Math.Max(1e-6, sceneHeightMm);

        for (var i = 0; i < frame.Points.Count - 1; i++)
        {
            var a = frame.Points[i];
            var b = frame.Points[i + 1];
            if (b.Blanked)
                continue;

            var midX = (a.X + b.X) * 0.5;
            var midY = (a.Y + b.Y) * 0.5;
            var owner = FindOwner(projectors, midX, midY, w, h) ?? projectors[0];
            var bag = result[owner.Id];
            bag.Points.Add(new RenderPoint
            {
                X = a.X, Y = a.Y, Z = a.Z, Blanked = true, Mass = a.Mass, Color = a.Color
            });
            bag.Points.Add(new RenderPoint
            {
                X = b.X, Y = b.Y, Z = b.Z, Blanked = false, Mass = b.Mass, Color = b.Color
            });
        }

        return result;
    }

    public static Rect2 GetFovNormalized(ProjectorProfile p, double sceneWidthMm, double sceneHeightMm)
    {
        var w = Math.Max(1e-6, sceneWidthMm);
        var h = Math.Max(1e-6, sceneHeightMm);
        var cx = p.Pose.PositionMm.X / w;
        var cy = p.Pose.PositionMm.Y / h;
        var fw = p.FovWidthMm / w;
        var fh = p.FovHeightMm / h;
        return new Rect2(cx - fw * 0.5, cy - fh * 0.5, fw, fh);
    }

    private static ProjectorProfile? FindOwner(
        IReadOnlyList<ProjectorProfile> projectors,
        double nx,
        double ny,
        double sceneW,
        double sceneH)
    {
        ProjectorProfile? best = null;
        var bestDist = double.MaxValue;
        foreach (var p in projectors)
        {
            var fov = GetFovNormalized(p, sceneW, sceneH);
            if (!fov.Contains(new Point2(nx, ny)))
                continue;

            var cx = p.Pose.PositionMm.X / sceneW;
            var cy = p.Pose.PositionMm.Y / sceneH;
            var d = (nx - cx) * (nx - cx) + (ny - cy) * (ny - cy);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }

        // Fallback: nearest projector center if outside all FOVs
        if (best is not null)
            return best;

        foreach (var p in projectors)
        {
            var cx = p.Pose.PositionMm.X / sceneW;
            var cy = p.Pose.PositionMm.Y / sceneH;
            var d = (nx - cx) * (nx - cx) + (ny - cy) * (ny - cy);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }
}
