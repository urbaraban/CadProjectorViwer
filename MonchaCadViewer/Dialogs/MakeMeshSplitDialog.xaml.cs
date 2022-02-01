using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Scenes;
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
    /// Логика взаимодействия для MakeMeshSplitDialog.xaml
    /// </summary>
    public partial class MakeMeshSplitDialog : Window
    {
        public MakeMeshSplitDialog()
        {
            InitializeComponent();
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.DataContext is ProjectionScene Scene)
            {
                Scene.Masks.Clear();

                double WidthStep = Scene.Size.Width / ColumnUpDn.Value.Value;
                double HeightStep = Scene.Size.Height / StrokeUpDn.Value.Value;

                for (int i = 0; i < ColumnUpDn.Value.Value; i += 1)
                {
                    for (int j = 0; j < StrokeUpDn.Value.Value; j += 1)
                    {
                        Scene.Masks.Add(new CadRect3D(
                            new CadPoint3D(new Point(i * WidthStep, j * HeightStep), Scene.Size, true),
                            new CadPoint3D(new Point((i + 1) * WidthStep, (j + 1) * HeightStep), Scene.Size, true),
                            true,
                            $"Mask:{i}/{j}"));
                    }
                }
            }
        }
    }
}
