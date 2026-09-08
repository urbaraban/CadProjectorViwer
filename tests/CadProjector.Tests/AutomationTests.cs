using System.Buffers.Binary;
using System.Text;
using CadProjector.Automation;
using CadProjector.Geometry.Primitives;

namespace CadProjector.Tests;

public class BinaryPacketParserTests
{
    [Fact]
    public void Points_ParsesContoursAndClosesPath()
    {
        var payload = BuildPointsPacket(
            name: "Part",
            closed: true,
            points: [new Point2(0, 0), new Point2(10, 0), new Point2(10, 5)],
            commands: "CLEAR&PLAY");

        Assert.True(BinaryPacketParser.TryParse(
            payload, @"C:\work",
            out var header, out _, out _, out var cmds, out var kind, out var path, out var geo));

        Assert.Equal("Points", header);
        Assert.Equal(RemoteCommandKind.LoadGeometry, kind);
        Assert.Null(path);
        Assert.Contains("CLEAR", cmds);
        Assert.Contains("PLAY", cmds);
        Assert.NotNull(geo);
        Assert.Equal("Part", geo!.Name);
        Assert.Single(geo.Contours);
        Assert.Equal(4, geo.Contours[0].Count); // closed → first appended
        Assert.Equal(new Point2(0, 0), geo.Contours[0][0]);
        Assert.Equal(new Point2(0, 0), geo.Contours[0][^1]);
    }

    [Fact]
    public void Filename_ResolvesAgainstWorkFolder()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var name = "a.dxf";
        var nameBytes = Encoding.GetEncoding(1251).GetBytes(name);
        using var ms = new MemoryStream();
        WriteHeader(ms, "Filename", "PLAY");
        var len = (short)nameBytes.Length;
        ms.WriteByte((byte)(len & 0xff));
        ms.WriteByte((byte)((len >> 8) & 0xff));
        ms.Write(nameBytes);

        Assert.True(BinaryPacketParser.TryParse(
            ms.ToArray(), @"D:\jobs",
            out var header, out _, out _, out _, out var kind, out var path, out _));
        Assert.Equal("Filename", header);
        Assert.Equal(RemoteCommandKind.LoadFile, kind);
        Assert.Equal(Path.Combine(@"D:\jobs", "a.dxf"), path);
    }

    private static byte[] BuildPointsPacket(string name, bool closed, Point2[] points, string commands)
    {
        using var ms = new MemoryStream();
        WriteHeader(ms, "Points", commands);
        var nameBytes = Encoding.ASCII.GetBytes(name);
        WriteShort(ms, (short)nameBytes.Length);
        ms.Write(nameBytes);
        WriteShort(ms, 1); // path count
        ms.WriteByte(closed ? (byte)1 : (byte)0);
        WriteShort(ms, (short)points.Length);
        foreach (var p in points)
        {
            WriteDouble(ms, p.X);
            WriteDouble(ms, p.Y);
        }
        return ms.ToArray();
    }

    private static void WriteHeader(MemoryStream ms, string header8, string commands)
    {
        var h = Encoding.ASCII.GetBytes(header8.PadRight(8).Substring(0, 8));
        ms.Write(h);
        WriteInt(ms, 42);
        WriteShort(ms, 1);
        var cmd = Encoding.ASCII.GetBytes(commands);
        WriteShort(ms, (short)cmd.Length);
        ms.Write(cmd);
    }

    private static void WriteShort(Stream s, short v)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(b, v);
        s.Write(b);
    }

    private static void WriteInt(Stream s, int v)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(b, v);
        s.Write(b);
    }

    private static void WriteDouble(Stream s, double v)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(b, v);
        s.Write(b);
    }
}

public class TextAndJsonParserTests
{
    [Fact]
    public void Text_ParsesPlayAndLoad()
    {
        var client = new ConnectedClient
        {
            EndpointId = "e1",
            RemoteIp = "127.0.0.1",
            RemotePort = 1,
            Transport = RemoteEndpointType.TcpText
        };
        var cmd = TextCommandParser.ToRemoteCommand("CLEAR;LOAD:C:\\a.dxf;PLAY", "e1", RemoteEndpointType.TcpText, client);
        Assert.Equal(RemoteCommandKind.LoadFile, cmd.Kind);
        Assert.Equal(@"C:\a.dxf", cmd.Path);
        Assert.Contains("CLEAR", cmd.Commands);
        Assert.Contains("PLAY", cmd.Commands);
    }

    [Fact]
    public void Json_ParsesPlay()
    {
        var client = new ConnectedClient
        {
            EndpointId = "e1",
            RemoteIp = "127.0.0.1",
            RemotePort = 1,
            Transport = RemoteEndpointType.TcpText
        };
        Assert.True(JsonCommandParser.TryParse(
            """{"cmd":"play"}""", "e1", RemoteEndpointType.TcpText, client, out var cmd));
        Assert.Equal(RemoteCommandKind.Play, cmd.Kind);
    }

    [Fact]
    public void Line_AutodetectsJson()
    {
        var client = new ConnectedClient
        {
            EndpointId = "e1",
            RemoteIp = "127.0.0.1",
            RemotePort = 1,
            Transport = RemoteEndpointType.UdpText
        };
        var cmd = LineCommandParser.Parse("""{"cmd":"stop"}""", "e1", RemoteEndpointType.UdpText, client);
        Assert.Equal(RemoteCommandKind.Stop, cmd.Kind);
        Assert.Equal("Json", cmd.Header);
    }
}

public class AutomationHubTests
{
    [Fact]
    public void AddEndpoint_AppearsInCollection()
    {
        var hub = new AutomationHub();
        var before = hub.Endpoints.Count;
        hub.AddEndpoint(RemoteEndpointType.TcpText, "127.0.0.1", 12000, "Phone");
        Assert.Equal(before + 1, hub.Endpoints.Count);
        Assert.Contains(hub.Endpoints, e => e.DisplayName == "Phone" && e.Port == 12000);
    }
}
