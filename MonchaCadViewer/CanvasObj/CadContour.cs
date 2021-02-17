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
        private bool _maincanvas;
        private AdornerContourFrame adornerContour;

        public CadContour(Geometry geometryGroup,  bool maincanvas) 
        {
            this.myGeometry = geometryGroup;
            this._maincanvas = maincanvas;

            this.myPen.Thickness = (MonchaHub.GetThinkess < 0 ? 1 : MonchaHub.GetThinkess) * 0.5;
            this.Loaded += CadContour_Loaded;
        }

        private void CadContour_Loaded(object sender, RoutedEventArgs e)
        {
            if (this._maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
                this.ContextMenuClosing += ViewContour_ContextMenuClosing;
                
            }
        }



        private void ViewContour_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {

        }
    }
}

