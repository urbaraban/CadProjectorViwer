using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Render;
using CadProjectorViewer.CanvasObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CadProjectorViewer.ViewModel.CanvasObj
{
    internal class VectorPreview : CanvasObject
    {
        LProjector _projector;

        public VectorPreview(UidObject uidObject, LProjector projector) : base(uidObject, true)
        {
            this._projector = projector;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Drawing(this.CadObject, this.IsSelected, this.IsMouseOver, true, StrokeThinkess, drawingContext);
        }

        private void Drawing(UidObject uidObject, bool IsSelected, bool MouseOver, bool ParentRender, double StrThink, DrawingContext drawingContext)
        {
            if (uidObject.Renders.ContainsKey(this._projector))
            {
                if (uidObject is CadGroup group)
                {
                    foreach (UidObject uid in group)
                    {
                        Drawing(uid, IsSelected, MouseOver, ParentRender && uid.IsRender, StrThink, drawingContext);
                    }
                }
                else if (uidObject.Renders[_projector] is VectorLinesCollection linesCollection)
                {
                    Pen pen = GetPen(
                        StrThink,
                        MouseOver,
                        IsSelected,
                        ParentRender,
                        uidObject.IsBlank,
                        _projector.ProjectionSetting.GetBrush);

                    Brush brush = GetBrush(uidObject);

                    foreach (VectorLine line in linesCollection)
                    {
                        drawingContext.DrawLine(pen, new Point(line.P1.X, line.P1.Y), new Point(line.P2.X, line.P2.Y));
                    }
                }
            }
        }
    }
}
