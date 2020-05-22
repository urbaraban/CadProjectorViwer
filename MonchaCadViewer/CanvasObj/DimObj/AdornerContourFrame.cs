using MonchaSDK;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonchaCadViewer.CanvasObj.DimObj
{
    class AdornerContourFrame : Adorner
    {
        // Be sure to call the base class constructor.

        // The Thumb to drag to rotate the strokes.
        private Thumb rotateHandle;

        // The surrounding boarder.
        private Path outline;

        private CadContour _adornedElement;

        private VisualCollection visualChildren;

        // The center of the strokes.
        Point center;

        public event EventHandler<double> AngleChange;

        RotateTransform rotation;

        private int HANDLEMARGIN = 10;

        // The bounds of the Strokes;
        Rect strokeBounds = Rect.Empty;

        public AdornerContourFrame(UIElement adornedElement, CadCanvas canvas)
            : base(adornedElement)
        {
            this._adornedElement = adornedElement as CadContour;
            this.ClipToBounds = false;
            this.HANDLEMARGIN = MonchaHub.GetThinkess() * 3;

            visualChildren = new VisualCollection(this);
            rotateHandle = new Thumb();
            rotateHandle.Cursor = Cursors.SizeNWSE;
            rotateHandle.Width = HANDLEMARGIN;
            rotateHandle.Height = HANDLEMARGIN;
            rotateHandle.Background = Brushes.Blue;

            rotateHandle.DragDelta += new DragDeltaEventHandler(rotateHandle_DragDelta);
            rotateHandle.DragCompleted += new DragCompletedEventHandler(rotateHandle_DragCompleted);

            outline = new Path();
            outline.Stroke = Brushes.Gray;
            outline.StrokeThickness = MonchaHub.GetThinkess() / 5;

 
            visualChildren.Add(outline);
            visualChildren.Add(rotateHandle);

            strokeBounds = new Rect(-AdornedStrokes.DesiredSize.Width, - AdornedStrokes.DesiredSize.Height, AdornedStrokes.DesiredSize.Width * 2, AdornedStrokes.DesiredSize.Height * 2);

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

            //this._adornedElement.UpdateGeometry(true);
        }

        public void Rotate(double angle)
        {
            rotation = new RotateTransform(angle, center.X, center.Y);
            outline.RenderTransform = rotation;
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
        protected override void OnRender(DrawingContext drawingContext)
        {

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
            outline.Arrange(new Rect(_adornedElement.Size));
        }


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
