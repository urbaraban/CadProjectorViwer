using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CadProjector.Automation;

public sealed class UdpBinaryEndpoint : IRemoteEndpoint
{
    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public UdpBinaryEndpoint(RemoteEndpointInfo info) => Info = info;

    public RemoteEndpointInfo Info { get; }
    public string WorkFolder { get; set; } = Environment.CurrentDirectory;
    public bool IsListening => _loop is { IsCompleted: false };

    public event EventHandler<RemoteCommand>? CommandReceived;
    public event EventHandler<ConnectedClient>? ClientSeen;
    public event EventHandler<string>? Error;

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsListening) return Task.CompletedTask;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var ep = new IPEndPoint(ParseBind(Info.BindIp), Info.Port);
        _client = new UdpClient(ep);
        _loop = Task.Run(() => ListenLoop(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        await _cts.CancelAsync();
        _client?.Close();
        if (_loop is not null)
        {
            try { await _loop; } catch { /* ignore */ }
        }
        _cts.Dispose();
        _cts = null;
        _client = null;
        _loop = null;
    }

    public async Task SendAsync(ConnectedClient client, string message, CancellationToken ct = default)
    {
        if (_client is null) return;
        var bytes = Encoding.UTF8.GetBytes(message);
        await _client.SendAsync(bytes, new IPEndPoint(IPAddress.Parse(client.RemoteIp), client.RemotePort), ct);
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _client is not null)
        {
            try
            {
                var result = await _client.ReceiveAsync(ct);
                var remote = (IPEndPoint)result.RemoteEndPoint;
                var client = new ConnectedClient
                {
                    EndpointId = Info.Id,
                    RemoteIp = remote.Address.ToString(),
                    RemotePort = remote.Port,
                    Transport = RemoteEndpointType.UdpBinary
                };
                ClientSeen?.Invoke(this, client);

                if (!BinaryPacketParser.TryParse(
                        result.Buffer, WorkFolder,
                        out var header, out var taskId, out var tableId, out var commands,
                        out var kind, out var path, out var geometry))
                    continue;

                CommandReceived?.Invoke(this, new RemoteCommand
                {
                    EndpointId = Info.Id,
                    Transport = RemoteEndpointType.UdpBinary,
                    Client = client,
                    Header = header,
                    TaskId = taskId,
                    TableId = tableId,
                    Commands = commands,
                    Kind = kind,
                    Path = path,
                    Geometry = geometry
                });
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex.Message);
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private static IPAddress ParseBind(string bindIp) =>
        string.IsNullOrWhiteSpace(bindIp) || bindIp is "0.0.0.0" or "*"
            ? IPAddress.Any
            : IPAddress.Parse(bindIp);
}

public sealed class UdpTextEndpoint : IRemoteEndpoint
{
    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public UdpTextEndpoint(RemoteEndpointInfo info) => Info = info;

    public RemoteEndpointInfo Info { get; }
    public bool IsListening => _loop is { IsCompleted: false };

    public event EventHandler<RemoteCommand>? CommandReceived;
    public event EventHandler<ConnectedClient>? ClientSeen;
    public event EventHandler<string>? Error;

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsListening) return Task.CompletedTask;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var ep = new IPEndPoint(ParseBind(Info.BindIp), Info.Port);
        _client = new UdpClient(ep);
        _loop = Task.Run(() => ListenLoop(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        await _cts.CancelAsync();
        _client?.Close();
        if (_loop is not null)
        {
            try { await _loop; } catch { /* ignore */ }
        }
        _cts.Dispose();
        _cts = null;
        _client = null;
        _loop = null;
    }

    public async Task SendAsync(ConnectedClient client, string message, CancellationToken ct = default)
    {
        if (_client is null) return;
        var bytes = Encoding.UTF8.GetBytes(message);
        await _client.SendAsync(bytes, new IPEndPoint(IPAddress.Parse(client.RemoteIp), client.RemotePort), ct);
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _client is not null)
        {
            try
            {
                var result = await _client.ReceiveAsync(ct);
                var remote = (IPEndPoint)result.RemoteEndPoint;
                var client = new ConnectedClient
                {
                    EndpointId = Info.Id,
                    RemoteIp = remote.Address.ToString(),
                    RemotePort = remote.Port,
                    Transport = RemoteEndpointType.UdpText
                };
                ClientSeen?.Invoke(this, client);
                var text = Encoding.UTF8.GetString(result.Buffer).Trim();
                if (string.IsNullOrEmpty(text)) continue;
                var cmd = LineCommandParser.Parse(text, Info.Id, RemoteEndpointType.UdpText, client);
                CommandReceived?.Invoke(this, cmd);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex.Message);
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private static IPAddress ParseBind(string bindIp) =>
        string.IsNullOrWhiteSpace(bindIp) || bindIp is "0.0.0.0" or "*"
            ? IPAddress.Any
            : IPAddress.Parse(bindIp);
}

public sealed class TcpTextEndpoint : IRemoteEndpoint
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private readonly Dictionary<string, TcpClient> _clients = new(StringComparer.Ordinal);

    public TcpTextEndpoint(RemoteEndpointInfo info) => Info = info;

    public RemoteEndpointInfo Info { get; }
    public bool IsListening => _acceptLoop is { IsCompleted: false };

    public event EventHandler<RemoteCommand>? CommandReceived;
    public event EventHandler<ConnectedClient>? ClientSeen;
    public event EventHandler<string>? Error;

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsListening) return Task.CompletedTask;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var ip = string.IsNullOrWhiteSpace(Info.BindIp) || Info.BindIp is "0.0.0.0" or "*"
            ? IPAddress.Any
            : IPAddress.Parse(Info.BindIp);
        _listener = new TcpListener(ip, Info.Port);
        _listener.Start();
        _acceptLoop = Task.Run(() => AcceptLoop(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;
        await _cts.CancelAsync();
        _listener?.Stop();
        lock (_clients)
        {
            foreach (var c in _clients.Values)
                c.Dispose();
            _clients.Clear();
        }
        if (_acceptLoop is not null)
        {
            try { await _acceptLoop; } catch { /* ignore */ }
        }
        _cts.Dispose();
        _cts = null;
        _listener = null;
        _acceptLoop = null;
    }

    public async Task SendAsync(ConnectedClient client, string message, CancellationToken ct = default)
    {
        TcpClient? tcp;
        lock (_clients)
            _clients.TryGetValue(client.Key, out tcp);
        if (tcp is null || !tcp.Connected) return;
        var bytes = Encoding.UTF8.GetBytes(message.EndsWith('\n') ? message : message + "\n");
        await tcp.GetStream().WriteAsync(bytes, ct);
    }

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener is not null)
        {
            try
            {
                var tcp = await _listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => ClientLoop(tcp, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex.Message);
            }
        }
    }

    private async Task ClientLoop(TcpClient tcp, CancellationToken ct)
    {
        var remote = (IPEndPoint)tcp.Client.RemoteEndPoint!;
        var client = new ConnectedClient
        {
            EndpointId = Info.Id,
            RemoteIp = remote.Address.ToString(),
            RemotePort = remote.Port,
            Transport = RemoteEndpointType.TcpText
        };
        lock (_clients)
            _clients[client.Key] = tcp;
        ClientSeen?.Invoke(this, client);

        try
        {
            await using var stream = tcp.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (!ct.IsCancellationRequested && tcp.Connected)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;
                line = line.Trim('\0', ' ', '\r');
                if (string.IsNullOrWhiteSpace(line)) continue;
                client.LastSeenUtc = DateTimeOffset.UtcNow;
                ClientSeen?.Invoke(this, client);
                var cmd = LineCommandParser.Parse(line, Info.Id, RemoteEndpointType.TcpText, client);
                CommandReceived?.Invoke(this, cmd);
            }
        }
        catch (OperationCanceledException) { /* ignore */ }
        catch (Exception ex)
        {
            Error?.Invoke(this, ex.Message);
        }
        finally
        {
            lock (_clients)
                _clients.Remove(client.Key);
            tcp.Dispose();
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
