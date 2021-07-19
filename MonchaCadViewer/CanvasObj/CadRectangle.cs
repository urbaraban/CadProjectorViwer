using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.Interface;
using MonchaCadViewer.Panels.ObjectPanel;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MonchaCadViewer.CanvasObj
{
    public class CadRectangle : CadObject, INotifyPropertyChanged
    {
        #region Property
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public override event EventHandler<string> Updated;
        public override event EventHandler<CadObject> Removed;

        public LSize3D LRect
        {
            get => _lrect;
            set
            {
                if (_lrect != null) { }
                _lrect = value;
                _lrect.PropertyChanged += _lrect_PropertyChanged;
            }
        }

        public SolidColorBrush BackColorBrush;

        private void _lrect_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            Updated?.Invoke(this, "Rect");
        }

        private LSize3D _lrect;


        public override double X
        {
            get => Math.Min(LRect.P1.MX, LRect.P2.MX);
            set
            {
                if (this.IsFix == false)
                {
                    double delta = value - Math.Min(LRect.P1.MX, LRect.P2.MX);
                    LRect.P1.MX += delta;
                    LRect.P2.MX += delta;
                    Updated?.Invoke(this, "X");
                    OnPropertyChanged("X");
                }
            }
        }
        public override double Y
        {
            get => Math.Min(LRect.P1.MY, LRect.P2.MY);
            set
            {
                if (this.IsFix == false)
                {
                    double delta = value - Math.Min(LRect.P1.MY, LRect.P2.MY);
                    LRect.P1.MY += delta;
                    LRect.P2.MY += delta;
                    Updated?.Invoke(this, "Y");
                    OnPropertyChanged("Y");
                }
            }
        }


        private List<CadAnchor> anchors;

        public override Rect Bounds => new Rect(LRect.P1.GetMPoint, LRect.P2.GetMPoint);

        public CadRectangle(LPoint3D P1, LPoint3D P2, string Label, bool MouseSet)
        {
            this.Name = Label;
            this.LRect = new LSize3D(P1, P2);
            LoadSetting(MouseSet);
        }

        public CadRectangle(LSize3D lRect, string Label, bool MouseSet)
        {
            this.Name = Label;
            LRect = lRect;
            LoadSetting(MouseSet);
        }


        private void LoadSetting(bool MouseSet)
        {
            this.Render = false;
            UpdateTransform(null, false, new Rect(X, Y, Math.Abs(LRect.P1.MX - LRect.P2.MX), Math.Abs(LRect.P1.MY - LRect.P2.MY)));

            ContextMenuLib.CadRectMenu(this.ContextMenu);
            this.ContextMenuClosing += ContextMenu_ContextMenuClosing;

            if (MouseSet == true)
            {
                this.Loaded += CadRectangle_Loaded;
            }
            else
            {
                this.Loaded += CadRectangleSet_Loaded;
            }
        }

        private void ContextMenu_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Tag)
                {
                    case "common_Setting":
                        CadRectangleSizePanel cadRectangleSizePanel = new CadRectangleSizePanel(this);
                        cadRectangleSizePanel.Show();
                        break;

                }
            }
        }


        /// <summary>
        /// Load Adorner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadRectangle_Loaded(object sender, RoutedEventArgs e)
        {
            //this.adornerLayer.Add(new CadRectangleAdorner(this));
            //this.adornerLayer.Visibility = Visibility.Visible;

            if (this.Parent is CadCanvas canvas)
            {
                canvas.MouseLeftButtonUp += canvas_MouseLeftButtonUP;
                canvas.MouseMove += Canvas_MouseMove;
                canvas.CaptureMouse();
            }
            adornerLayer.InvalidateArrange();
        }

        private void CadRectangleSet_Loaded(object sender, RoutedEventArgs e)
        {
            AddAnchors();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                this.LRect.P2.Set(e.GetPosition(this));
                this.InvalidateVisual();
            });
        }

        private void canvas_MouseLeftButtonUP(object sender, MouseButtonEventArgs e)
        {
            if (sender is CadCanvas cadCanvas)
            {
                if (cadCanvas.UnderAnchor != null) this.LRect.P2 = cadCanvas.UnderAnchor.GetLPoint;
                else
                {
                    this.LRect.P2.Set(e.GetPosition(cadCanvas));
                }
                cadCanvas.MouseLeftButtonUp -= canvas_MouseLeftButtonUP;
                cadCanvas.MouseMove -= Canvas_MouseMove;
                /*
                this.ObjAdorner = new CadRectangleAdorner(this);
                this.ObjAdorner.Visibility = Visibility.Visible;
                
                adornerLayer.Visibility = Visibility.Visible;
                adornerLayer.Add(new CadRectangleAdorner(this));*/
            }

            this.ReleaseMouseCapture();

            AddAnchors();
        }

        private void AddAnchors()
        {
            this.InvalidateVisual();
            if (this.Parent is CadCanvas cadCanvas)
            {
                anchors = new List<CadAnchor>()
                {
                    new CadAnchor(this.LRect.P1, this.LRect.P1, false){ Render = false },
                    new CadAnchor(this.LRect.P1, this.LRect.P2, false){ Render = false },
                    new CadAnchor(this.LRect.P2, this.LRect.P2, false){ Render = false },
                    new CadAnchor(this.LRect.P2, this.LRect.P1, false){ Render = false }
                };

                foreach (CadAnchor cadAnchor in anchors)
                {
                    cadCanvas.Add(cadAnchor);
                }
            }
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawText(
                new FormattedText(this.Name.ToString(),
                new System.Globalization.CultureInfo("ru-RU"),
                FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    (int)MonchaHub.GetThinkess * 3,
                    Brushes.Gray),
                new Point(LRect.P1.MX, LRect.P1.MY));



            drawingContext.DrawRectangle(BackColorBrush, myPen, new Rect(X, Y, Math.Abs(LRect.P1.MX - LRect.P2.MX), Math.Abs(LRect.P1.MY - LRect.P2.MY)));
        }

        public override void Remove()
        {
            Removed?.Invoke(this, this);

            foreach (CadAnchor cadAnchor in anchors)
            {
                cadAnchor.Remove();
            }
        }

    }
}

