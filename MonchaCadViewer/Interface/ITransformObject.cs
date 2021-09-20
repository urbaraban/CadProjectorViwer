using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CadProjectorViewer.Interface
{
    interface ITransformObject
    {
        Transform3DGroup TransformGroup { get; }
        ScaleTransform3D Scale { get; }
        TranslateTransform3D Translate { get; }

        double X { get; set; }
        double Y { get; set; }
        double Z { get; set; }

        bool IsFix { get; set; }
        bool WasMove { get; }
        bool Mirror { get; set; }

        void UpdateTransform(bool ResetPosition);
    }
}
