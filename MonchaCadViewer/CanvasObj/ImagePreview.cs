using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
{
    public class ImagePreview : FrameworkElement
    {
        public CadImage CadImage { get; set; }

        public ImagePreview(CadImage image)
        {
            CadImage = image;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawImage(CadImage.imageSource, CadImage.Bounds);
        }
    }
}
