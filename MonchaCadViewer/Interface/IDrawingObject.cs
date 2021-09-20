using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.Interface
{
    public interface IDrawingObject
    {
        bool IsInit { get; }

        void SetTwoPoint(System.Windows.Point point);

        void Init();
    }
}
