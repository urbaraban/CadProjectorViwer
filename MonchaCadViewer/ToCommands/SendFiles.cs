using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands
{
    internal class SendFiles : ToCommand, IToCommand
    {
        public string Name => "FILESLIST";

        public SendFiles(AppMainModel mainModel) : base(mainModel) { }

        public object Run(string message)
        {
            string[] strs = message.Split(':');

            if (strs[0] == this.Name)
            {
                List<FileSystemInfo> items = this.mainModel.WorkFolder.GetFolderItems(strs[1]);
                string paths_concat = string.Empty;
                for (int i = 0; i < items.Count; i += 1)
                {
                    paths_concat = string.Join(Environment.NewLine, items[i].FullName);
                }
                string outmessage = $"{this.Name}:{paths_concat}";

                return this.mainModel.CUTServer.SendMessage(outmessage);
            }
            return false;
        }
    }
}
