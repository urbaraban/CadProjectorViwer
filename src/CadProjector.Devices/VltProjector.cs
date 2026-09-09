using System.Net;
using CadProjector.Core.Devices;
using CadProjector.Ilda;
using CadProjector.Logging;
using CadProjector.Rendering;
using VLTLaserControllerNET;

namespace CadProjector.Devices;

public sealed class VltProjector : ILaserProjector, IDisposable
{
    private VLTLaserController? _controller;
    private readonly object _gate = new();

    public VltProjector(ProjectorProfile profile)
    {
        Profile = profile;
    }

    public ProjectorProfile Profile { get; }
    public string Id => Profile.Id;
    public string DisplayName => Profile.DisplayName;
    public bool IsConnected { get; private set; }
    public ProjectorPose3D Pose => Profile.Pose;
    public bool IsPlaying { get; private set; }
    public int LastByteCount { get; private set; }
    public event EventHandler? ConnectionLost;

    private int _aliveMisses;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_gate)
            {
                _controller?.Disconnect();
                if (!IPAddress.TryParse(Profile.Host, out var ip))
                    throw new InvalidOperationException($"Invalid host '{Profile.Host}'.");
                _controller = new VLTLaserController(ip);
                _controller.Connect(Profile.Port);
                _controller.Disconnected += OnControllerDisconnected;
                IsConnected = true;
                _aliveMisses = 0;
            }
            CadLog.Good($"Connected {DisplayName} {Profile.Host}:{Profile.Port}");
        }
        catch (Exception ex)
        {
            IsConnected = false;
            CadLog.Error($"Connect failed ({DisplayName}): {ex.Message}");
            throw;
        }
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            DetachControllerEvents();
            try { _controller?.TurnPlay(false); } catch { /* ignore */ }
            try { _controller?.Disconnect(); } catch { /* ignore */ }
            _controller = null;
            var was = IsConnected;
            IsConnected = false;
            IsPlaying = false;
            if (was)
                CadLog.Info($"Disconnected {DisplayName}");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// QUERY the device (UDP). Two consecutive misses count as a lost link and stop the laser.
    /// </summary>
    public bool ProbeAlive()
    {
        lock (_gate)
        {
            if (!IsConnected || _controller is null)
                return false;

            try
            {
                if (_controller.WakeUpDevice() || _controller.IsAlive)
                {
                    _aliveMisses = 0;
                    return true;
                }

                _aliveMisses++;
                if (_aliveMisses < 2)
                    return true;

                MarkLostLocked();
                return false;
            }
            catch
            {
                MarkLostLocked();
                return false;
            }
        }
    }

    public void SendFrame(LinesCollection frame)
    {
        lock (_gate)
        {
            if (_controller is null || !IsConnected)
                throw new InvalidOperationException("VLT is not connected.");

            var ilda = DeviceIldaEncoder.FromDeviceBag(frame, Profile);
            var bytes = IldaEncoder.ToFormat5Bytes(ilda);
            LastByteCount = bytes.Length;

            var scanrate = Math.Max(1, frame.Points.Count);
            var delay = (short)Math.Clamp(216000000.0 / scanrate / 2.0, 1, short.MaxValue);
            _controller.SendScan(delay);
            _controller.SendFrame(bytes);
        }
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_controller is null) throw new InvalidOperationException("VLT is not connected.");
            _controller.TurnPlay(true);
            IsPlaying = true;
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_controller is null) return Task.CompletedTask;
            _controller.TurnPlay(false);
            IsPlaying = false;
        }
        return Task.CompletedTask;
    }

    public void Dispose() => _ = DisconnectAsync();

    private void OnControllerDisconnected(object? sender, EventArgs e)
    {
        lock (_gate)
            MarkLostLocked();
    }

    private void DetachControllerEvents()
    {
        if (_controller is null) return;
        _controller.Disconnected -= OnControllerDisconnected;
    }

    private void MarkLostLocked()
    {
        if (!IsConnected)
            return;

        DetachControllerEvents();
        try { _controller?.TurnPlay(false); } catch { /* ignore */ }
        try { _controller?.Disconnect(); } catch { /* ignore */ }
        _controller = null;
        IsConnected = false;
        IsPlaying = false;
        CadLog.Error($"Lost link {DisplayName} — laser stopped");
        ConnectionLost?.Invoke(this, EventArgs.Empty);
    }
}
