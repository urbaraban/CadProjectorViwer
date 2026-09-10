using CadProjector.Core.Scene;
using CadProjector.FileFormats.Stl;
using CadProjector.Geometry.Camera;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Tests;

public class MeshShadeRasterTests
{
    [Fact]
    public void Fill_PaintsEveryTriangle_NotAPrefix()
    {
        var verts = new Point3[]
        {
            new(1, 1, 0), new(4, 1, 0), new(4, 4, 0), new(1, 4, 0),
            new(80, 80, 0), new(84, 80, 0), new(84, 84, 0), new(80, 84, 0)
        };
        var mesh = new TriangleMesh(verts, [0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7]);
        const int w = 100;
        const int h = 100;
        var pixels = new byte[w * h * 4];
        var z = new float[w * h];
        MeshShadeRaster.Fill(
            pixels, w, h, w * 4, z, mesh, new Point3(40, 40, 200),
            (Point3 p, out float x, out float y, out float d) =>
            {
                x = (float)p.X;
                y = (float)p.Y;
                d = (float)(-p.Z);
                return true;
            });

        Assert.Equal(255, pixels[PixelOffset(w, 2, 2) + 3]);
        Assert.Equal(255, pixels[PixelOffset(w, 82, 82) + 3]);
        Assert.Equal(0, pixels[PixelOffset(w, 40, 40) + 3]);
    }

    [Fact]
    public void Fill_CloserTriangleWinsZBuffer()
    {
        var verts = new Point3[]
        {
            new(0, 0, 0), new(10, 0, 0), new(0, 10, 0),
            new(0, 0, 8), new(10, 0, 8), new(0, 10, 8)
        };
        var mesh = new TriangleMesh(verts, [0, 1, 2, 3, 4, 5]);
        const int w = 16;
        const int h = 16;
        var pixels = new byte[w * h * 4];
        var z = new float[w * h];
        MeshShadeRaster.Fill(
            pixels, w, h, w * 4, z, mesh, new Point3(3, 3, 100),
            (Point3 p, out float x, out float y, out float d) =>
            {
                x = (float)p.X;
                y = (float)p.Y;
                d = (float)(-p.Z);
                return true;
            });

        Assert.Equal(-8f, z[2 * w + 2], 2);
    }

    [Fact]
    public void Camera_ShadesFrontFacingQuad()
    {
        var mesh = new TriangleMesh(
            [new(0, 0, 0), new(20, 0, 0), new(20, 20, 0), new(0, 20, 0)],
            [0, 1, 2, 0, 2, 3]);
        var cam = new OrbitCamera
        {
            Target = new Point3(10, 10, 0),
            Distance = 80,
            PitchDeg = 90,
            YawDeg = 0,
            FovDeg = 45
        };
        const int w = 64;
        const int h = 64;
        var pixels = new byte[w * h * 4];
        var z = new float[w * h];
        MeshShadeRaster.Fill(
            pixels, w, h, w * 4, z, mesh, cam.Eye,
            (Point3 p, out float x, out float y, out float d) =>
            {
                if (!cam.TryProject(p, w, h, out var s, out var depth))
                {
                    x = y = d = 0;
                    return false;
                }
                x = (float)s.X;
                y = (float)s.Y;
                d = (float)depth;
                return true;
            });

        var opaque = 0;
        for (var i = 3; i < pixels.Length; i += 4)
            if (pixels[i] == 255)
                opaque++;
        Assert.True(opaque > 80, $"opaque {opaque}");
        Assert.Equal(255, pixels[PixelOffset(w, w / 2, h / 2) + 3]);
    }

    [Fact]
    public async Task CatiaAsciiFixture_ShadeCoversAabbNotAChunk()
    {
        const string path = @"c:\Users\urbar\Downloads\Telegram Desktop\Part1_3\Part1_3.stl";
        if (!File.Exists(path))
            return;

        var mesh = await StlImporter.LoadAsync(path);
        var target = new MeshTarget { Mesh = mesh, SourcePath = path };
        target.FitToPlane(1000, 1000);
        var world = target.WorldMesh!;
        const int w = 160;
        const int h = 160;
        var pixels = new byte[w * h * 4];
        var z = new float[w * h];
        var box = target.WorldBounds;
        MeshShadeRaster.Fill(
            pixels, w, h, w * 4, z, world, new Point3(box.Center.X, box.Center.Y, 1e6),
            (Point3 p, out float x, out float y, out float d) =>
            {
                x = (float)((p.X - box.Min.X) / box.Size.X * (w - 1));
                y = (float)((box.Max.Y - p.Y) / box.Size.Y * (h - 1));
                d = (float)(-p.Z);
                return true;
            });

        var opaque = 0;
        var minX = w;
        var maxX = 0;
        var minY = h;
        var maxY = 0;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            if (pixels[PixelOffset(w, x, y) + 3] != 255)
                continue;
            opaque++;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        Assert.True(opaque > 400, $"opaque {opaque}");
        Assert.True(maxX - minX > w * 0.6, $"spanX {maxX - minX}");
        Assert.True(maxY - minY > h * 0.4, $"spanY {maxY - minY}");
    }

    [Fact]
    public void Fill_HugeProjectedCoords_DoNotThrow()
    {
        var mesh = new TriangleMesh(
            [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)],
            [0, 1, 2]);
        const int w = 32;
        const int h = 32;
        var pixels = new byte[w * h * 4];
        var z = new float[w * h];

        MeshShadeRaster.Fill(
            pixels, w, h, w * 4, z, mesh, new Point3(0, 0, 1),
            (Point3 p, out float x, out float y, out float d) =>
            {
                x = p.X == 0 ? 8 : 1e20f;
                y = 8;
                d = 1;
                return true;
            });
    }

    [Fact]
    public void Fill_FromProjectorOnTheTable_DoesNotThrow()
    {
        var mesh = new TriangleMesh(
            [
                new(-20_000, -8_000, 0), new(20_000, -8_000, 0), new(20_000, 8_000, 50),
                new(-20_000, -8_000, 0), new(20_000, 8_000, 50), new(-20_000, 8_000, 50)
            ],
            [0, 1, 2, 3, 4, 5]);
        var cam = new ProjectorCamera
        {
            Position = new Point3(500, 500, 0),
            PitchDeg = 90,
            FovHDeg = 40,
            FovVDeg = 40
        };
        const int w = 64;
        const int h = 64;
        var pixels = new byte[w * h * 4];
        var z = new float[w * h];
        MeshShadeRaster.Fill(
            pixels, w, h, w * 4, z, mesh, cam.Eye,
            (Point3 p, out float x, out float y, out float d) =>
            {
                if (!cam.TryProject(p, w, h, out var s, out var depth))
                {
                    x = y = d = 0;
                    return false;
                }
                x = (float)s.X;
                y = (float)s.Y;
                d = (float)depth;
                return true;
            });
    }

    private static int PixelOffset(int width, int x, int y) => (y * width + x) * 4;
}
