using CadProjectorSDK.Render;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CadProjectorViewer.ViewModel
{
    internal class NotifyFrameworkElement : FrameworkElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            this.Update();
        }

        public virtual void Update()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.InvalidateVisual();
            });
        }

        protected void DrawingIRenderableObjects(IEnumerable<IRenderedObject> objects, DrawingContext drawingContext, RenderDeviceModel renderDevice, Brush brush, Pen pen)
        {
            foreach (IRenderedObject obj in objects)
            {
                if (obj is LinesCollection vectorLines)
                {
                    DrawVectorLines(vectorLines, drawingContext, renderDevice, brush, pen);
                }
            }
        }

        protected void DrawVectorLines(LinesCollection vectorLines, DrawingContext drawingContext, RenderDeviceModel renderDevice, Brush brush, Pen pen)
        {
            if (vectorLines.Count > 0)
            {
                StreamGeometry streamGeometry = new StreamGeometry();
                streamGeometry.FillRule = FillRule.EvenOdd;

                using (StreamGeometryContext ctx = streamGeometry.Open())
                {
                    for (int i = 0; i < vectorLines.Count; i += 1)
                    {
                        Point point = renderDevice.GetPoint(vectorLines[i].P1.X, vectorLines[i].P1.Y);
                        ctx.BeginFigure(point, vectorLines.IsClosed, vectorLines.IsClosed);

                        Point point_second = renderDevice.GetPoint(vectorLines[i].P2.X, vectorLines[i].P2.Y);
                        ctx.LineTo(point_second, true, true);
                    }
                    ctx.Close();
                }
                drawingContext.DrawGeometry(brush, pen, streamGeometry);
            }
        }
    }
}
