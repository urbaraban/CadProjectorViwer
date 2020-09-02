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

namespace MonchaCadViewer.CanvasObj
{
    public class CadContour : CadObject
    {
        public double CRS => ReadyFrame.CRS.MX; // / Math.Max(this.ScaleX, this.ScaleY);

        private bool _maincanvas;
        private AdornerContourFrame adornerContour;

        public CadContour(PathGeometry Path, bool maincanvas, bool Capturemouse) : base (Capturemouse, false, Path)
        {
           
            this._maincanvas = maincanvas;
            
            if (this._maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
                this.MouseLeave += Contour_MouseLeave;
                this.MouseLeftButtonUp += Contour_MouseLeftUp;
                this.Loaded += ViewContour_Loaded;
                this.ContextMenuClosing += ViewContour_ContextMenuClosing;
                this.MouseWheel += CadContour_MouseWheel;
            }

            this.Fill = Brushes.Transparent;
            this.StrokeThickness = (MonchaHub.GetThinkess() < 0 ? 1 : MonchaHub.GetThinkess()) * 0.5;
            this.Stroke = Brushes.Red;
        }


        private void CadContour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.Angle += Math.Abs(e.Delta)/e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 1 : 5);
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


        private void Contour_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (this.WasMove)
            {
                this.ReleaseMouseCapture();
                this.WasMove = false;
            }
            else
            {

            }
            
        }

        private void Contour_MouseLeave(object sender, MouseEventArgs e)
        {
            this.ReleaseMouseCapture();
        }
   }
}

