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
    public class CadLine : CadObject, INotifyPropertyChanged
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

        public LPoint3D P1;
        public LPoint3D P2;

        public double Lenth => LPoint3D.Lenth3D(P1, P2);

        public override double X
        {
            get => Math.Min(P1.MX, P2.MX);
            set
            {
                if (this.IsFix == false)
                {
                    double delta = value - Math.Min(P1.MX, P2.MX);
                    P1.MX += delta;
                    P2.MX += delta;
                    Updated?.Invoke(this, "X");
                    OnPropertyChanged("X");
                }
            }
        }
        public override double Y
        {
            get => Math.Min(P1.MY, P2.MY);
            set
            {
                if (this.IsFix == false)
                {
                    double delta = value - Math.Min(P1.MY, P2.MY);
                    P1.MY += delta;
                    P2.MY += delta;
                    Updated?.Invoke(this, "Y");
                    OnPropertyChanged("Y");
                }
            }
        }


        private List<CadAnchor> anchors;

        public override Rect Bounds => new Rect(P1.GetMPoint, P2.GetMPoint);

        public CadLine(LPoint3D P1, LPoint3D P2, bool MouseSet)
        {
            this.P1 = P1;
            this.P2 = P2;
            LoadSetting(MouseSet);
        }

        private void LoadSetting(bool MouseSet)
        {
            this.P1.PropertyChanged += P1_PropertyChanged;
            this.P2.PropertyChanged += P1_PropertyChanged;

            this.Render = true;
            this.TransformGroup = new Transform3DGroup()
            {
                Children = new Transform3DCollection()
                    {
                        new ScaleTransform3D(),
                        new RotateTransform3D(),
                        new TranslateTransform3D()
                    }
            };

            if (MouseSet == true)
            {
                this.Loaded += CadRectangle_Loaded;
            }
            else
            {
                this.Loaded += CadRectangleSet_Loaded;
            }
        }

        private void P1_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            Updated?.Invoke(this, "Point");
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
                this.P2.Set(e.GetPosition(this));
                this.InvalidateVisual();
            });
        }

        private void canvas_MouseLeftButtonUP(object sender, MouseButtonEventArgs e)
        {
            if (sender is CadCanvas cadCanvas)
            {
                this.P2.Set(e.GetPosition(cadCanvas));
                cadCanvas.MouseLeftButtonUp -= canvas_MouseLeftButtonUP;
                cadCanvas.MouseMove -= Canvas_MouseMove;
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
                    new CadAnchor(this.P1, this.P1, false){ Render = false },
                    new CadAnchor(this.P2, this.P2, false){ Render = false }
                };

                foreach (CadAnchor cadAnchor in anchors)
                {
                    cadCanvas.Add(cadAnchor);
                }
            }
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.IsSelected == true)
            {
                drawingContext.DrawText(new FormattedText(Math.Round(this.Lenth, 2).ToString(), 
                    new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
                   new Typeface("Segoe UI"), (int)MonchaHub.GetThinkess * 3, Brushes.Gray), 
                   new Point(this.X + Math.Abs(P1.MX - P2.MX) / 2, this.Y + Math.Abs(P1.MY - P2.MY) / 2));
            }

            drawingContext.DrawLine(myPen, P1.GetMPoint, P2.GetMPoint);
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
