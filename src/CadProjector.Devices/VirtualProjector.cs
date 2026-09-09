using CadProjector.Core.Devices;
using CadProjector.Logging;
using CadProjector.Rendering;

namespace CadProjector.Devices;

public sealed class VirtualProjector : ILaserProjector
{
    public string Id { get; } = "virtual";
    public string DisplayName { get; set; } = "Virtual Projector";
    public bool IsConnected { get; private set; }
    public ProjectorPose3D Pose { get; } = new();
    public bool IsPlaying { get; private set; }
    public LinesCollection? LastFrame { get; private set; }
    public int LastSegmentCount => LastFrame?.SegmentCount ?? 0;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        CadLog.Info($"Connected {DisplayName}");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var was = IsConnected;
        IsPlaying = false;
        IsConnected = false;
        LastFrame = null;
        if (was)
            CadLog.Info($"Disconnected {DisplayName}");
        return Task.CompletedTask;
    }

    public void SendFrame(LinesCollection frame)
    {
        LastFrame = frame;
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Virtual projector is not connected.");
        IsPlaying = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsPlaying = false;
        return Task.CompletedTask;
    }
}
