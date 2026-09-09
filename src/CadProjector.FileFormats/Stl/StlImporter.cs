using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using CadProjector.FileFormats;
using CadProjector.Geometry.Mesh;
using CadProjector.Geometry.Primitives;
using CadProjector.Logging;

namespace CadProjector.FileFormats.Stl;

/// <summary>ASCII and binary STL (little-endian) → <see cref="TriangleMesh"/>. Units as stored (mm assumed).</summary>
public static class StlImporter
{
    public static async Task<TriangleMesh> LoadAsync(
        string path,
        CancellationToken cancellationToken = default,
        IProgress<ImportProgress>? progress = null)
    {
        await using var fs = File.OpenRead(path);
        return await LoadAsync(fs, Path.GetFileName(path), cancellationToken, progress);
    }

    public static async Task<TriangleMesh> LoadAsync(
        Stream stream,
        string name = "stl",
        CancellationToken cancellationToken = default,
        IProgress<ImportProgress>? progress = null)
    {
        progress?.Report(new ImportProgress($"Reading {name}…", 0.05));
        if (!stream.CanSeek)
        {
            var copy = new MemoryStream();
            await stream.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;
            stream = copy;
        }

        var mesh = IsBinary(stream) ? ReadBinary(stream, cancellationToken, progress) : ReadAscii(stream);
        if (mesh.TriangleCount == 0)
            throw new InvalidDataException($"STL '{name}' has no triangles.");
        if (mesh.TriangleCount > MeshTargetWarnLimit)
            CadLog.Warn($"STL {name}: {mesh.TriangleCount} triangles (>{MeshTargetWarnLimit}) — interaction may drop");
        CadLog.Good($"STL {name}: {mesh.TriangleCount} triangles");
        progress?.Report(new ImportProgress("STL loaded", 1));
        return mesh;
    }

    private const int MeshTargetWarnLimit = 500_000;

    internal static bool IsBinary(Stream stream)
    {
        stream.Position = 0;
        if (stream.Length < 84)
            return false;
        Span<byte> header = stackalloc byte[84];
        if (stream.Read(header) < 84)
            return false;
        var count = BinaryPrimitives.ReadUInt32LittleEndian(header[80..]);
        var expected = 84L + 50L * count;
        stream.Position = 0;
        if (expected == stream.Length && count > 0)
            return true;

        // Binary files sometimes start with "solid" in the 80-byte header; length is the tie-breaker.
        var asciiHint = Encoding.ASCII.GetString(header[..5]).Equals("solid", StringComparison.OrdinalIgnoreCase);
        return !asciiHint && expected == stream.Length;
    }

    private static TriangleMesh ReadBinary(
        Stream stream,
        CancellationToken cancellationToken,
        IProgress<ImportProgress>? progress)
    {
        stream.Position = 80;
        Span<byte> countBuf = stackalloc byte[4];
        if (stream.Read(countBuf) < 4)
            throw new InvalidDataException("Truncated STL header.");
        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(countBuf);
        if (count < 0 || count > 20_000_000)
            throw new InvalidDataException($"Unreasonable STL triangle count: {count}.");

        var vertices = new Point3[count * 3];
        var indices = new int[count * 3];
        var tri = new byte[50];
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stream.Read(tri) < 50)
                throw new InvalidDataException("Truncated STL triangle.");
            var baseV = i * 3;
            vertices[baseV] = ReadVertex(tri, 12);
            vertices[baseV + 1] = ReadVertex(tri, 24);
            vertices[baseV + 2] = ReadVertex(tri, 36);
            indices[baseV] = baseV;
            indices[baseV + 1] = baseV + 1;
            indices[baseV + 2] = baseV + 2;
            if (i % 50_000 == 0)
                progress?.Report(new ImportProgress($"STL triangles {i}/{count}", 0.1 + 0.8 * i / Math.Max(1, count)));
        }

        return new TriangleMesh(vertices, indices);
    }

    private static Point3 ReadVertex(ReadOnlySpan<byte> tri, int offset) => new(
        BinaryPrimitives.ReadSingleLittleEndian(tri[offset..]),
        BinaryPrimitives.ReadSingleLittleEndian(tri[(offset + 4)..]),
        BinaryPrimitives.ReadSingleLittleEndian(tri[(offset + 8)..]));

    private static TriangleMesh ReadAscii(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        var verts = new List<Point3>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length < 6 || !trimmed.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                continue;
            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
                continue;
            verts.Add(new Point3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])));
        }

        if (verts.Count % 3 != 0)
            throw new InvalidDataException("ASCII STL vertex count is not a multiple of 3.");

        var indices = new int[verts.Count];
        for (var i = 0; i < indices.Length; i++)
            indices[i] = i;
        return new TriangleMesh(verts.ToArray(), indices);
    }

    private static double Parse(string s) =>
        double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
}
