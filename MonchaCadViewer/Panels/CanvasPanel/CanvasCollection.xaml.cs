using CadProjectorSDK;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.StaticTools;
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
        ProjectorHub projectorHub => (ProjectorHub)this.DataContext;

        public CanvasCollection()
        {
            InitializeComponent();
        }

        protected async override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (await FileLoad.GetScene(e) is UidObject Obj)
            {
                if (this.projectorHub.ScenesCollection.LoadedObject.Contains(Obj) == false)
                {
                    this.projectorHub.ScenesCollection.LoadedObject.Add(Obj);
                }
                else
                {
                    this.projectorHub.ScenesCollection.SelectedScene.Add(Obj);
                }
            }
        }

        public ICommand NextCommand => new ActionCommand(Next);
        private async void Next()
        {
            int index = this.projectorHub.ScenesCollection.IndexOf(this.projectorHub.ScenesCollection.SelectedScene);
            this.projectorHub.ScenesCollection.SelectedScene = this.projectorHub.ScenesCollection[Math.Abs(index + 1) % this.projectorHub.ScenesCollection.Count];
        }

        public ICommand PreviousCommand => new ActionCommand(Previous);
        private async void Previous()
        {
            int index = this.projectorHub.ScenesCollection.IndexOf(this.projectorHub.ScenesCollection.SelectedScene);
            this.projectorHub.ScenesCollection.SelectedScene = this.projectorHub.ScenesCollection[Math.Abs(index - 1) % this.projectorHub.ScenesCollection.Count];
        }
    }
}
