using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.Interface;
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

        public LRect LRect;

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

        public CadRectangle(LPoint3D P1, LPoint3D P2, bool MouseSet)
        {
            this.Render = false;
            this.TransformGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                    {
                        new ScaleTransform(),
                        new RotateTransform(),
                        new TranslateTransform()
                    }
            };

            this.LRect = new LRect(P1, P2);

            if (MouseSet == true)
            {
                this.Loaded += CadRectangle_Loaded;
            }
            else
            {
                this.Loaded += CadRectangleSet_Loaded;
            }
        }

        public CadRectangle(LRect lRect, bool MouseSet)
        {
            this.Render = false;
            this.TransformGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                    {
                        new ScaleTransform(),
                        new RotateTransform(),
                        new TranslateTransform()
                    }
            };

            LRect = lRect;
            LRect.PropertyChanged += Point1_PropertyChanged;

            if (MouseSet == true)
            {
                this.Loaded += CadRectangle_Loaded;
            }
            else
            {
                this.Loaded += CadRectangleSet_Loaded;
            }
        }


        private void Point1_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
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
                this.LRect.P2.Set(e.GetPosition(cadCanvas));
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
                    new CadAnchor(this.LRect.P1, this.LRect.P1, MonchaHub.GetThinkess * 3, false){ Render = false },
                    new CadAnchor(this.LRect.P1, this.LRect.P2, MonchaHub.GetThinkess * 3, false){ Render = false },
                    new CadAnchor(this.LRect.P2, this.LRect.P2, MonchaHub.GetThinkess * 3, false){ Render = false },
                    new CadAnchor(this.LRect.P2, this.LRect.P1, MonchaHub.GetThinkess * 3, false){ Render = false }
                };

                foreach (CadAnchor cadAnchor in anchors)
                {
                    cadCanvas.Add(cadAnchor);
                }
            }
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            SolidColorBrush TransparentBrush = new SolidColorBrush();
            TransparentBrush.Color = Colors.Transparent;

            Pen myPen = new Pen(Brushes.Blue, MonchaHub.GetThinkess / 2);
            drawingContext.DrawRectangle(TransparentBrush, myPen, new Rect(X, Y, Math.Abs(LRect.P1.MX - LRect.P2.MX), Math.Abs(LRect.P1.MY - LRect.P2.MY)));
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


public class CadRectangleAdorner : Adorner
{
    //use thumb for resizing elements
    Thumb topLeft, topRight, bottomLeft, bottomRight;
    //visual child collection for adorner
    VisualCollection visualChilderns;

    public CadRectangleAdorner(CadRectangle element) : base(element)
    {
        CadRectangle Parent = element;
        this.RenderTransform = element.TransformGroup;

        visualChilderns = new VisualCollection(this);

        visualChilderns.Add(new Rectangle()
        {
            Width = 100,
            Height = 100,
            Fill = Brushes.Red,
        });

        element.MouseMove += Element_MouseMove;

        //adding thumbs for drawing adorner rectangle and setting cursor
        BuildAdornerCorners(ref topLeft, Cursors.SizeNWSE);
        BuildAdornerCorners(ref topRight, Cursors.SizeNESW);
        BuildAdornerCorners(ref bottomLeft, Cursors.SizeNESW);
        BuildAdornerCorners(ref bottomRight, Cursors.SizeNWSE);

        //registering drag delta events for thumb drag movement
        topLeft.DragDelta += TopLeft_DragDelta;
        topRight.DragDelta += TopRight_DragDelta;
        bottomLeft.DragDelta += BottomLeft_DragDelta;
        bottomRight.DragDelta += BottomRight_DragDelta;
    }

    private void Element_MouseMove(object sender, MouseEventArgs e)
    {
        this.InvalidateVisual();
    }

    public void BuildAdornerCorners(ref Thumb cornerThumb, Cursor customizedCursors)
    {
        //adding new thumbs for adorner to visual childern collection
        if (cornerThumb != null) 
            return;

        cornerThumb = new Thumb() { 
            Cursor = customizedCursors, 
            Height = 200,
            Width = 200, Opacity = 1, 
            Background = new SolidColorBrush(Colors.Red) 
        };
    }

