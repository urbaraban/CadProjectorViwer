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
                byte[,] masks = new byte[(int)ColumnUpDn.Value.Value, (int)StrokeUpDn.Value.Value];

                for (int i = 0; i < masks.GetLength(0); i += 1)
                {
                    for (int j = 0; j < masks.GetLength(1); j += 1)
                    {
                        masks[i,j] = 1;
                    }
                }
                SceneTask sceneTask = new SceneTask()
                {
                    Object = masks,
                    TableID = Scene.TableID,
                    TaskID = -1,
                    TaskName = "Masks",
                    Command = new List<string>()
                };
                
                Scene.RunTask(sceneTask, false);
            }
        }
    }
}
