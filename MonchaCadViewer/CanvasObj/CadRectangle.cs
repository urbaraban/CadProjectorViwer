using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CadRectangle : CadObject
    {
        protected Rectangle rectangle => (Rectangle)this.ObjectShape;
        protected CadRectangleAdorner adorner;

        public LPoint3D PointTL;
        public LPoint3D PointBR;

        public CadRectangle(LPoint3D PointTL, LPoint3D PointBR) : base(true, true)
        {
            this.PointTL = PointTL;
            this.PointBR = PointBR;

            this.PointTL.PropertyChanged += PointTL_PropertyChanged;
            this.PointBR.PropertyChanged += PointTL_PropertyChanged;

            this.ObjectShape = new Rectangle();

            this.Loaded += CadRectangle_Loaded;
        }

        private void PointTL_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => Update();

        private void CadRectangle_Loaded(object sender, RoutedEventArgs e)
        {
            this.adorner = new CadRectangleAdorner(this);
            this.adorner.Visibility = Visibility.Visible;

            this.adornerLayer.Add(this.adorner);
            this.adornerLayer.Visibility = Visibility.Visible;

            if (this.Parent is CadCanvas canvas)
            {
                canvas.MouseLeftButtonUp += canvas_MouseLeftButtonDown;
                canvas.GotMouseCapture += canvas_GotMouseCapture;
                canvas.CaptureMouse();
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is IInputElement inputElement)
            {
                this.PointBR.Set(e.GetPosition(inputElement));
            }

            this.ReleaseMouseCapture();
        }

        private void Update()
        {
            //The x-value of our Rectangle should be the minimum between the start x-value and the current x-position.
            Canvas.SetLeft(this, Math.Min(PointTL.X, PointBR.X));

            //same as above x-value. The y-value of our Rectangle should be the minimum between the start y-value and the current y-position.
            Canvas.SetTop(this, Math.Min(PointTL.Y, PointBR.Y));

            //the width of our rectangle should be the maximum between the start x-position and current x-position MINUS.
            rectangle.Width = Math.Abs(PointTL.X - PointBR.X);

            rectangle.Height = Math.Abs(PointTL.Y - PointBR.Y);
        }

        private void canvas_GotMouseCapture(object sender, MouseEventArgs e)
        {
            Point temp = e.GetPosition(this);
            Console.WriteLine(temp.ToString());
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
    }
}
