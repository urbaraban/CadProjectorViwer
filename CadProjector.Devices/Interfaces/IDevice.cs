using System;
using System.Threading.Tasks;

namespace CadProjector.Devices
{
    public interface IConnected
    {
        string IpAddress { get; }
        bool Status { get; }
        Task Reconnect();
    }

    public interface IRenderingDisplay
    {
   Guid Uid { get; }
        bool IsActive { get; }
        ProjectionSettings ProjectionSetting { get; }
    }

    public class ProjectionSettings
  {
        public float Scale { get; set; } = 1.0f;
      public float Brightness { get; set; } = 1.0f;
    }
}