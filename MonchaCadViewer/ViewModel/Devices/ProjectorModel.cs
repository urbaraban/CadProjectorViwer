using CadProjectorSDK.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Devices
{
    public class ProjectorModel
    {
        public LProjector LProjector { get; }

        public ProjectorModel(LProjector projector)
        {
            this.LProjector = projector;
        }
    }
}
