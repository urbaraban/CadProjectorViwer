using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Core.Scene;

/// <summary>Why a facet can or cannot be drawn on by one projector.</summary>
public enum FacetCoverage : byte
{
    OutOfView = 0,
    BackFacing = 1,
    Shadowed = 2,
    Grazing = 3,
    Good = 4
}

public readonly record struct MeshCoverageStats(
    int Good,
    int Grazing,
    int Shadowed,
    int OutOfView,
    int BackFacing)
{
    public int Total => Good + Grazing + Shadowed + OutOfView + BackFacing;

    /// <summary>Facets the beam reaches at a usable angle.</summary>
    public double GoodFraction => Total == 0 ? 0 : Good / (double)Total;

    /// <summary>Facets the beam reaches at all, grazing included.</summary>
    public double ReachedFraction => Total == 0 ? 0 : (Good + Grazing) / (double)Total;
}

/// <summary>
/// Per-facet reachability of an aligned STL from one projector pose: in the field,
/// facing the beam, not hidden behind the part itself, and not at a grazing angle
/// where the spot smears into a streak.
/// </summary>
public sealed class MeshCoverage
{
    /// <summary>Beyond this incidence angle the spot is too smeared to be a usable line.</summary>
    public const double GrazingLimitDeg = 65;

    /// <summary>Above this the shadow pass is skipped — one ray per facet stops being interactive.</summary>
    public const int ShadowTriangleLimit = 400_000;

    private MeshCoverage(byte[] facets, MeshCoverageStats stats, bool shadowsTested)
    {
        Facets = facets;
        Stats = stats;
        ShadowsTested = shadowsTested;
    }

    /// <summary>One <see cref="FacetCoverage"/> per world triangle, in mesh order.</summary>
    public byte[] Facets { get; }

    public MeshCoverageStats Stats { get; }

    public bool ShadowsTested { get; }

    public static MeshCoverage Classify(
        MeshTarget target,
        ProjectorCamera camera,
        double grazingLimitDeg = GrazingLimitDeg)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(camera);

        var world = target.WorldMesh;
        var n = world?.TriangleCount ?? 0;
        if (world is null || n == 0)
            return new MeshCoverage([], default, shadowsTested: false);

        var facets = new byte[n];
        var eye = camera.Eye;
        var grazingCos = Math.Cos(Math.Clamp(grazingLimitDeg, 1, 89.9) * Math.PI / 180.0);
        var testShadows = n <= ShadowTriangleLimit;
        var eps = Math.Max(1e-3, world.Bounds.Diagonal * 1e-5);

        var good = 0;
        var grazing = 0;
        var shadowed = 0;
        var outOfView = 0;
        var backFacing = 0;

        for (var t = 0; t < n; t++)
        {
            var centroid = world.TriangleCentroid(t);
            var toEye = eye - centroid;
            var dist = toEye.Length;
            if (dist < eps)
            {
                facets[t] = (byte)FacetCoverage.OutOfView;
                outOfView++;
                continue;
            }

            var dir = toEye * (1.0 / dist);
            var normal = world.TriangleNormal(t);
            var incidenceCos = Point3.Dot(normal, dir);
            if (incidenceCos <= 0)
            {
                facets[t] = (byte)FacetCoverage.BackFacing;
                backFacing++;
                continue;
            }

            if (!camera.TryProjectDevice(centroid, out var uv, out _)
                || uv.X < 0 || uv.X > 1 || uv.Y < 0 || uv.Y > 1)
            {
                facets[t] = (byte)FacetCoverage.OutOfView;
                outOfView++;
                continue;
            }

            if (testShadows
                && target.TryHitRay(centroid + normal * eps, dir, out var blocker)
                && (blocker - centroid).Length < dist - eps * 2)
            {
                facets[t] = (byte)FacetCoverage.Shadowed;
                shadowed++;
                continue;
            }

            if (incidenceCos < grazingCos)
            {
                facets[t] = (byte)FacetCoverage.Grazing;
                grazing++;
                continue;
            }

            facets[t] = (byte)FacetCoverage.Good;
            good++;
        }

        return new MeshCoverage(
            facets,
            new MeshCoverageStats(good, grazing, shadowed, outOfView, backFacing),
            testShadows);
    }
}
