using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.ViewModel.Scene;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanelItem.xaml
    /// </summary>
    public partial class ScrollPanelItem : UserControl
    {
        public SceneTask SceneTsk => (SceneTask)this.DataContext;


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

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (this.DataContext is SceneTask Obj && Obj.Object is TransformObject transformObject)
            {
                transformObject.UpdateTransform(transformObject.Bounds, false, "Middle:Middle");
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
