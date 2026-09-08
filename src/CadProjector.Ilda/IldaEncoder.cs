using System.Buffers.Binary;
using System.Text;
using CadProjector.Rendering;

namespace CadProjector.Ilda;

public static class IldaEncoder
{
    public static IldaFrame FromNormalizedLines(
        LinesCollection lines,
        int widthResolution,
        int heightResolution,
        byte r,
        byte g,
        byte b,
        byte alpha = 255,
        CancellationToken ct = default)
    {
        var frame = new IldaFrame();
        frame.Points.Capacity = lines.Points.Count;
        var ar = (byte)(r * alpha / 255);
        var ag = (byte)(g * alpha / 255);
        var ab = (byte)(b * alpha / 255);

        for (var i = 0; i < lines.Points.Count; i++)
        {
            if ((i & 0x3FF) == 0)
                ct.ThrowIfCancellationRequested();

            var p = lines.Points[i];
            var useDeviceColor = p.Color.R == 0 && p.Color.G == 0 && p.Color.B == 0;
            frame.Points.Add(new IldaPoint
            {
                X = (float)((p.X - 0.5) * widthResolution),
                Y = (float)((p.Y - 0.5) * heightResolution),
                Z = (float)p.Z,
                R = p.Blanked ? (byte)0 : (useDeviceColor ? ar : p.Color.R),
                G = p.Blanked ? (byte)0 : (useDeviceColor ? ag : p.Color.G),
                B = p.Blanked ? (byte)0 : (useDeviceColor ? ab : p.Color.B),
                Blanked = p.Blanked
            });
        }

        return frame;
    }

    public static byte[] ToFormat5Bytes(IldaFrame frame, int frameNumber = 0, CancellationToken ct = default)
    {
        var count = frame.Points.Count;
        var bytes = new byte[32 + count * 8];
        WriteFormat5(bytes.AsSpan(), frame, frameNumber, ct);
        return bytes;
    }

    /// <summary>Writes one ILDA format-5 frame into <paramref name="dest"/> (must be 32 + Points*8 bytes).</summary>
    public static void WriteFormat5(Span<byte> dest, IldaFrame frame, int frameNumber = 0, CancellationToken ct = default)
    {
        var count = frame.Points.Count;
        if (dest.Length < 32 + count * 8)
            throw new ArgumentException("Destination buffer is too small.", nameof(dest));

        Encoding.ASCII.GetBytes("ILDA", dest[..4]);
        dest[4] = 0;
        dest[5] = 0;
        dest[6] = 0;
        dest[7] = 5;
        WritePaddedName(dest.Slice(8, 8), frame.FrameName);
        WritePaddedName(dest.Slice(16, 8), frame.CompanyName);
        BinaryPrimitives.WriteUInt16BigEndian(dest.Slice(24, 2), (ushort)count);
        BinaryPrimitives.WriteUInt16BigEndian(dest.Slice(26, 2), (ushort)frameNumber);
        BinaryPrimitives.WriteUInt16BigEndian(dest.Slice(28, 2), (ushort)frameNumber);
        dest[30] = 0;
        dest[31] = 0;

        for (var i = 0; i < count; i++)
        {
            if ((i & 0x3FF) == 0)
                ct.ThrowIfCancellationRequested();

            var p = frame.Points[i];
            var offset = 32 + i * 8;
            var posx = (short)Math.Clamp((int)p.X, short.MinValue, short.MaxValue);
            var posy = (short)Math.Clamp((int)p.Y, short.MinValue, short.MaxValue);
            BinaryPrimitives.WriteInt16BigEndian(dest.Slice(offset, 2), posx);
            BinaryPrimitives.WriteInt16BigEndian(dest.Slice(offset + 2, 2), posy);

            byte status = 0;
            if (p.Blanked) status |= 0x40;
            if (i == count - 1) status |= 0x80;
            dest[offset + 4] = status;
            dest[offset + 5] = p.B;
            dest[offset + 6] = p.G;
            dest[offset + 7] = p.R;
        }
    }

    private static void WritePaddedName(Span<byte> dest, string name)
    {
        dest.Fill((byte)' ');
        var n = Math.Min(8, name.Length);
        for (var i = 0; i < n; i++)
            dest[i] = (byte)name[i];
    }
}
