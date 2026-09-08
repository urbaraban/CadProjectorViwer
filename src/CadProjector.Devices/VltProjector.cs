using System.Net;
using CadProjector.Core.Devices;
using CadProjector.Ilda;
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

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _controller?.Disconnect();
            if (!IPAddress.TryParse(Profile.Host, out var ip))
                throw new InvalidOperationException($"Invalid host '{Profile.Host}'.");
            _controller = new VLTLaserController(ip);
            _controller.Connect(Profile.Port);
            IsConnected = true;
        }
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            try { _controller?.TurnPlay(false); } catch { /* ignore */ }
            try { _controller?.Disconnect(); } catch { /* ignore */ }
            _controller = null;
            IsConnected = false;
            IsPlaying = false;
        }
        return Task.CompletedTask;
    }

    public void SendFrame(LinesCollection frame)
    {
        if (_controller is null || !IsConnected)
            throw new InvalidOperationException("VLT is not connected.");

        var ilda = IldaEncoder.FromNormalizedLines(
            frame,
            Profile.WidthResolution,
            Profile.HeightResolution,
            Profile.Red,
            Profile.Green,
            Profile.Blue,
            Profile.Alpha);

        // Prefer per-point colors from frame when present
        for (var i = 0; i < Math.Min(ilda.Points.Count, frame.Points.Count); i++)
        {
            var rp = frame.Points[i];
            if (rp.Blanked) continue;
            ilda.Points[i].R = rp.Color.R;
            ilda.Points[i].G = rp.Color.G;
            ilda.Points[i].B = rp.Color.B;
        }

        var bytes = IldaEncoder.ToFormat5Bytes(ilda);
        LastByteCount = bytes.Length;

        var scanrate = Math.Max(1, frame.Points.Count);
        var delay = (short)Math.Clamp(216000000.0 / scanrate / 2.0, 1, short.MaxValue);
        _controller.SendScan(delay);
        _controller.SendFrame(bytes);
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (_controller is null) throw new InvalidOperationException("VLT is not connected.");
        _controller.TurnPlay(true);
        IsPlaying = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_controller is null) return Task.CompletedTask;
        _controller.TurnPlay(false);
        IsPlaying = false;
        return Task.CompletedTask;
    }

    public void Dispose() => _ = DisconnectAsync();
}
