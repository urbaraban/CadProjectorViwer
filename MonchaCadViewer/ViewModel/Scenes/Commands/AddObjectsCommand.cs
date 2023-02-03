using CadProjectorSDK.CadObjects.Abstract;
using System;
using System.Collections.Generic;

namespace CadProjectorViewer.ViewModel.Scenes.Commands
{
    internal class AddObjectsCommand : SceneCommand, ISceneCommand
    {
        private IList<UidObject> uidObjects { get; set; }
        private IList<UidObject> Scene { get; set; }

        public AddObjectsCommand(IList<UidObject> Scene, IList<UidObject> Objects)
        {
            this.Scene = Scene;
            this.uidObjects = Objects;
        }

        public void Run()
        {
            foreach (UidObject uidObject in uidObjects)
            {
                this.Scene.Add(uidObject);
            }
            Status = true;
        }

        public void Undo()
        {
            foreach (UidObject uidObject in uidObjects)
            {
                uidObject.Remove();
            }
            Status = false;
        }
    }
}
