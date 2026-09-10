using CadProjector.Geometry.Primitives;

namespace CadProjector.Geometry.Mesh;

/// <summary>
/// Software flat-shaded triangle fill with a z-buffer. STL facets do not share
/// vertices, so a wireframe always undersamples; a solid gray shade shows the
/// whole part without welding the mesh.
/// </summary>
public static class MeshShadeRaster
{
    public delegate bool ProjectVertex(Point3 world, out float x, out float y, out float depth);

    /// <param name="facetClass">Optional per-triangle index into <paramref name="classRgb"/>; empty paints flat gray.</param>
    /// <param name="classRgb">Palette as 0xRRGGBB.</param>
    public static void Fill(
        Span<byte> bgra,
        int width,
        int height,
        int stride,
        Span<float> zbuffer,
        TriangleMesh mesh,
        Point3 eye,
        ProjectVertex project,
        byte gray = 176,
        ReadOnlySpan<byte> facetClass = default,
        ReadOnlySpan<uint> classRgb = default)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(project);
        if (width < 1 || height < 1)
            return;
        if (stride < width * 4)
            throw new ArgumentOutOfRangeException(nameof(stride));
        if (bgra.Length < stride * height)
            throw new ArgumentException("Pixel buffer is smaller than stride * height.", nameof(bgra));
        if (zbuffer.Length < width * height)
            throw new ArgumentException("Z-buffer is smaller than width * height.", nameof(zbuffer));

        bgra[..(stride * height)].Clear();
        zbuffer[..(width * height)].Fill(float.MaxValue);

        var ambient = 0.22;
        var diffuse = 0.78;
        var tinted = !facetClass.IsEmpty && !classRgb.IsEmpty;
        var nTris = mesh.TriangleCount;
        for (var t = 0; t < nTris; t++)
        {
            mesh.GetTriangle(t, out var a, out var b, out var c);
            var n = Point3.Cross(b - a, c - a);
            var n2 = n.LengthSquared;
            if (n2 < 1e-20)
                continue;
            n *= 1.0 / Math.Sqrt(n2);
            var centroid = (a + b + c) * (1.0 / 3.0);
            var view = (eye - centroid).Normalized();
            var ndot = Math.Abs(Point3.Dot(n, view));
            var shade = ambient + diffuse * ndot;

            byte baseR = gray, baseG = gray, baseB = gray;
            if (tinted && t < facetClass.Length)
            {
                var slot = facetClass[t];
                var rgb = classRgb[slot < classRgb.Length ? slot : 0];
                baseR = (byte)((rgb >> 16) & 0xFF);
                baseG = (byte)((rgb >> 8) & 0xFF);
                baseB = (byte)(rgb & 0xFF);
            }

            var sr = (byte)Math.Clamp(16 + shade * baseR, 0, 255);
            var sg = (byte)Math.Clamp(16 + shade * baseG, 0, 255);
            var sb = (byte)Math.Clamp(16 + shade * baseB, 0, 255);

            if (!project(a, out var ax, out var ay, out var az) || !IsFinite(ax, ay, az))
                continue;
            if (!project(b, out var bx, out var by, out var bz) || !IsFinite(bx, by, bz))
                continue;
            if (!project(c, out var cx, out var cy, out var cz) || !IsFinite(cx, cy, cz))
                continue;

            RasterTriangle(
                bgra, width, height, stride, zbuffer,
                ax, ay, az, bx, by, bz, cx, cy, cz, sr, sg, sb);
        }
    }

    private static void RasterTriangle(
        Span<byte> bgra,
        int width,
        int height,
        int stride,
        Span<float> zbuffer,
        float ax, float ay, float az,
        float bx, float by, float bz,
        float cx, float cy, float cz,
        byte red,
        byte green,
        byte blue)
    {
        var area = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        if (!float.IsFinite(area) || MathF.Abs(area) < 1e-6f)
            return;

        // Clamp the bbox to the framebuffer *before* the int cast: a projector
        // sitting on the table sends vertices to ±1e20, and (int)1e20 throws.
        var minXf = MathF.Min(ax, MathF.Min(bx, cx));
        var maxXf = MathF.Max(ax, MathF.Max(bx, cx));
        var minYf = MathF.Min(ay, MathF.Min(by, cy));
        var maxYf = MathF.Max(ay, MathF.Max(by, cy));
        if (minXf > width - 1 || maxXf < 0 || minYf > height - 1 || maxYf < 0)
            return;

        var minX = (int)MathF.Floor(Math.Clamp(minXf, 0, width - 1));
        var maxX = (int)MathF.Ceiling(Math.Clamp(maxXf, 0, width - 1));
        var minY = (int)MathF.Floor(Math.Clamp(minYf, 0, height - 1));
        var maxY = (int)MathF.Ceiling(Math.Clamp(maxYf, 0, height - 1));
        if (minX > maxX || minY > maxY)
            return;

        var invArea = 1f / area;
        for (var y = minY; y <= maxY; y++)
        {
            var py = y + 0.5f;
            var row = y * stride;
            var zrow = y * width;
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f;
                var w0 = (cx - bx) * (py - by) - (cy - by) * (px - bx);
                var w1 = (ax - cx) * (py - cy) - (ay - cy) * (px - cx);
                var w2 = (bx - ax) * (py - ay) - (by - ay) * (px - ax);
                if (w0 * area < -1e-3f || w1 * area < -1e-3f || w2 * area < -1e-3f)
                    continue;

                var b0 = w0 * invArea;
                var b1 = w1 * invArea;
                var b2 = w2 * invArea;
                var depth = b0 * az + b1 * bz + b2 * cz;
                var zi = zrow + x;
                if (depth >= zbuffer[zi])
                    continue;
                zbuffer[zi] = depth;
                var p = row + (x << 2);
                bgra[p] = blue;
                bgra[p + 1] = green;
                bgra[p + 2] = red;
                bgra[p + 3] = 255;
            }
        }
    }

    private static bool IsFinite(float x, float y, float z)
        => float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z);
}
