using MonchaSDK;
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

namespace MonchaCadViewer.CanvasObj.DimObj
{
    internal class AdornerLineDim : Adorner
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Constructor

        ////////////////////////////////////////////////////////////////////////////////////////// Public
        private CadLine line;
        private Label label;
        private VisualCollection collection;

        #region 

        public AdornerLineDim(UIElement uiElement) : base(uiElement)
        {

            this.collection = new VisualCollection(uiElement);

            this.line = uiElement as CadLine;

            this.label = new Label();
            this.label.Content = Math.Round(this.line.Lenth, 2).ToString();
            this.label.FontSize = 120;

            this.collection.Add(this.label);

        }
        #endregion


        #region OnRender(drawingContext)

        protected override void OnRender(DrawingContext drawingContext)
        {
           /* drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(this.line.BaseContextPoint.X - 25, this.line.BaseContextPoint.Y - 25, 50, 50));
            drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(this.line.SecondContextPoint.X - 25, this.line.SecondContextPoint.Y - 25, 50, 50));*/

            //double theta = Math.Atan2(this.line.SecondContextPoint.Y - this.line.BaseContextPoint.Y, this.line.SecondContextPoint.X - this.line.BaseContextPoint.X);
            //drawingContext.PushTransform(new RotateTransform((180 / Math.PI) * theta, this.line.BaseContextPoint.X, this.line.BaseContextPoint.Y));

            FormattedText text = new FormattedText(Math.Round(this.line.Lenth, 2).ToString(), new System.Globalization.CultureInfo("ru-RU"), FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), (int)MonchaHub.GetThinkess() * 5, Brushes.Gray);

            drawingContext.DrawText(text, new Point(this.line.BaseContextPoint.X + (this.line.SecondContextPoint.X - this.line.BaseContextPoint.X) / 2, this.line.BaseContextPoint.Y + (this.line.SecondContextPoint.Y - this.line.BaseContextPoint.Y) / 2));


            label.Arrange(new Rect(0,0,500,500));

        }



        #endregion
    }
}
