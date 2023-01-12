using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CadProjectorViewer.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для EllipsoidSettingDialog.xaml
    /// </summary>
    public partial class EllipsoidSettingDialog : Window
    {
        public EllipsoidSettingDialog()
        {
            InitializeComponent();
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.DataContext is LProjector projector)
            {
                IList<IRenderedObject> elements = new List<IRenderedObject>();

                double widthstep = 1d / (projector.Ellipsoid.XAxisCorrect.Count - 1);
                double heightstep = 1d / (projector.Ellipsoid.YAxisCorrect.Count - 1);

                //VectorLinesCollection dot = new VectorLinesCollection(CadProjectorSDK.Device.Mesh.MeshType.NONE);
                //dot.Add(new VectorLine(0.5, 0.5, 0.5, 0.5, false));
                //elements.Add(dot);

                for (int i = 0; i < projector.Ellipsoid.XAxisCorrect.Count; i += 1)
                {
                    VectorLinesCollection line = new VectorLinesCollection(CadProjectorSDK.Device.Mesh.MeshType.NONE)
                    {
                        new VectorLine(
                            new RenderPoint(i * widthstep, 0.1),
                            new RenderPoint(i * widthstep, 0.99), false)
                    };
                    elements.Add(line);
                }

                projector.RefreshFrame?.Invoke(elements);
            }
        }
    }
}
