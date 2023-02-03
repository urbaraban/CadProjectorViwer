using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Device.Mesh;
using StclLibrary.Video.VideoThread.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes.Commands
{
    public class SelectNextCommand : SceneCommand, ISceneCommand
    {
        private readonly bool forward = true;

        private readonly IList<UidObject> Objects;

        public SelectNextCommand(bool forward, IList<UidObject> objects)
        {
            this.forward = forward;
            Objects = objects;
        }

        public void Run() => SelectNext(this.forward, this.Objects);

        public void Undo() => SelectNext(!this.forward, this.Objects);

        private static void SelectNext(bool forward, IEnumerable<UidObject> uidObjects)
        {
            foreach (UidObject obj in uidObjects)
            {
                if (obj is ProjectorMesh mesh)
                {
                    if (forward == true)
                        mesh.SelectNext();
                    else
                        mesh.SelectPrevious();
                }
                else if (obj is CadGroup cadGroup && cadGroup.Count > 1)
                {
                    bool toggle = false;
                    for (int k = 0; k < cadGroup.Count && toggle == false; k += 1)
                    {
                        if (cadGroup[k].IsRender == true)
                        {
                            int nextIndex = (cadGroup.Count + k + (forward == true ? +1 : -1)) % cadGroup.Count;
                            if (cadGroup[nextIndex].IsRender == false)
                            {
                                cadGroup[k].IsRender = false;
                                cadGroup[nextIndex].IsRender = true;
                                toggle = true;
                            }
                        }
                        cadGroup[k].IsRender = false;
                    }
                }
            }
        }
    }
}
