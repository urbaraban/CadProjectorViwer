using System.Text;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Automation;

public interface IBinaryScenario
{
    string Header { get; }
    bool TryRead(BytePacketReader reader, string workFolder, out object? body);
}

/// <summary>Legacy PointScenario — header "Points".</summary>
public sealed class PointsScenario : IBinaryScenario
{
    public string Header => "Points";

    public bool TryRead(BytePacketReader reader, string workFolder, out object? body)
    {
        body = null;
        var nameLen = reader.GetShort();
        if (nameLen < 0) return false;
        var name = reader.GetString(nameLen);
        var pathCount = reader.GetShort();
        if (pathCount < 0) return false;

        var contours = new List<List<Point2>>(pathCount);
        for (var p = 0; p < pathCount; p++)
        {
            var closed = reader.GetByte() != 0;
            var pointsCount = reader.GetShort();
            if (pointsCount < 0) return false;
            var poly = new List<Point2>(pointsCount);
            for (var i = 0; i < pointsCount; i++)
                poly.Add(new Point2(reader.GetDouble(), reader.GetDouble()));

            if (closed && poly.Count > 1)
            {
                var a = poly[0];
                var b = poly[^1];
                if (Math.Abs(a.X - b.X) > 1e-9 || Math.Abs(a.Y - b.Y) > 1e-9)
                    poly.Add(a);
            }

            if (poly.Count > 0)
                contours.Add(poly);
        }

        body = new RemoteGeometryPayload
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Points" : name.Trim(),
            Contours = contours
        };
        return true;
    }
}

public sealed class FilenameScenario : IBinaryScenario
{
    public string Header => "Filename";

    public bool TryRead(BytePacketReader reader, string workFolder, out object? body)
    {
        body = null;
        var len = reader.GetShort();
        if (len < 0) return false;
        var name = reader.GetEncoding1251(len);
        body = Path.Combine(workFolder, name);
        return true;
    }
}

public sealed class FilepathScenario : IBinaryScenario
{
    public string Header => "Filepath";

    public bool TryRead(BytePacketReader reader, string workFolder, out object? body)
    {
        body = null;
        var len = reader.GetShort();
        if (len < 0) return false;
        body = reader.GetEncoding1251(len);
        return true;
    }
}

/// <summary>Parses legacy binary UDP packets into <see cref="RemoteCommand"/> shells (client filled by transport).</summary>
public static class BinaryPacketParser
{
    static BinaryPacketParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static readonly IBinaryScenario[] Scenarios =
    [
        new PointsScenario(),
        new FilenameScenario(),
        new FilepathScenario()
    ];

    public static bool TryParse(
        byte[] buffer,
        string workFolder,
        out string header,
        out int taskId,
        out short tableId,
        out string[] commands,
        out RemoteCommandKind kind,
        out string? path,
        out RemoteGeometryPayload? geometry)
    {
        header = "";
        taskId = 0;
        tableId = 0;
        commands = [];
        kind = RemoteCommandKind.None;
        path = null;
        geometry = null;

        if (buffer.Length < 16) return false;

        try
        {
            var r = new BytePacketReader(buffer);
            header = r.GetAscii(8).Trim(' ', '\0');
            taskId = r.GetInt();
            tableId = r.GetShort();
            var cmdLen = r.GetShort();
            if (cmdLen < 0) return false;
            var commandsRaw = r.GetAscii(cmdLen).Trim();
            commands = commandsRaw.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var headerLocal = header;
            var scenario = Scenarios.FirstOrDefault(s =>
                string.Equals(s.Header, headerLocal, StringComparison.OrdinalIgnoreCase));
            if (scenario is not null && scenario.TryRead(r, workFolder, out var body))
            {
                if (body is RemoteGeometryPayload geo)
                {
                    geometry = geo;
                    kind = RemoteCommandKind.LoadGeometry;
                }
                else if (body is string p)
                {
                    path = p;
                    kind = RemoteCommandKind.LoadFile;
                }
            }

            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
