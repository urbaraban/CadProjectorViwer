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
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Interfaces;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanelItem.xaml
    /// </summary>
    public partial class ScrollPanelItem : UserControl
    {
        public UidObject uidObject => (UidObject)this.DataContext;

        public bool IsSelected
        {
            get => this._isselected;
            set
            {
                this._isselected = value;
            }
        }
        private bool _isselected = false;

        public bool IsSolved
        {
            get => this._issolved;
            set
            {
                this._issolved = value;
            }
        }
        private bool _issolved = false;


        private string filepath = string.Empty;

        public string FileName => uidObject.NameID;

        public ScrollPanelItem()
        {
            InitializeComponent();
        }


        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            uidObject.IsSelected = !uidObject.IsSelected;
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
                DragDrop.DoDragDrop(this, this.uidObject, Keyboard.Modifiers == ModifierKeys.Alt ? DragDropEffects.Copy : DragDropEffects.Move);
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (this.DataContext is UidObject Obj)
            {
                Obj.UpdateTransform(new CadRect3D(Obj.Bounds.Width, Obj.Bounds.Height, 0), AppSt.Default.Attach);
            }
        }


        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            uidObject.Remove();
        }


        private void SolvedToggle_Checked(object sender, RoutedEventArgs e)
        {
            //this.IsSolved = SolvedToggle.IsChecked.Value;
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

    public class ItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CadGroup cadGroup)
            {
                return cadGroup;
            }
            else if (value is UidObject uidObject)
            {
                return new List<UidObject>() { uidObject };
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
