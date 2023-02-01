using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.Scenes;
using CadProjectorSDK;
using CadProjectorViewer.StaticTools;
using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using CadProjectorViewer.ViewModel.Scenes;

namespace CadProjectorViewer.ToCommands.MainAppCommand
{
    internal class FilePathCommand : ToCommand, IToCommand
    {
        public string Name => "FILEPATH";

        public bool ReturnRequest => false;

        public FilePathCommand(object OperableObj, string description) : base(OperableObj, description) { }

        public IToCommand MakeThisCommand(object OperableObj, string description)
        {
            return new FilePathCommand(OperableObj, description);
        }

        public object Run()
        {
            if (this.OperableObj is SceneModel sceneModel)
            {
                if (sceneModel.PathLoad(this.Description) == false)
                {
                    return new CommandDummy("FILESLIST", this.Description);
                }
            }
            return null;
        }
    }
}