    private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
        Thumb bottomRightCorner = sender as Thumb;
        //setting new height and width after drag
        if (adornedElement != null && bottomRightCorner != null)
        {
            //EnforceSize(adornedElement);

            double oldWidth = adornedElement.Width;
            double oldHeight = adornedElement.Height;

            double newWidth = Math.Max(adornedElement.Width + e.HorizontalChange, bottomRightCorner.DesiredSize.Width);
            double newHeight = Math.Max(e.VerticalChange + adornedElement.Height, bottomRightCorner.DesiredSize.Height);

            adornedElement.Width = newWidth;
            adornedElement.Height = newHeight;
        }
    }

    private void TopRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
        Thumb topRightCorner = sender as Thumb;
        //setting new height, width and canvas top after drag
        if (adornedElement != null && topRightCorner != null)
        {
            //EnforceSize(adornedElement);

            double oldWidth = adornedElement.Width;
            double oldHeight = adornedElement.Height;

            double newWidth = Math.Max(adornedElement.Width + e.HorizontalChange, topRightCorner.DesiredSize.Width);
            double newHeight = Math.Max(adornedElement.Height - e.VerticalChange, topRightCorner.DesiredSize.Height);
            adornedElement.Width = newWidth;

            double oldTop = Canvas.GetTop(adornedElement);
            double newTop = oldTop - (newHeight - oldHeight);
            adornedElement.Height = newHeight;
            //Canvas.SetTop(adornedElement, newTop);
        }
    }

    private void TopLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
        Thumb topLeftCorner = sender as Thumb;
        //setting new height, width and canvas top, left after drag
        if (adornedElement != null && topLeftCorner != null)
        {
            //EnforceSize(adornedElement);

            double oldWidth = adornedElement.Width;
            double oldHeight = adornedElement.Height;

            double newWidth = Math.Max(adornedElement.Width - e.HorizontalChange, topLeftCorner.DesiredSize.Width);
            double newHeight = Math.Max(adornedElement.Height - e.VerticalChange, topLeftCorner.DesiredSize.Height);

            double oldLeft = Canvas.GetLeft(adornedElement);
            double newLeft = oldLeft - (newWidth - oldWidth);
            adornedElement.Width = newWidth;
            //Canvas.SetLeft(adornedElement, newLeft);

            double oldTop = Canvas.GetTop(adornedElement);
            double newTop = oldTop - (newHeight - oldHeight);
            adornedElement.Height = newHeight;
            //Canvas.SetTop(adornedElement, newTop);
        }
    }

    private void BottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        FrameworkElement adornedElement = this.AdornedElement as FrameworkElement;
        Thumb topRightCorner = sender as Thumb;
        //setting new height, width and canvas left after drag
        if (adornedElement != null && topRightCorner != null)
        {
            //EnforceSize(adornedElement);

            double oldWidth = adornedElement.Width;
            double oldHeight = adornedElement.Height;

            double newWidth = Math.Max(adornedElement.Width - e.HorizontalChange, topRightCorner.DesiredSize.Width);
            double newHeight = Math.Max(adornedElement.Height + e.VerticalChange, topRightCorner.DesiredSize.Height);

            double oldLeft = Canvas.GetLeft(adornedElement);
            double newLeft = oldLeft - (newWidth - oldWidth);
            adornedElement.Width = newWidth;
            Canvas.SetLeft(adornedElement, newLeft);

            adornedElement.Height = newHeight;
        }
    }


    /*protected override void OnRender(DrawingContext drawingContext)
    {
        Rect adornedElementRect = new Rect(this.AdornedElement.DesiredSize);
        SolidColorBrush renderBrush = new SolidColorBrush(Colors.Black);
        renderBrush.Opacity = 0.3;
        Pen renderPen = new Pen(new SolidColorBrush(Colors.Black), 1.5);
        double radius = MonchaHub.GetThinkess;
        drawingContext.DrawEllipse(renderBrush, renderPen, Parent.Bounds.TopLeft, radius, radius);
        drawingContext.DrawEllipse(renderBrush, renderPen, Parent.Bounds.TopRight, radius, radius);
        drawingContext.DrawEllipse(renderBrush, renderPen, Parent.Bounds.BottomLeft, radius, radius);
        drawingContext.DrawEllipse(renderBrush, renderPen, Parent.Bounds.BottomRight, radius, radius);
    }*/

    // Arrange the Adorners.

    protected override Size ArrangeOverride(Size finalSize)
    {
        // desiredWidth and desiredHeight are the width and height of the element that's being adorned.

        // These will be used to place the ResizingAdorner at the corners of the adorned element.

        double desiredWidth = AdornedElement.DesiredSize.Width;

        double desiredHeight = AdornedElement.DesiredSize.Height;

        // adornerWidth & adornerHeight are used for placement as well.

        double adornerWidth = 200;

        double adornerHeight = 200;

        topLeft.Arrange(new Rect(200, 200, adornerWidth, adornerHeight));

        topRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));

        bottomLeft.Arrange(new Rect(-adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

        bottomRight.Arrange(new Rect(desiredWidth - adornerWidth / 2, desiredHeight - adornerHeight / 2, adornerWidth, adornerHeight));

        // Return the final size.

        return finalSize;
    }


    protected override int VisualChildrenCount => visualChilderns.Count;
    protected override Visual GetVisualChild(int index) => visualChilderns[index];
}

