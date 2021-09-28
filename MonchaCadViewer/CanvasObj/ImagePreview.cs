using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
{
    public class ImagePreview : CanvasObject
    {
        public IPixelObject CadImage 
        {
            get => (IPixelObject)CadObject;
        }

        public ImagePreview(UidObject image) : base(true)
        {
            CadObject = image;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            ImageSource imageSource = this.CadImage.GetImage();

            drawingContext.DrawImage(imageSource,
                new Rect(
                    this.Translate.OffsetX, 
                    this.Translate.OffsetY,
                    imageSource.Width,
                    imageSource.Height));
        }
    }
}
