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

                this.ContextMenuClosing += ViewContour_ContextMenuClosing;
                this.MouseWheel += CadContour_MouseWheel;
            }

            this.Fill = Brushes.Transparent;
            this.StrokeThickness = (MonchaHub.GetThinkess() < 0 ? 1 : MonchaHub.GetThinkess()) * 0.5;
            this.Stroke = Brushes.Blue;
        }


        private void CadContour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((e.Delta != 0) && (Keyboard.Modifiers != ModifierKeys.Control))
            {
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
    }
}

