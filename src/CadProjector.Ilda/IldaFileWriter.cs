namespace CadProjector.Ilda;

public static class IldaFileWriter
{
    /// <summary>Writes ILDA format-5 file with a zero-point terminator frame.</summary>
    public static async Task WriteAsync(string path, IldaFrame frame, CancellationToken ct = default)
    {
        var dataFrame = IldaEncoder.ToFormat5Bytes(frame, frameNumber: 0, ct);
        var terminator = new IldaFrame
        {
            FrameName = frame.FrameName,
            CompanyName = frame.CompanyName
        };
        var termBytes = IldaEncoder.ToFormat5Bytes(terminator, frameNumber: 1, ct);

        await using var fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await fs.WriteAsync(dataFrame, ct);
        await fs.WriteAsync(termBytes, ct);
        await fs.FlushAsync(ct);
    }

    public static byte[] ToFileBytes(IldaFrame frame, CancellationToken ct = default)
    {
        var data = IldaEncoder.ToFormat5Bytes(frame, frameNumber: 0, ct);
        var terminator = new IldaFrame
        {
            FrameName = frame.FrameName,
            CompanyName = frame.CompanyName
        };
        var term = IldaEncoder.ToFormat5Bytes(terminator, frameNumber: 1, ct);
        var all = new byte[data.Length + term.Length];
        Buffer.BlockCopy(data, 0, all, 0, data.Length);
        Buffer.BlockCopy(term, 0, all, data.Length, term.Length);
        return all;
    }
}
