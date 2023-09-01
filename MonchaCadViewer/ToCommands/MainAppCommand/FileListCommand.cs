using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace CadProjectorViewer.ToCommands.MainAppCommand
{
    internal class FileListCommand : ToCommand, IToCommand
    {
        public override string Name => "FILESLIST";

        public bool ReturnRequest => true;

        public FileListCommand(object OperableObj, string description) : base(OperableObj, description) { }

        public IToCommand MakeThisCommand(object OperableObj, string description)
        {
            return new FileListCommand(OperableObj, description);
        }
        public object Run()
        {
            if (this.OperableObj is AppMainModel isAppMainModel)
            {
                string path = string.IsNullOrEmpty(this.Description) == false ?
                    this.Description : CadProjectorViewer.Properties.Settings.Default.save_work_folder;
                List<FileSystemInfo> items = isAppMainModel.WorkFolder.GetFolderItems(path);

                string outmessage = $"{this.Name}:";
                foreach (FileSystemInfo item in items)
                {
                    outmessage += $"{Environment.NewLine}{item.FullName}";
                }
                return outmessage;
            }

            return null;
        }


    }
}
