using CadProjector.Geometry.Primitives;

namespace CadProjector.Automation;

public enum RemoteEndpointType
{
    UdpBinary = 0,
    UdpText = 1,
    TcpText = 2
}

public enum RemoteCommandKind
{
    None = 0,
    LoadFile = 1,
    LoadGeometry = 2,
    Play = 3,
    Stop = 4,
    Clear = 5,
    Align = 6,
    Custom = 7
}

public sealed class ConnectedClient
{
    public required string EndpointId { get; init; }
    public required string RemoteIp { get; init; }
    public required int RemotePort { get; init; }
    public RemoteEndpointType Transport { get; init; }
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;

    public string Key => $"{EndpointId}:{RemoteIp}:{RemotePort}";
    public string Display => $"{RemoteIp}:{RemotePort}";
}

public sealed class RemoteGeometryPayload
{
    public required string Name { get; init; }
    public required List<List<Point2>> Contours { get; init; }
}

public sealed class RemoteCommand
{
    public required string EndpointId { get; init; }
    public required RemoteEndpointType Transport { get; init; }
    public required ConnectedClient Client { get; init; }
    public string Header { get; init; } = "";
    public int TaskId { get; init; }
    public short TableId { get; init; }
    public IReadOnlyList<string> Commands { get; init; } = [];
    public RemoteCommandKind Kind { get; init; }
    public string? Path { get; init; }
    public RemoteGeometryPayload? Geometry { get; init; }
    public string? RawText { get; init; }
    public bool ReplyRequested { get; init; }
}

public sealed class RemoteEndpointInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string DisplayName { get; set; } = "Endpoint";
    public RemoteEndpointType Type { get; set; } = RemoteEndpointType.UdpBinary;
    public string BindIp { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 11000;
    public bool AutoStart { get; set; }
    public string? BoundSceneId { get; set; }
}

public interface IRemoteEndpoint : IAsyncDisposable
{
    RemoteEndpointInfo Info { get; }
    bool IsListening { get; }
    event EventHandler<RemoteCommand>? CommandReceived;
    event EventHandler<ConnectedClient>? ClientSeen;
    event EventHandler<string>? Error;
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    Task SendAsync(ConnectedClient client, string message, CancellationToken ct = default);
}
