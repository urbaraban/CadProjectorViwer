namespace CadProjector.Core.Devices;

public interface ILaserProjector
{
    string Id { get; }
    string DisplayName { get; }
    bool IsConnected { get; }
    ProjectorPose3D Pose { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task PlayAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
