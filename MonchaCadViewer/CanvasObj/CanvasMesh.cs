using CadProjectorSDK.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.CanvasObj
{
    public class CanvasMesh : CanvasObject
    {
        public CanvasMesh(LDeviceMesh mesh) : base(mesh, true)
        {

        }
    }
}
