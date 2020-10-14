using MonchaSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ToGeometryConverter.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObjectsGroup : Shape
    {
        public List<CadObject> Objects = new List<CadObject>();
        public TransformGroup Transform = new TransformGroup();

        private bool Opened = false;

        private RotateTransform _rotate = new RotateTransform();
        private TranslateTransform _translate = new TranslateTransform();
        private ScaleTransform _scale = new ScaleTransform();

        public CadObjectsGroup(List<Shape> shapes)
        {
            foreach (Shape shape in shapes)
            {
                this.Objects.Add(new CadContour(shape, true, true));
            }
            this.UpdateTransform();
        }

        /*private void CadObjectsGroup_Selected(object sender, CadObject e)
        {
            if (e.IsSelected == true && e.NoEvent == false)
            {
                if (Keyboard.Modifiers != ModifierKeys.Control && Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    foreach (CadObject cadObject in this.Objects)
                    {
                        if (cadObject != e && cadObject.IsSelected == true)
                        {
                            cadObject.IsSelected = false;
                        }
                    }
                }
            }
        }*/

        public void UpdateTransform()
        {
            ScaleTransform scaleTransform = new ScaleTransform();
            RotateTransform rotateTransform = new RotateTransform();
            TranslateTransform translateTransform = new TranslateTransform();

            this.Transform.Children.Clear();
            this.Transform.Children.Add(scaleTransform);
            this.Transform.Children.Add(rotateTransform);
         
            this.Transform.Children.Add(translateTransform);

            Rect rect = this.Bounds;

            scaleTransform.CenterX = rect.X + rect.Width / 2;
            scaleTransform.CenterY = rect.Y + rect.Height / 2;

            rotateTransform.CenterX = rect.X + rect.Width / 2;
            rotateTransform.CenterY = rect.Y + rect.Height / 2;

            translateTransform.X = MonchaHub.Size.GetMPoint.X / 2 - (rect.X + rect.Width / 2);
            translateTransform.Y = MonchaHub.Size.GetMPoint.Y / 2 - (rect.Y + rect.Height / 2);


            foreach (CadObject cadObject in this.Objects)
            {
                cadObject.UpdateTransform(this.Transform);
            }
        }

        public Geometry Data
        {
            get
            {
                return this.DefiningGeometry;
            }
        }

        public System.Windows.Rect Bounds
        {
            get
            {
                GeometryGroup geometryGroup = new GeometryGroup();

                foreach (Shape shape in this.Objects)
                {
                    switch (shape.GetType().Name)
                    {
                        case "Path":
                            Path path = (Path)shape;
                            geometryGroup.Children.Add(path.Data);
                            break;
                        case "NurbsShape":
                            NurbsShape nurbsShape = (NurbsShape)shape;
                            geometryGroup.Children.Add(nurbsShape.Data);
                            break;
                        case "CadContour":
                            CadContour cadContour = (CadContour)shape;
                            geometryGroup.Children.Add(cadContour.Data);
                            break;
                    }
                }
                return geometryGroup.Bounds;
            }
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                GeometryGroup geometry = new GeometryGroup();
                foreach (CadContour cadContour in this.Objects)
                {
                    switch (cadContour.ObjectShape.GetType().Name)
                    {
                        case "Path":
                            Path path = (Path)cadContour.ObjectShape;
                            geometry.Children.Add(path.Data);

                            break;
                        case "NurbsShape":
                            NurbsShape nurbsShape = (NurbsShape)cadContour.ObjectShape;
                            geometry.Children.Add(nurbsShape.Data);
                            break;
                    }
                }

                geometry.Transform = this.Transform;
                return geometry;
            }
        }

    }
}
