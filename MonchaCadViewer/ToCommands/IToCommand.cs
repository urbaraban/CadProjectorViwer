using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands
{
    public interface IToCommand
    {
        public string Name { get; }
        public string Description { get; }
        public bool ReturnRequest { get; }
        public object Run();
        public IToCommand MakeThisCommand(object OperableObj, string message);
    }
}
