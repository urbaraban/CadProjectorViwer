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

        public bool ReturnRequest => true;

        public SendFiles(object OperableObj, string description) : base(OperableObj, description) { }

        public IToCommand MakeThisCommand(object OperableObj, string description)
        {
            return new SendFiles(OperableObj, description);
        }
        public object Run()
        {
            if (this.OperableObj is AppMainModel isAppMainModel)
            {
                string path = string.IsNullOrEmpty(this.Description) == false ?
                    this.Description : CadProjectorViewer.Properties.Settings.Default.save_work_folder;
                List<FileSystemInfo> items = isAppMainModel.WorkFolder.GetFolderItems(path);

                return items;
            }

            return null;
        }

        public string GetRequestMessage(object obj)
        {
            string outmessage = $"{this.Name}:";
            if (obj is List<FileSystemInfo> list)
            {
                foreach (FileSystemInfo item in list)
                {
                    outmessage += $"{Environment.NewLine}{item.FullName}";
                }
            }
            return outmessage;
        }
    }
}
