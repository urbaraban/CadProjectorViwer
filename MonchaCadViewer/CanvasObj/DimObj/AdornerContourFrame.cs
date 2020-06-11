using Kompas6API7;
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

        private CadContour Contour;

        private VisualCollection visualChildren;

        // The center of the strokes.
        Point center;

        public event EventHandler<double> AngleChange;

        public RotateTransform Rotation;

        // The bounds of the Strokes;
        private Rect strokeBounds = Rect.Empty;

        private Pen LinePen;
        private double Textsize;

        public AdornerContourFrame(UIElement adornedElement)
            : base(adornedElement)
        {
            this.Contour = adornedElement as CadContour;
            this.IsClipEnabled = true;
            visualChildren = new VisualCollection(this);
            rotateHandle = new Thumb();
            rotateHandle.Cursor = Cursors.SizeNWSE;
            rotateHandle.Width = MonchaHub.GetThinkess() * 5;
            rotateHandle.Height = MonchaHub.GetThinkess() * 5;
            rotateHandle.Background = Brushes.Blue;

            rotateHandle.DragDelta += new DragDeltaEventHandler(rotateHandle_DragDelta);
            rotateHandle.DragCompleted += new DragCompletedEventHandler(rotateHandle_DragCompleted);

            outline = new Path();
            outline.Stroke = Brushes.Gray;
            outline.StrokeThickness = MonchaHub.GetThinkess() / 2;

 
            visualChildren.Add(outline);
            visualChildren.Add(rotateHandle);

            strokeBounds = this.Contour.Transform.TransformBounds(this.Contour.GmtrObj.Bounds);
            LinePen = new Pen(Brushes.LightGray, MonchaHub.GetThinkess() / 2);
            Textsize = MonchaHub.GetThinkess() * 5 + 1;
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
            this.Rotation = new RotateTransform(angle, center.X, center.Y);
            outline.RenderTransform = this.Rotation;

            if (AngleChange != null)
                AngleChange(this, angle);

            //this._adornedElement.UpdateGeometry(true);
        }

        /// <summary>
        /// Rotates the strokes to the same angle as outline.
        /// </summary>
        void rotateHandle_DragCompleted(object sender,
                                        DragCompletedEventArgs e)
        {
            if (this.Rotation == null)
            {
                return;
            }
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
                                                    MonchaHub.GetThinkess() * 7),
                                  strokeBounds.Width, strokeBounds.Height);

            if (this.Rotation != null)
            {
                handleRect.Transform(this.Rotation.Value);
            }

            // Draws the thumb and the rectangle around the strokes.
            rotateHandle.Arrange(handleRect);
            outline.Data = new RectangleGeometry(strokeBounds);
            outline.Arrange(new Rect(Contour.Size));

            /*
            //Line side to contour
            //Top 
            DrawMarginLine(new Point(0, -Contour.BaseContextPoint.GetMPoint.Y), 
            new Point(0, -outline.ActualHeight / 2),
            new Point(0, -Contour.BaseContextPoint.GetMPoint.Y));
            //Bottom
            DrawMarginLine(new Point(0, MonchaHub.Size.GetMPoint.Y - Contour.BaseContextPoint.GetMPoint.Y),
            new Point(0, outline.ActualHeight / 2),
            new Point(0, MonchaHub.Size.GetMPoint.Y - Contour.BaseContextPoint.GetMPoint.Y - Textsize * 2));
            //Left
            DrawMarginLine(new Point(-Contour.BaseContextPoint.GetMPoint.X, 0),
            new Point(-outline.ActualWidth / 2, 0),
            new Point(-Contour.BaseContextPoint.GetMPoint.X, 0));
            //Right
            DrawMarginLine(new Point(MonchaHub.Size.GetMPoint.X - Contour.BaseContextPoint.GetMPoint.X, 0),
            new Point(outline.ActualWidth / 2, 0),
            new Point(MonchaHub.Size.GetMPoint.X - Contour.BaseContextPoint.GetMPoint.X -Textsize * 2, 0));
            */

            //width

            drawingContext.DrawText(new FormattedText("X:" + Math.Round(Math.Round(outline.ActualWidth), 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), (int)Textsize, Brushes.Gray), 
            new Point(outline.Data.Bounds.Location.X + outline.Data.Bounds.Width / 2, outline.Data.Bounds.Location.Y + outline.Data.Bounds.Height / 2 - Textsize * 1.5));

            //height
            drawingContext.DrawText(new FormattedText("Y:" + Math.Round(Math.Round(outline.ActualHeight), 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
           new Typeface("Segoe UI"), (int)Textsize, Brushes.Gray), 
           new Point(outline.Data.Bounds.Location.X + outline.Data.Bounds.Width / 2, outline.Data.Bounds.Location.Y + outline.Data.Bounds.Height / 2));

            //angle
            if (this.Rotation != null)
            {
                drawingContext.DrawText(new FormattedText("a:" + Math.Round(Math.Round(this.Rotation.Angle), 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), 
                    FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 
            (int)Textsize, Brushes.Gray), 
            new Point(outline.Data.Bounds.Location.X + outline.Data.Bounds.Width / 2, outline.Data.Bounds.Location.Y + outline.Data.Bounds.Height / 2 + Textsize));
            }
            void DrawMarginLine(Point point1, Point point2, Point TextPoint)
            {
                drawingContext.DrawLine(LinePen, point1, point2);
                //text
                drawingContext.DrawText(
                    new FormattedText(Math.Round(Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2)), 2).ToString(),
                    new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), (int)Textsize, Brushes.Gray),
                TextPoint);
            }
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
