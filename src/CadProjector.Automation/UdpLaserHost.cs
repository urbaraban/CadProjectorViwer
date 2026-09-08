using System.Net.Sockets;
using System.Text;

namespace CadProjector.Automation;

public sealed class UdpCommandEventArgs : EventArgs
{
    public required string Header { get; init; }
    public int TaskId { get; init; }
    public short TableId { get; init; }
    public IReadOnlyList<string> Commands { get; init; } = [];
    public string? PathOrName { get; init; }
}

/// <summary>Byte-compatible UDP listener (legacy PROTOCOL.md: Filename/Filepath + CLEAR/SHOW/ALIGN/PLAY/OFF).</summary>
public sealed class UdpLaserHost : IAsyncDisposable
{
    static UdpLaserHost()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public int Port { get; private set; } = 11000;
    public string WorkFolder { get; set; } = Environment.CurrentDirectory;
    public bool IsRunning => _loop is { IsCompleted: false };

    public event EventHandler<UdpCommandEventArgs>? CommandReceived;
    public event EventHandler<string>? Error;

    public Task StartAsync(int port = 11000)
    {
        if (IsRunning) return Task.CompletedTask;
        Port = port;
        _cts = new CancellationTokenSource();
        _client = new UdpClient(port);
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

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _client is not null)
        {
            try
            {
                var result = await _client.ReceiveAsync(ct);
                if (TryParse(result.Buffer, out var args))
                    CommandReceived?.Invoke(this, args);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex.Message);
            }
        }
    }

    public bool TryParse(byte[] buffer, out UdpCommandEventArgs args)
    {
        args = null!;
        if (buffer.Length < 16) return false;

        var header = Encoding.ASCII.GetString(buffer, 0, 8).Trim(' ', '\0');
        var taskId = BitConverter.ToInt32(buffer, 8);
        var tableId = BitConverter.ToInt16(buffer, 12);
        var cmdLen = BitConverter.ToInt16(buffer, 14);
        if (cmdLen < 0 || 16 + cmdLen > buffer.Length) return false;

        var commandsRaw = Encoding.ASCII.GetString(buffer, 16, cmdLen).Trim();
        var commands = commandsRaw.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? path = null;
        var bodyOffset = 16 + cmdLen;
        if (header is "Filename" or "Filepath")
        {
            if (bodyOffset + 2 <= buffer.Length)
            {
                var strLen = BitConverter.ToInt16(buffer, bodyOffset);
                bodyOffset += 2;
                if (strLen > 0 && bodyOffset + strLen <= buffer.Length)
                {
                    var enc = Encoding.GetEncoding(1251);
                    var name = enc.GetString(buffer, bodyOffset, strLen);
                    path = header == "Filename"
                        ? Path.Combine(WorkFolder, name)
                        : name;
                }
            }
        }

        args = new UdpCommandEventArgs
        {
            Header = header,
            TaskId = taskId,
            TableId = tableId,
            Commands = commands,
            PathOrName = path
        };
        return true;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
