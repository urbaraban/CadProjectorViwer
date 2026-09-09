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

    public static void Fill(
        Span<byte> bgra,
        int width,
        int height,
        int stride,
        Span<float> zbuffer,
        TriangleMesh mesh,
        Point3 eye,
        ProjectVertex project,
        byte gray = 176)
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
            var g = (byte)Math.Clamp(28 + shade * gray, 0, 255);

            if (!project(a, out var ax, out var ay, out var az))
                continue;
            if (!project(b, out var bx, out var by, out var bz))
                continue;
            if (!project(c, out var cx, out var cy, out var cz))
                continue;

            RasterTriangle(
                bgra, width, height, stride, zbuffer,
                ax, ay, az, bx, by, bz, cx, cy, cz, g);
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
        byte gray)
    {
        var area = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        if (MathF.Abs(area) < 1e-6f)
            return;

        var minX = (int)MathF.Floor(MathF.Min(ax, MathF.Min(bx, cx)));
        var maxX = (int)MathF.Ceiling(MathF.Max(ax, MathF.Max(bx, cx)));
        var minY = (int)MathF.Floor(MathF.Min(ay, MathF.Min(by, cy)));
        var maxY = (int)MathF.Ceiling(MathF.Max(ay, MathF.Max(by, cy)));
        if (minX < 0) minX = 0;
        if (minY < 0) minY = 0;
        if (maxX >= width) maxX = width - 1;
        if (maxY >= height) maxY = height - 1;
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
                bgra[p] = gray;
                bgra[p + 1] = gray;
                bgra[p + 2] = gray;
                bgra[p + 3] = 255;
            }
        }
    }
}
