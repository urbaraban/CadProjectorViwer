using System;
using System.Threading.Tasks;

namespace CadProjector.Devices
{
    public interface IDevice
 {
        Guid Uid { get; }
      string Name { get; }
    bool IsActive { get; }
    }

    public interface ILaserDevice : IDevice
    {
        double Width { get; set; }
        double Height { get; set; }
        double PowerPercent { get; set; }
        Task Start();
        Task Stop();
    }

public interface IConnectedDevice : IDevice
    {
        string IpAddress { get; }
        int Port { get; }
        bool IsConnected { get; }
        Task<bool> Connect();
 Task Disconnect();
        Task Reconnect();
    }
}