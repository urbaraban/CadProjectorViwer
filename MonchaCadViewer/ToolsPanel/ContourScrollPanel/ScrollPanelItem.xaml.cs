using MonchaCadViewer.CanvasObj;
using MonchaSDK;
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

namespace MonchaCadViewer.ToolsPanel.ContourScrollPanel
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanelItem.xaml
    /// </summary>
    public partial class ScrollPanelItem : UserControl
    {
        public event EventHandler<bool> Selected;
        public event EventHandler Removed;

        public bool IsSelected
        {
            get => this._isselected;
            set
            {
                this._isselected = value;
                Selected?.Invoke(this, this._isselected);
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

        

        public CadObjectsGroup objectsGroup;

        private CadCanvas cadCanvas;

        public ScrollPanelItem(CadObjectsGroup cadObjectsGroup)
        {
            InitializeComponent();

            this.Width = this.Height;
            this.NameLabel.Content = cadObjectsGroup.Name;

            Viewbox _viewbox = new Viewbox();
            _viewbox.Stretch = Stretch.Uniform;
            _viewbox.StretchDirection = StretchDirection.DownOnly;
            _viewbox.Margin = new Thickness(0);
            _viewbox.DataContext = cadObjectsGroup;
            _viewbox.ClipToBounds = true;
            _viewbox.Cursor = Cursors.Hand;
            _viewbox.MouseLeftButtonUp += _viewbox_MouseLeftButtonUp; ;
            _viewbox.MouseRightButtonUp += _viewbox_MouseRightButtonUp;


            Viewbox _canvasviewbox = new Viewbox();
            _canvasviewbox.Stretch = Stretch.Uniform;
            _canvasviewbox.StretchDirection = StretchDirection.DownOnly;
            _canvasviewbox.Margin = new Thickness(0);

            cadCanvas = new CadCanvas(MonchaHub.Size, false);
            cadCanvas.Background = Brushes.White;

            _canvasviewbox.Child = cadCanvas;
            _viewbox.Child = _canvasviewbox;
            Grid.SetColumn(_viewbox, 0);
            Grid.SetRow(_viewbox, 0);
            Grid.SetRowSpan(_viewbox, 2);

            MainGrid.Children.Add(_viewbox);

            this.objectsGroup = cadObjectsGroup;
            CadContour cadContour = new CadContour(cadObjectsGroup.myGeometry, false);
            cadContour.TransformGroup = this.objectsGroup.TransformGroup;
            cadCanvas.DrawContour(cadContour);
        }

        public void Remove()
        {
            Removed?.Invoke(this, null);
        }
        private void _viewbox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.IsSelected = !this.IsSelected;
        }

        private void _viewbox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Viewbox viewItem)
            {
                if (viewItem.DataContext is CadObjectsGroup CadObj)
                {
                    CadObj.UpdateTransform(CadObj.TransformGroup, true);
                }
            }
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            Removed?.Invoke(this, null);
        }

        private void SetBackround()
        {
            if (this._isselected == true)
            {
                cadCanvas.Background = Brushes.GreenYellow;
            }
            else if (this._issolved == true)
            {
                cadCanvas.Background = Brushes.Gray;
            }
            else
            {
                cadCanvas.Background = Brushes.White;
            }

            if (this._issolved == true)
            {
                NameLabel.Background = Brushes.Gray;
            }
            else if(this._isselected == true)
            {
                NameLabel.Background = Brushes.GreenYellow;
            }
            else
            {
                NameLabel.Background = Brushes.WhiteSmoke;
            }
        }

        private void SolvedToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.IsSolved = SolvedToggle.IsChecked.Value;
        }
    }
}
