using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (this.OperableObj is AppMainModel isAppMainModel)
            {
                if (File.Exists(this.Description)) 
                {
                    isAppMainModel.OpenGeometryFile(this.Description);
                    return null;
                }
                else if (Directory.Exists(this.Description))
                {
                    return new FileListCommand(this.OperableObj, this.Description);
                }
            }

            return null;
        }

    }
}
