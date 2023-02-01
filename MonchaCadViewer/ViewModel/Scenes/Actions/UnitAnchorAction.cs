using CadProjectorSDK.CadObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes.Actions
{
    class UnitAnchorAction : ComplexAction
    {
        public override bool ContinueAction => false;

        private CadAnchor cadAnchor => (CadAnchor)ActionObject;

        public override List<ISceneAction> ActionsList { get; }

        public UnitAnchorAction(CadAnchor cadAnchor)
        {
            ActionsList = new List<ISceneAction>()
            {

            };
        }
    }

    internal class UnionAnchor : ISceneAction
    {
        public Type ActionObjectType => typeof(CadAnchor);
        public object ActionObject { get; private set; }
        public bool ManualEnd => false;
        public bool AutoStart => false;

        public event ISceneAction.ReturnObjDelegate ReturnObj;

        public UnionAnchor(CadAnchor anchor)
        {
            ActionObject = anchor;
        }

        public bool Run(object Obj)
        {
            if (ActionObject is CadAnchor anchor1 && Obj is CadAnchor anchor2)
            {
                return true;
            }
            return false;
        }
    }
}
