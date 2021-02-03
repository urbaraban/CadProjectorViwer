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

        protected CadRectangleAdorner adorner;

        public LPoint3D Point1;
        public LPoint3D Point2;

        public override Rect Bounds => new Rect(Point1.GetMPoint, Point2.GetPoint);

        public CadRectangle(LPoint3D Point1, LPoint3D Point2) : base (true, true)
        {
            this.Point1 = Point1;
            this.Point2 = Point2;

            this.X = Math.Min(Point1.X, Point2.X);
            this.Y = Math.Min(Point1.Y, Point2.Y);

            this.Loaded += CadRectangle_Loaded;
        }

        /// <summary>
        /// Load Adorner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CadRectangle_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTransform(null);

            this.adorner = new CadRectangleAdorner(this);
            this.adorner.Visibility = Visibility.Visible;

            //this.adornerLayer.Visibility = Visibility.Visible;

            if (this.Parent is CadCanvas canvas)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
                adornerLayer.Visibility = Visibility.Visible;
                adornerLayer.Add(new CadRectangleAdorner(this));
                canvas.MouseLeftButtonUp += canvas_MouseLeftButtonUP;
                canvas.MouseMove += Canvas_MouseMove;
                canvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                this.Point2.Set(e.GetPosition(this));
                this.InvalidateVisual();
            });
        }

        private void canvas_MouseLeftButtonUP(object sender, MouseButtonEventArgs e)
        {
            if (sender is CadCanvas inputElement)
            {
                this.Point2.Set(e.GetPosition(inputElement));
                inputElement.MouseLeftButtonUp -= canvas_MouseLeftButtonUP;
                inputElement.MouseMove -= Canvas_MouseMove;
                this.InvalidateVisual();
            }

            this.ReleaseMouseCapture();
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            SolidColorBrush TransparentBrush = new SolidColorBrush();
            TransparentBrush.Color = Colors.Transparent;

            Pen myPen = new Pen(Brushes.Blue, MonchaHub.GetThinkess / 2);
            drawingContext.PushTransform(this.TransformGroup);
            drawingContext.DrawRectangle(TransparentBrush, myPen, new Rect(0, 0, Math.Abs(Point1.X - Point2.X), Math.Abs(Point1.Y - Point2.Y)));
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, Math.Abs(Point1.X - Point2.X), Math.Abs(Point1.Y - Point2.Y))));
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
        this.RenderTransform = element.RenderTransform;

        visualChilderns = new VisualCollection(this);

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

    public void BuildAdornerCorners(ref Thumb cornerThumb, Cursor customizedCursors)
    {
        //adding new thumbs for adorner to visual childern collection
        if (cornerThumb != null) return;
        cornerThumb = new Thumb() { Cursor = customizedCursors, Height = MonchaHub.GetThinkess * 3, Width = MonchaHub.GetThinkess * 3, Opacity = 0.5, Background = new SolidColorBrush(Colors.DarkGreen) };
        cornerThumb.RenderTransform
                    = new TranslateTransform(1000, 1000);
        cornerThumb.RenderTransformOrigin = new Point(0.5, 0.5);
        visualChilderns.Add(cornerThumb);
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

    protected override void OnRender(DrawingContext drawingContext)
    {
        SolidColorBrush TransparentBrush = new SolidColorBrush();
        TransparentBrush.Color = Colors.Transparent;

        Pen myPen = new Pen(Brushes.Blue, MonchaHub.GetThinkess / 2);
        drawingContext.PushTransform(this.RenderTransform);
    }
}

