using Kompas6API7;
using MonchaSDK;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj.DimObj
{
    class AdornerContourFrame : Adorner
    {
        private CadGeometry Contour;

        public AdornerContourFrame(UIElement adornedElement, Visibility visibility)
            : base(adornedElement)
        {
            this.RenderTransform = adornedElement.RenderTransform;
            this.Contour = (CadGeometry)adornedElement;
            this.Visibility = visibility;
        }

        // Override the VisualChildrenCount and 
        // GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override void OnRender(DrawingContext drawingContext)
        {

            // The rectangle that determines the position of the Thumb.
            Rect handleRect = Contour.Bounds;

            drawingContext.DrawRectangle(Brushes.Gray, null, Contour.Bounds);

            /*
            // Draws the thumb and the rectangle around the strokes.
            // rotateHandle.Arrange(handleRect);
            this.Draving = new RectangleGeometry(strokeBounds);
            if (Contour.Bounds.Size != Size.Empty)
                outline.Arrange(new Rect(Contour.Bounds.Size));

            //Line side to contour
            //Top 
            DrawMarginLine(this.Contour.GmtrObj.Bounds.TopLeft, 
            this.Contour.GmtrObj.Bounds.TopRight,
            new Point ((this.Contour.GmtrObj.Bounds.TopLeft.X + this.Contour.GmtrObj.Bounds.TopRight.X) / 2, this.Contour.GmtrObj.Bounds.TopLeft.Y - MonchaHub.GetThinkess() * 4));
            //Right
            DrawMarginLine(this.Contour.GmtrObj.Bounds.TopRight,
            this.Contour.GmtrObj.Bounds.BottomRight,
            this.Contour.GmtrObj.Bounds.BottomRight);
            //Bottom
            DrawMarginLine(this.Contour.GmtrObj.Bounds.BottomRight,
            this.Contour.GmtrObj.Bounds.BottomLeft,
            this.Contour.GmtrObj.Bounds.BottomRight);
            //Left
            DrawMarginLine(this.Contour.GmtrObj.Bounds.BottomLeft,
            this.Contour.GmtrObj.Bounds.TopLeft,
            this.Contour.GmtrObj.Bounds.BottomRight);

            //width
            
            drawingContext.DrawText(new FormattedText("X:" + Math.Round(Math.Round(outline.ActualWidth), 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), (int)Textsize, Brushes.Gray), 
            new Point(outline.Data.Bounds.Location.X + outline.Data.Bounds.Width / 2, outline.Data.Bounds.Location.Y + outline.Data.Bounds.Height / 2 - Textsize * 1.5));

            //height
            drawingContext.DrawText(new FormattedText("Y:" + Math.Round(Math.Round(outline.ActualHeight), 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
           new Typeface("Segoe UI"), (int)Textsize, Brushes.Gray), 
           new Point(outline.Data.Bounds.Location.X + outline.Data.Bounds.Width / 2, outline.Data.Bounds.Location.Y + outline.Data.Bounds.Height / 2));
            */
            //angle

            /*void DrawMarginLine(Point point1, Point point2, Point TextPoint)
            {
                drawingContext.DrawLine(LinePen, point1, point2);
                //text
                drawingContext.DrawText(
                    new FormattedText(Math.Round(Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2)), 2).ToString(),
                    new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), (int)Textsize, Brushes.Gray),
                TextPoint);
            }*/
        }
    }
}
