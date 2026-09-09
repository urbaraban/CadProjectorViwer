using CadProjector.Core.Devices;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Rendering;

/// <summary>
/// Clips scene-normalized geometry to each projector's FOV rectangle and remaps
/// that rectangle to 0..1 device space (pose XY + FoV size on the scene plane).
/// Overlapping FOVs both receive the overlap, each in their own local field.
/// </summary>
public static class GeometrySplitter
{
    public static Dictionary<string, LinesCollection> SplitByFov(
        LinesCollection frame,
        IReadOnlyList<ProjectorProfile> projectors,
        double sceneWidthMm,
        double sceneHeightMm)
    {
        var result = projectors.ToDictionary(p => p.Id, _ => new LinesCollection());
        if (projectors.Count == 0 || frame.Points.Count < 2)
            return result;

        var w = Math.Max(1e-6, sceneWidthMm);
        var h = Math.Max(1e-6, sceneHeightMm);
        var segs = StrokeSegmentOps.ToSegments(frame);

        foreach (var p in projectors)
        {
            var fov = GetFovNormalized(p, w, h);
            var bag = result[p.Id];
            var fw = Math.Max(1e-6, fov.Width);
            var fh = Math.Max(1e-6, fov.Height);

            foreach (var seg in segs)
            {
                if (seg.IsBlank)
                    continue;
                if (!StrokeSegmentOps.TryClipToRect(seg, fov, out var clipped))
                    continue;

                MapToFov(clipped.P1, fov.X, fov.Y, fw, fh);
                MapToFov(clipped.P2, fov.X, fov.Y, fw, fh);
                AppendIndependent(bag, clipped);
            }
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

    private static void MapToFov(RenderPoint pt, double originX, double originY, double fw, double fh)
    {
        pt.X = (pt.X - originX) / fw;
        pt.Y = (pt.Y - originY) / fh;
    }

    /// <summary>Each clipped stroke is its own hop so disconnected pieces stay blank-separated.</summary>
    private static void AppendIndependent(LinesCollection bag, StrokeSegment seg)
    {
        var a = seg.P1.Clone();
        a.Blanked = true;
        var b = seg.P2.Clone();
        b.Blanked = false;
        bag.Points.Add(a);
        bag.Points.Add(b);
    }
}
