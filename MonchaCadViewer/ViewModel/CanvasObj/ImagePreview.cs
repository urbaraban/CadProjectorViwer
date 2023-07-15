using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects.Interfaces;
using System.Windows.Media;

namespace CadProjectorViewer.CanvasObj
{
    internal class ImagePreview : CanvasObject
    {
        public IPixelObject CadImage 
        {
            get => (IPixelObject)CadObject;
        }

        public ImagePreview(UidObject image) : base(image, true)
        {

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

/*            BitmapSource imageSource = this.CadImage.GetImage();

            drawingContext.DrawImage(imageSource,
                new Rect(
                    this.CadObject.Translate.OffsetX, 
                    this.CadObject.Translate.OffsetY,
                    imageSource.Width,
                    imageSource.Height));*/
        }
    }
}
