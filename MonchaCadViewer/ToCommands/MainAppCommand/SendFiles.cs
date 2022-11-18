using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands.MainAppCommand
{
    internal class SendFiles : ToCommand, IToCommand
    {
        public string Name => "FILESLIST";

        public SendFiles(object OperableObj, string description) : base(OperableObj, description) { }

        public IToCommand MakeThisCommand(object OperableObj, string description)
        {
            return new SendFiles(OperableObj, description);
        }
        public object Run()
        {
            if (this.OperableObj is AppMainModel isAppMainModel)
            {
                string[] strs = this.Description.Split(':');

                if (strs[0] == this.Name)
                {
                    List<FileSystemInfo> items = isAppMainModel.WorkFolder.GetFolderItems(strs[1]);
                    string paths_concat = string.Empty;
                    for (int i = 0; i < items.Count; i += 1)
                    {
                        paths_concat = string.Join(Environment.NewLine, items[i].FullName);
                    }
                    string outmessage = $"{this.Name}:{paths_concat}";

                    return isAppMainModel.CUTServer.SendMessage(outmessage);
                }
            }

            return false;
        }
    }
}
