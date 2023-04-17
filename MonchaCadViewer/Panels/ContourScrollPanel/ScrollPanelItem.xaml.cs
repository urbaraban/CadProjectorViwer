using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.Opening;
using CadProjectorSDK;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using ToGeometryConverter;
using AppSt = CadProjectorViewer.Properties.Settings;
using ToGeometryConverter.Object;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using System.Globalization;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Interfaces;
using CadProjectorViewer.ViewModel;
using System.Xml.Linq;
using System.ComponentModel;
using CadProjectorViewer.ViewModel.Scene;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanelItem.xaml
    /// </summary>
    public partial class ScrollPanelItem : UserControl
    {
        public SceneTask SceneTsk => (SceneTask)this.DataContext;


        private string filepath = string.Empty;

        public string FileName => SceneTsk.TaskInfo.Name;

        public ScaleTransform Scale { get; set; } = new ScaleTransform();

        public ScrollPanelItem()
        {
            InitializeComponent();
        }


        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            SceneTsk.Selecting();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            //DragDrop.DoDragDrop(this, this.Scene, DragDropEffects.Move);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(this, this, Keyboard.Modifiers == ModifierKeys.Alt ? DragDropEffects.Copy : DragDropEffects.Move);
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (this.DataContext is SceneTask Obj && Obj.Object is TransformObject transformObject)
            {
                transformObject.UpdateTransform(transformObject.Bounds, false, "Middle%Middle");
            }
        }


        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            SceneTsk.Remove();
        }


        private void SolvedToggle_Checked(object sender, RoutedEventArgs e)
        {
            //this.IsSolved = SolvedToggle.IsChecked.Value;
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => Refresh();

        public async void Refresh()
        {
            foreach(GCFormat gCFormat in FileLoad.MyFormat)
            {
                if (gCFormat.ShortName.Contains(this.SceneTsk.TaskInfo.Extension) == true)
                {
                    object obj = await gCFormat.ReadFile?.Invoke(this.SceneTsk.TaskInfo.FullName, 35);
                    this.SceneTsk.Object = await FileLoad.ConvertObject(obj);
                    this.SceneTsk.Selecting();
                }
            }
        }
    }

    public class BackColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b == true) return Brushes.YellowGreen;
            else return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ProjectionScene scene = new ProjectionScene();

            if (value is UidObject uidObject)
            {
                scene.Size.Width = uidObject.Bounds.Width;
                scene.Size.Height = uidObject.Bounds.Height;
                scene.Add(uidObject);
            }

            return new SceneModel(scene);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
