using CadProjectorViewer.ViewModel.Scene;

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
