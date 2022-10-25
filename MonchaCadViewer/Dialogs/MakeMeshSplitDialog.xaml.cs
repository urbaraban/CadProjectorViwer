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
        private ProjectionScene _scene;
        private Rect _bounds;

        public MakeMeshSplitDialog(Rect bounds, ProjectionScene scene)
        {
            InitializeComponent();
            this._bounds = bounds;
            this._scene = scene;
            this.DataContextChanged += MakeMeshSplitDialog_DataContextChanged;
        }

        private void MakeMeshSplitDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsLoaded == true)
            {
                MakeSplit();
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded == true)
            {
                MakeSplit();
            }
        }

        private void MakeSplit()
        {
            if (_scene != null)
            {
                Rect[,] masks = new Rect[(int)StrokeUpDn.Value.Value, (int)ColumnUpDn.Value.Value];

                double width_step = this._bounds.Width / ColumnUpDn.Value.Value;
                double height_step = this._bounds.Height / StrokeUpDn.Value.Value;

                for (int i = 0; i < masks.GetLength(0); i += 1)
                {
                    for (int j = 0; j < masks.GetLength(1); j += 1)
                    {
                        masks[i, j] = new Rect(
                            this._bounds.X + width_step * j,
                            this._bounds.Y + height_step * i,
                            width_step,
                            height_step);
                    }
                }
                SceneTask sceneTask = new SceneTask()
                {
                    Object = masks,
                    TableID = _scene.TableID,
                    TaskID = -1,
                    TaskInfo = new System.IO.FileInfo("Masks"),
                    Command = new List<string>()
                };

                _scene.RunTask(sceneTask, false);
            }
        }
    }
}
