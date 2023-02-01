using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;

namespace CadProjectorViewer.ViewModel.Scenes.Actions
{
    public class DrawLineAction : ComplexAction
    {
        public override object Refresh => new DrawLineAction();

        public override bool ContinueAction => true;

        public override object ActionObject { get; } = new CadLine(new CadPoint3D(), new CadPoint3D());

        private CadLine Line => (CadLine)ActionObject;

        public override List<ISceneAction> ActionsList { get; }

        public DrawLineAction()
        {
            ActionsList = new List<ISceneAction>()
            {
                new StartLine(this.Line),
                new EditAnchor(this.Line.P2),
                new EndDrawing(this.Line)
            };
        }
    }

    internal class StartLine : ISceneAction
    {
        public Type ActionObjectType => typeof(Point);
        public object ActionObject { get; private set; }
        public bool ManualEnd => false;
        public bool AutoStart => false;

        public event ISceneAction.ReturnObjDelegate ReturnObj;

        public StartLine(CadLine cadLine)
        {
            ActionObject = cadLine;
        }

        public bool Run(object Obj)
        {
            if (ActionObject is CadLine cadLine && Obj is Point cadPoint)
            {
                cadLine.P1.Set(cadPoint.X, cadPoint.Y, 0);
                cadLine.P2.Set(cadPoint.X, cadPoint.Y, 0);
                ReturnObj?.Invoke(ActionObject);
                return true;
            }
            return false;
        }
    }

    internal class EndDrawing : ISceneAction
    {
        public Type ActionObjectType => null;
        public object ActionObject { get; }
        public bool ManualEnd => false;
        public bool AutoStart => true;

        public event ISceneAction.ReturnObjDelegate ReturnObj;

        public EndDrawing(IDrawingObject drawingObject)
        {
            ActionObject = drawingObject;
        }

        public bool Run(object Obj)
        {
            if (ActionObject is IDrawingObject drawingObject)
            {
                drawingObject.Init();
                return true;
            }
            return false;
        }
    }
}
