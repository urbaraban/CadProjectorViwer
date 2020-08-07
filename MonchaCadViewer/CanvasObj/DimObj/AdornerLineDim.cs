using MonchaSDK;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj.DimObj
{
    internal class AdornerLineDim : Adorner
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Constructor

        ////////////////////////////////////////////////////////////////////////////////////////// Public


        #region 

        public AdornerLineDim(UIElement uiElement) : base(uiElement)
        {
            /*
            this.collection = new VisualCollection(uiElement);

            this.line = uiElement as CadLine;

            this.label = new Label();
            this.label.Content = Math.Round(this.line.Lenth, 2).ToString();
            this.label.FontSize = 120;

            this.collection.Add(this.label);*/

        }
        #endregion


        #region OnRender(drawingContext)

        protected override void OnRender(DrawingContext drawingContext)
        {
           /* drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(this.line.BaseContextPoint.X - 25, this.line.BaseContextPoint.Y - 25, 50, 50));
            drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(this.line.SecondContextPoint.X - 25, this.line.SecondContextPoint.Y - 25, 50, 50));*/

            //double theta = Math.Atan2(this.line.SecondContextPoint.Y - this.line.BaseContextPoint.Y, this.line.SecondContextPoint.X - this.line.BaseContextPoint.X);
            //drawingContext.PushTransform(new RotateTransform((180 / Math.PI) * theta, this.line.BaseContextPoint.X, this.line.BaseContextPoint.Y));

          /*  FormattedText text = new FormattedText(Math.Round(this.line.Lenth, 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), (int)MonchaHub.GetThinkess() * 5, Brushes.Gray);

            drawingContext.DrawText(text, new Point(this.line.BaseContextPoint.X + (this.line.SecondContextPoint.X - this.line.BaseContextPoint.X) / 2, this.line.BaseContextPoint.Y + (this.line.SecondContextPoint.Y - this.line.BaseContextPoint.Y) / 2));


            label.Arrange(new Rect(0,0,500,500));*/

        }



        #endregion
    }
}
