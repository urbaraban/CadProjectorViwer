using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes.Commands
{
    public interface ISceneCommand
    {
        public delegate void CommandDelegate(ISceneCommand command);

        public bool Status { get; set; }
        public void Run();
        public void Undo();
    }
}
