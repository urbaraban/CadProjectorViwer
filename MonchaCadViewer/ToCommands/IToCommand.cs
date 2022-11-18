using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands
{
    internal interface IToCommand
    {
        public string Name { get; }
        public string Description { get; }
        public object Run();
        public IToCommand MakeThisCommand(object OperableObj, string message);
    }
}
