namespace CadProjector.Geometry.Primitives;

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor Red => new(255, 0, 0);
    public static RgbColor FromArgb(uint argb) =>
        new((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
}
