using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;

namespace CadProjector.App.Controls;

internal static class MeshShadeBlit
{
    /// <summary>Palette for <see cref="CadProjector.Core.Scene.FacetCoverage"/>, in enum order.</summary>
    public static readonly uint[] CoverageRgb =
    [
        0x46464E, // OutOfView — barely lit
        0x37373E, // BackFacing
        0x6E3C3C, // Shadowed — behind the part itself
        0xC89638, // Grazing — beam too oblique
        0x96BE96  // Good
    ];

    public static void Draw(
        DrawingContext context,
        ref WriteableBitmap? bitmap,
        ref float[]? zbuffer,
        Size size,
        TriangleMesh mesh,
        Point3 eye,
        MeshShadeRaster.ProjectVertex project,
        byte[]? facetClass = null,
        uint[]? classRgb = null)
    {
        var w = Math.Max(1, (int)Math.Ceiling(size.Width));
        var h = Math.Max(1, (int)Math.Ceiling(size.Height));
        if (bitmap is null || bitmap.PixelSize.Width != w || bitmap.PixelSize.Height != h)
        {
            bitmap?.Dispose();
            bitmap = new WriteableBitmap(
                new PixelSize(w, h),
                new Vector(96, 96),
                PixelFormats.Bgra8888,
                AlphaFormat.Premul);
        }

        var zlen = w * h;
        if (zbuffer is null || zbuffer.Length < zlen)
            zbuffer = new float[zlen];

        using (var fb = bitmap.Lock())
        {
            unsafe
            {
                var pixels = new Span<byte>((void*)fb.Address, fb.RowBytes * h);
                MeshShadeRaster.Fill(
                    pixels, w, h, fb.RowBytes,
                    zbuffer.AsSpan(0, zlen),
                    mesh, eye, project,
                    gray: 176,
                    facetClass: facetClass ?? default,
                    classRgb: classRgb ?? default);
            }
        }

        context.DrawImage(bitmap, new Rect(0, 0, size.Width, size.Height));
    }
}
