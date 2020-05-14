using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonchaCadViewer.CanvasObj
{
    internal class AdornerFrame : Adorner
    {
        // Be sure to call the base class constructor.

        // The Thumb to drag to rotate the strokes.
        Thumb rotateHandle;

        // The surrounding boarder.
        Path outline;

        // Size label
        ContentPresenter labelwidth;
        ContentPresenter labelheight;



        VisualCollection visualChildren;

        // The center of the strokes.
        Point center;

        public event EventHandler<double> AngleChange;

        RotateTransform rotation;

        private int HANDLEMARGIN = 10;

        // The bounds of the Strokes;
        Rect strokeBounds = Rect.Empty;

        public AdornerFrame(UIElement adornedElement, CadCanvas canvas)
            : base(adornedElement)
        {
            this.ClipToBounds = false;
            this.HANDLEMARGIN = (int)(Math.Min(canvas.ActualWidth, canvas.ActualHeight) * 0.05);

            visualChildren = new VisualCollection(this);
            rotateHandle = new Thumb();
            rotateHandle.Cursor = Cursors.SizeNWSE;
            rotateHandle.Width = HANDLEMARGIN;
            rotateHandle.Height = HANDLEMARGIN;
            rotateHandle.Background = Brushes.Blue;

            rotateHandle.DragDelta += new DragDeltaEventHandler(rotateHandle_DragDelta);
            rotateHandle.DragCompleted += new DragCompletedEventHandler(rotateHandle_DragCompleted);

            outline = new Path();
            outline.Stroke = Brushes.Blue;
            outline.StrokeThickness = adornedElement.DesiredSize.Width * 0.03;

 
            visualChildren.Add(outline);
            visualChildren.Add(rotateHandle);
            visualChildren.Add(labelwidth);
            visualChildren.Add(labelheight);

            strokeBounds = new Rect(-AdornedStrokes.DesiredSize.Width * 0.89, - AdornedStrokes.DesiredSize.Height * 0.89, AdornedStrokes.DesiredSize.Width * 1.89, AdornedStrokes.DesiredSize.Height * 1.89);

        }

        /// <summary>
        /// Draw the rotation handle and the outline of
        /// the element.
        /// </summary>
        /// <param name="finalSize">The final area within the 
        /// parent that this element should use to arrange 
        /// itself and its children.</param>
        /// <returns>The actual size used. </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (strokeBounds.IsEmpty)
            {
                return finalSize;
            }

            center = new Point(strokeBounds.X + strokeBounds.Width / 2,
                               strokeBounds.Y + strokeBounds.Height / 2);

            // The rectangle that determines the position of the Thumb.
            Rect handleRect = new Rect(strokeBounds.X,
                                  strokeBounds.Y - (strokeBounds.Height / 2 +
                                                    HANDLEMARGIN),
                                  strokeBounds.Width, strokeBounds.Height);

            if (rotation != null)
            {
                handleRect.Transform(rotation.Value);
            }

            // Draws the thumb and the rectangle around the strokes.
            rotateHandle.Arrange(handleRect);
            outline.Data = new RectangleGeometry(strokeBounds);
            outline.Arrange(new Rect(finalSize));
            return finalSize;
        }

        /// <summary>
        /// Rotates the rectangle representing the
        /// strokes' bounds as the user drags the
        /// Thumb.
        /// </summary>
        void rotateHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // Find the angle of which to rotate the shape.  Use the right
            // triangle that uses the center and the mouse's position 
            // as vertices for the hypotenuse.

            Point pos = Mouse.GetPosition(this);

            double deltaX = pos.X - center.X;
            double deltaY = pos.Y - center.Y;

            if (deltaY.Equals(0))
            {

                return;
            }

            double tan = deltaX / deltaY;
            double angle = Math.Atan(tan);

            // Convert to degrees.
            angle = angle * 180 / Math.PI;

            // If the mouse crosses the vertical center, 
            // find the complementary angle.
            if (deltaY > 0)
            {
                angle = 180 - Math.Abs(angle);
            }

            // Rotate left if the mouse moves left and right
            // if the mouse moves right.
            if (deltaX < 0)
            {
                angle = -Math.Abs(angle);
            }
            else
            {
                angle = Math.Abs(angle);
            }

            if (Double.IsNaN(angle))
            {
                return;
            }

            int mult = (Keyboard.Modifiers == ModifierKeys.Shift ? 5 : 1);

            angle = (int)(angle / mult) * mult;

            // Apply the rotation to the strokes' outline.
            rotation = new RotateTransform(angle, center.X, center.Y);
            outline.RenderTransform = rotation;

            if (AngleChange != null)
                AngleChange(this, angle);

        }

        /// <summary>
        /// Rotates the strokes to the same angle as outline.
        /// </summary>
        void rotateHandle_DragCompleted(object sender,
                                        DragCompletedEventArgs e)
        {
            if (rotation == null)
            {
                return;
            }


            // Rotate the strokes to match the new angle.
           /* Matrix mat = new Matrix();
            mat.RotateAt(rotation.Angle - lastAngle, center.X, center.Y);
            AdornedStrokes.RenderTransform = new MatrixTransform(mat);

            // Save the angle of the last rotation.
            lastAngle = rotation.Angle;*/

            // Redraw rotateHandle.
            this.InvalidateArrange();
        }

        /// <summary>
        /// Gets the strokes of the adorned element 
        /// (in this case, an InkPresenter).
        /// </summary>
        private UIElement AdornedStrokes
        {
            get
            {
                return ((UIElement)AdornedElement);
            }
        }

        // Override the VisualChildrenCount and 
        // GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override int VisualChildrenCount
        {
            get { return visualChildren.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visualChildren[index];
        }
    }
}
