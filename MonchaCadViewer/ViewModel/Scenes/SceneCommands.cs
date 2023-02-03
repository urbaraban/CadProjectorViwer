using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CadProjectorViewer.ViewModel.Scenes.Commands;

namespace CadProjectorViewer.ViewModel.Scenes
{
    public partial class SceneModel
    {
        public CommandCollection HistoryCommands { get; } = new CommandCollection();

        private void Obj_InvokeCommand(ISceneCommand sceneCommand)
        {
            HistoryCommands.Add(sceneCommand);
        }

        /// <summary>
        /// Orientation flag for SelecNext void
        /// </summary>
        private bool InverseSelectFlag = false;

        public void SelectNextObject(int step)
        {
            for (int i = 0; i < this.ProjectScene.Count; i += 1)
            {
                if (this.ProjectScene[i] is ProjectorMesh mesh)
                {
                    if (step > 0)
                        mesh.SelectNext();
                    else
                        mesh.SelectPrevious();
                }
                else if (this.ProjectScene[i] is CadGroup cadGroup && cadGroup.Count > 1)
                {
                    for (int k = 0; k < cadGroup.Count; k += 1)
                    {
                        int nextIndex = (k + 1) % cadGroup.Count;
                        if (cadGroup[k].IsRender == true && cadGroup[nextIndex].IsRender == false)
                        {
                            cadGroup[k].IsRender = false;
                            cadGroup[nextIndex].IsRender = true;
                            return;
                        }
                        cadGroup[k].IsRender = false;
                    }
                }
                else if (this.ProjectScene[i] is UidObject cadObject)
                {
                    if (cadObject.IsSelected)
                    {
                        cadObject.Select(false);

                        try
                        {
                            if (this.ProjectScene[i + (InverseSelectFlag ? -step : +step)] is UidObject cadObject2)
                            {
                                cadObject2.Select(true);
                                cadObject2.IsFix = false;
                            }
                        }
                        catch (Exception exeption)
                        {
                            ProjectorHub.Log?.Invoke($"Main: {exeption.Message}", "Scene");
                            InverseSelectFlag = !InverseSelectFlag;
                            cadObject.Select(true);
                        }
                        break;
                    }
                }
            }
        }
    }
}
