using System.Text;

namespace CadProjector.Automation;

/// <summary>Sequential reader over a UDP/TCP binary packet (legacy ByteParser).</summary>
public sealed class BytePacketReader
{
    private readonly byte[] _b;
    public int Position { get; private set; }

    public BytePacketReader(byte[] buffer) => _b = buffer;

    public int Remaining => _b.Length - Position;

    public byte GetByte()
    {
        Ensure(1);
        return _b[Position++];
    }

    public short GetShort()
    {
        Ensure(2);
        var v = BitConverter.ToInt16(_b, Position);
        Position += 2;
        return v;
    }

    public int GetInt()
    {
        Ensure(4);
        var v = BitConverter.ToInt32(_b, Position);
        Position += 4;
        return v;
    }

    public double GetDouble()
    {
        Ensure(8);
        var v = BitConverter.ToDouble(_b, Position);
        Position += 8;
        return v;
    }

    public byte[] GetBytes(int length)
    {
        Ensure(length);
        var slice = new byte[length];
        Buffer.BlockCopy(_b, Position, slice, 0, length);
        Position += length;
        return slice;
    }

    /// <summary>Legacy GetString: one byte per char (not UTF-8).</summary>
    public string GetString(int length)
    {
        Ensure(length);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append((char)_b[Position++]);
        return sb.ToString();
    }

    public string GetAscii(int length) =>
        Encoding.ASCII.GetString(GetBytes(length));

    public string GetEncoding1251(int length)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251).GetString(GetBytes(length));
    }

    private void Ensure(int n)
    {
        if (Position + n > _b.Length)
            throw new InvalidDataException($"Packet truncated at {Position}, need {n} more bytes.");
    }
}
