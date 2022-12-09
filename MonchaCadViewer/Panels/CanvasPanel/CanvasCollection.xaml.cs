using CadProjectorSDK;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.StaticTools;
using CadProjectorViewer.ViewModel;
using Microsoft.Xaml.Behaviors.Core;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CadProjectorViewer.Panels.CanvasPanel
{
    /// <summary>
    /// Логика взаимодействия для CanvasCollection.xaml
    /// </summary>
    public partial class CanvasCollection : UserControl
    {
        public CanvasCollection()
        {
            InitializeComponent();
        }

        protected async override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (this.DataContext is ScenesCollection scenecollection)
            {
                if (await FileLoad.GetDrop(e, scenecollection.SelectedScene.ProjectionSetting.PointStep.Value) is UidObject Obj)
                {
                    if (scenecollection.LoadedObjects.Contains(Obj) == false)
                    {
                        SceneTask sceneTask = new SceneTask()
                        {
                            Object = Obj,
                            TableID = scenecollection.SelectedScene.TableID,
                        };
                        scenecollection.AddTask(sceneTask);
                    }
                    else
                    {
                        scenecollection.SelectedScene.Add(Obj);
                    }
                }
            }
        }

        public ICommand NextCommand => new ActionCommand(Next);
        private async void Next()
        {
            if (this.DataContext is ScenesCollection scenecollection)
            {
                int index = scenecollection.IndexOf(scenecollection.SelectedScene);
                scenecollection.SelectedScene =
                    scenecollection[Math.Abs(index + 1) % scenecollection.Count];
            }

        }

        public ICommand PreviousCommand => new ActionCommand(Previous);
        private async void Previous()
        {
            if (this.DataContext is ScenesCollection scenecollection)
            {
                int index = scenecollection.IndexOf(scenecollection.SelectedScene);
                scenecollection.SelectedScene =
                    scenecollection[Math.Abs(index - 1) % scenecollection.Count];
            }
        }

        public ICommand RefreshFrameCommand => new ActionCommand(refresh);
        private async void refresh()
        {
            if (this.DataContext is ScenesCollection scenecollection)
            {
                scenecollection.SelectedScene.RefreshScene();
            }
        }
    }
}
