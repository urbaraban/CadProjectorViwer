using CadProjectorSDK.CadObjects.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes.Actions
{
    public interface ISceneAction
    {
        public delegate void ReturnObjDelegate(object obj);

        public event ReturnObjDelegate ReturnObj;

        public Type ActionObjectType { get; }
        public object ActionObject { get; }

        public bool ManualEnd { get; }
        public bool AutoStart { get; }
        public bool Run(object Obj);
    }
}
