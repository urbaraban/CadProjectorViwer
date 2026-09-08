using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CadProjector.Automation;

/// <summary>
/// Connection manager (legacy ToCutEthernetHub analogue): multiple UDP/TCP endpoints,
/// unified command fan-in, and a roster of recently seen clients.
/// </summary>
public sealed class AutomationHub : IAsyncDisposable
{
    private readonly Dictionary<string, IRemoteEndpoint> _live = new(StringComparer.Ordinal);
    private readonly object _clientGate = new();

    public ObservableCollection<RemoteEndpointInfo> Endpoints { get; } = [];
    public ObservableCollection<ConnectedClient> Clients { get; } = [];
    public ObservableCollection<string> RecentLog { get; } = [];

    public string WorkFolder { get; set; } = Environment.CurrentDirectory;

    public event EventHandler<RemoteCommand>? CommandReceived;
    public event EventHandler<string>? Error;

    public AutomationHub()
    {
        // Default MES-compatible listener (stopped until Start).
        Endpoints.Add(new RemoteEndpointInfo
        {
            DisplayName = "UDP Binary",
            Type = RemoteEndpointType.UdpBinary,
            BindIp = "0.0.0.0",
            Port = 11000
        });
    }

    public RemoteEndpointInfo AddEndpoint(RemoteEndpointType type, string bindIp, int port, string? name = null)
    {
        var info = new RemoteEndpointInfo
        {
            DisplayName = name ?? type switch
            {
                RemoteEndpointType.UdpBinary => "UDP Binary",
                RemoteEndpointType.UdpText => "UDP Text",
                RemoteEndpointType.TcpText => "TCP Text",
                _ => "Endpoint"
            },
            Type = type,
            BindIp = string.IsNullOrWhiteSpace(bindIp) ? "0.0.0.0" : bindIp,
            Port = port
        };
        Endpoints.Add(info);
        return info;
    }

    public async Task RemoveEndpointAsync(RemoteEndpointInfo info)
    {
        await StopEndpointAsync(info);
        Endpoints.Remove(info);
    }

    public bool IsListening(RemoteEndpointInfo info) =>
        _live.TryGetValue(info.Id, out var ep) && ep.IsListening;

    public async Task StartEndpointAsync(RemoteEndpointInfo info)
    {
        if (_live.ContainsKey(info.Id))
            return;

        IRemoteEndpoint endpoint = info.Type switch
        {
            RemoteEndpointType.UdpBinary => new UdpBinaryEndpoint(info) { WorkFolder = WorkFolder },
            RemoteEndpointType.UdpText => new UdpTextEndpoint(info),
            RemoteEndpointType.TcpText => new TcpTextEndpoint(info),
            _ => throw new ArgumentOutOfRangeException(nameof(info))
        };

        endpoint.CommandReceived += OnEndpointCommand;
        endpoint.ClientSeen += OnClientSeen;
        endpoint.Error += (_, msg) =>
        {
            PushLog($"ERR [{info.DisplayName}] {msg}");
            Error?.Invoke(this, msg);
        };

        _live[info.Id] = endpoint;
        if (endpoint is UdpBinaryEndpoint bin)
            bin.WorkFolder = WorkFolder;
        await endpoint.StartAsync();
        PushLog($"START {info.DisplayName} {info.BindIp}:{info.Port} ({info.Type})");
    }

    public async Task StopEndpointAsync(RemoteEndpointInfo info)
    {
        if (!_live.Remove(info.Id, out var endpoint))
            return;
        endpoint.CommandReceived -= OnEndpointCommand;
        endpoint.ClientSeen -= OnClientSeen;
        await endpoint.DisposeAsync();
        PushLog($"STOP {info.DisplayName}");
    }

    public async Task StopAllAsync()
    {
        foreach (var info in Endpoints.ToList())
            await StopEndpointAsync(info);
    }

    public Task SendReplyAsync(RemoteCommand command, string message) =>
        _live.TryGetValue(command.EndpointId, out var ep)
            ? ep.SendAsync(command.Client, message)
            : Task.CompletedTask;

    private void OnEndpointCommand(object? sender, RemoteCommand e)
    {
        PushLog($"{e.Transport} {e.Header} cmds=[{string.Join('&', e.Commands)}] path={e.Path ?? "-"} geo={e.Geometry?.Name ?? "-"}");
        CommandReceived?.Invoke(this, e);
    }

    private void OnClientSeen(object? sender, ConnectedClient client)
    {
        lock (_clientGate)
        {
            var existing = Clients.FirstOrDefault(c => c.Key == client.Key);
            if (existing is not null)
            {
                existing.LastSeenUtc = DateTimeOffset.UtcNow;
                return;
            }
            Clients.Insert(0, client);
            while (Clients.Count > 32)
                Clients.RemoveAt(Clients.Count - 1);
        }
    }

    private void PushLog(string line)
    {
        RecentLog.Insert(0, $"{DateTime.Now:HH:mm:ss} {line}");
        while (RecentLog.Count > 40)
            RecentLog.RemoveAt(RecentLog.Count - 1);
    }

    public async ValueTask DisposeAsync() => await StopAllAsync();
}
