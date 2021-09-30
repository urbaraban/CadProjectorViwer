using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.StaticTools;
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

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanelItem.xaml
    /// </summary>
    public partial class ScrollPanelItem : UserControl
    {
        public ProjectionScene ProjectionScene => (ProjectionScene)this.DataContext;

        public bool IsSelected
        {
            get => this._isselected;
            set
            {
                this._isselected = value;
                SetBackround();
            }
        }
        private bool _isselected = false;

        public bool IsSolved
        {
            get => this._issolved;
            set
            {
                this._issolved = value;
                SetBackround();
            }
        }
        private bool _issolved = false;

        public ProjectionScene Scene => (ProjectionScene)this.DataContext;

        private string filepath = string.Empty;

        public string FileName => Scene.NameID;

        public ScrollPanelItem()
        {
            InitializeComponent();
            this.Width = this.Height;
        }


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            ProjectionScene.IsSelected = !ProjectionScene.IsSelected;
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (this.DataContext is ProjectionScene scene)
            {
                foreach (UidObject cadObject in scene.Objects)
                {
                    cadObject.UpdateTransform(true);
                }
            }
        }


        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            ProjectionScene.RemoveScene();
        }

        private void SetBackround()
        {
            if (this._isselected == true)
            {
                this.BackCanvas.Background = Brushes.GreenYellow;
            }
            else if (this._issolved == true)
            {
                this.BackCanvas.Background = Brushes.Gray;
            }
            else
            {
                this.BackCanvas.Background = Brushes.White;
            }
        }

        private void SolvedToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.IsSolved = SolvedToggle.IsChecked.Value;
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => Refresh();

        public async void Refresh()
        {
           /*this.DataContext =
                new ProjectionScene(
                    new CanvasGroup((GCCollection)
                        await FileLoad.Get(this.filepath)));*/
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
}
