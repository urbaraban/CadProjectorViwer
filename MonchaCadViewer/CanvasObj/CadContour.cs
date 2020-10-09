using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonchaCadViewer.CanvasObj
{
    public class CadContour : CadObject
    {
        private bool _maincanvas;
        private AdornerContourFrame adornerContour;

        public CadContour(Shape Shapes,  bool maincanvas, bool Capturemouse) : base(Capturemouse, false)
        {
            this.ObjectShape = Shapes;
            this._maincanvas = maincanvas;

            if (this._maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
      
                this.Loaded += ViewContour_Loaded;
                this.ContextMenuClosing += ViewContour_ContextMenuClosing;
                this.MouseWheel += CadContour_MouseWheel;
            }

            this.Fill = null;
            this.StrokeThickness = (MonchaHub.GetThinkess() < 0 ? 1 : MonchaHub.GetThinkess()) * 0.5;
            this.Stroke = Brushes.Blue;
        }


        private void CadContour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                Point point = this.RenderedGeometry.Bounds.TopLeft;
                this.Rotate.CenterX = this.Translate.X;
                this.Rotate.CenterY = this.Translate.Y;
                this.Angle += Math.Abs(e.Delta) / e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
            }
        }


        private void ViewContour_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Header)
                {
                    case "Mirror":
                        this.Mirror = !this.Mirror;
                        break;

                    case "Fix":
                        this.IsFix = !this.IsFix;
                        break;

                    case "Remove":
                        this.Remove();
                        break;

                    case "Render":
                        this.Render = !this.Render;
                        break;
                }
            }
        }

        private void ViewContour_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas && this._maincanvas)
            {
                this.adornerLayer = AdornerLayer.GetAdornerLayer(canvas);

                this.ObjAdorner = new AdornerContourFrame(this);
                this.ObjAdorner.Visibility = Visibility.Hidden;
                this.ObjAdorner.DataContext = this;

                adornerLayer.Add(this.ObjAdorner);
                this.adornerContour = this.ObjAdorner as AdornerContourFrame;
                //this.adornerContour.Rotation = this.Rotate;
            }
        }

    }
}

