using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CadProjectorViewer.ViewModel.Scenes.Actions.ISceneAction;

namespace CadProjectorViewer.ViewModel.Scenes.Actions
{
    public abstract class ComplexAction
    {
        public delegate void EndDelegate();

        public event EndDelegate EndEvent;

        public ReturnObjDelegate ReturnObj { get; set; }

        public virtual object Refresh => null;
        public virtual bool ContinueAction => false;

        public virtual object ActionObject => null;

        protected virtual void FillAction(object actionobject)
        {
            throw new NotImplementedException();
        }

        public virtual List<ISceneAction> ActionsList { get; } = new List<ISceneAction>();

        public bool CanAction => AlreadyAction != null;

        private ISceneAction AlreadyAction 
        {
            get
            {
                return _alreadyAction;
            }
            set
            {
                if (_alreadyAction != null) _alreadyAction.ReturnObj -= _alreadyAction_ReturnObj;
                _alreadyAction = value;
                if (_alreadyAction != null)
                {
                    _alreadyAction.ReturnObj += _alreadyAction_ReturnObj;
                    if (_alreadyAction.AutoStart) this.Run(null);
                }
            } 
        }

        private ISceneAction _alreadyAction;

        private void _alreadyAction_ReturnObj(object obj)
        {
            ReturnObj?.Invoke(obj);
        }


        public bool Run(object ActionObj)
        {
            Type ObjType = ActionObj == null ? typeof(Nullable) : ActionObj.GetType();

            if (ObjType == AlreadyAction.ActionObjectType || AlreadyAction.ActionObjectType == null)
            {
                if (AlreadyAction.Run(ActionObj) == true)
                {
                    if (AlreadyAction.ManualEnd == false)
                    {
                        NextAction(null);
                    }
                    return true;
                }
            }

            return false;
        }

        public void NextAction(object Obj)
        {
            int index = ActionsList.IndexOf(AlreadyAction);

            if (index < ActionsList.Count - 1)
            {
                this.AlreadyAction = ActionsList[index + 1];
                if (this.CanAction == true)
                {
                    this.Run(Obj);
                }
            }
            else EndEvent?.Invoke();
        }


        protected void Add(ISceneAction sceneAction)
        {
            ActionsList.Add(sceneAction);
        }
    }
}
