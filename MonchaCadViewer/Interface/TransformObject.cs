using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MonchaCadViewer.Interface
{
    interface TransformObject
    {
        TransformGroup TransformGroup { get; }
        ScaleTransform Scale { get; }
        RotateTransform Rotate { get; }
        TranslateTransform Translate { get; }

        double X { get; set; }
        double Y { get; set; }

        bool IsFix { get; set; }
        bool WasMove { get; }
        bool Mirror { get; set; }

        void UpdateTransform(TransformGroup transformGroup, bool ResetPosition);
    }
}
