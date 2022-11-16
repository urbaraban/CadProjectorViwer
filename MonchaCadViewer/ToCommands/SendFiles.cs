using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands
{
    internal class SendFiles : ToCommand, IToCommand
    {
        public string Name => "FILESLIST";

        public SendFiles(AppMainModel mainModel) : base(mainModel)
        {

        }

        public object Run(string message)
        {
            string[] paths = new string[this.mainModel.WorkFolder.FilInfosCollection.Count];
            for (int i = 0; i < paths.Length; i += 1)
            {
                paths[i] = this.mainModel.WorkFolder.FilInfosCollection.GetItemAt(i).ToString();
            }

            string paths_concat = string.Join(Environment.NewLine, paths);
            string outmessage = $"{this.Name}:{paths_concat}";

            return this.mainModel.CUTServer.SendMessage(outmessage);
        }
    }
}
