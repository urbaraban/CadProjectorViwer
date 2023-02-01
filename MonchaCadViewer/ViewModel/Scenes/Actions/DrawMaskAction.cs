using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static CadProjectorSDK.CadObjects.Interfaces.IMultiply;

namespace CadProjectorViewer.ViewModel.Scenes.Actions
{
    public class DrawMaskAction : ComplexAction
    {
        public override object ActionObject { get; } = new CadRect3D(new CadAnchor(), new CadAnchor(), false);

        private CadRect3D Rect => (CadRect3D)ActionObject;

        public override List<ISceneAction> ActionsList { get; }

        public DrawMaskAction(CadRect3D cadRect3D)
        {
            this.ActionObject = new CadRect3D(new CadAnchor(), new CadAnchor(), false, "Mask")
            {
                Multiply = () => cadRect3D
            };

            ActionsList = new List<ISceneAction>()
            {
                new StartRect(this.Rect),
                new EditAnchor(this.Rect.TR),
                new EndDrawing(this.Rect)
            };
        }
    }

    internal class StartRect : ISceneAction
    {
        public Type ActionObjectType => typeof(Point);
        public object ActionObject { get; private set; }
        public bool ManualEnd => false;
        public bool AutoStart => false;

        public event ISceneAction.ReturnObjDelegate ReturnObj;

        public StartRect(CadRect3D rect3D)
        {
            ActionObject = rect3D;
        }

        public bool Run(object Obj)
        {
            if (ActionObject is CadRect3D rect3D && Obj is Point inPoint)
            {
                rect3D.BL.MX = inPoint.X;
                rect3D.BL.MY = inPoint.Y;
                rect3D.BL.MZ = 0;

                rect3D.TR.MX = inPoint.X;
                rect3D.TR.MY = inPoint.Y;
                rect3D.TR.MZ = 0;

                ReturnObj?.Invoke(ActionObject);
                return true;
            }
            return false;
        }
    }

    internal class EditAnchor : ISceneAction
    {
        public Type ActionObjectType => typeof(Point);
        public object ActionObject { get; private set; }
        public bool ManualEnd => true;
        public bool AutoStart => false;

        public event ISceneAction.ReturnObjDelegate ReturnObj;

        public EditAnchor(CadAnchor cadPoint3D)
        {
            ActionObject = cadPoint3D;
        }

        public bool Run(object Obj)
        {
            if (ActionObject is CadAnchor thisPoint && Obj is Point inPoint)
            {
                thisPoint.MX = inPoint.X;
                thisPoint.MY = inPoint.Y;
                thisPoint.MZ = 0;
                return true;
            }
            return false;
        }
    }
}
