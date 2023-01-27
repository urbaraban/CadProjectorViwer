using CadProjectorSDK.CadObjects;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CadProjectorViewer.CanvasObj.CanvasObject;
using Rect = System.Windows.Rect;

namespace CadProjectorViewer.ViewModel.CanvasObj
{
    internal interface IAnchoredObject
    {
        public event EventHandler UpdateAnchorPoints;
        public IEnumerable<CadAnchor> Anchors { get; }
        public GetViewDelegate GetViewModel { get; set; }
        public Rect Bounds { get; }
    }
}
