using CadProjectorViewer.EthernetServer.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands
{
    public interface IToCutCommandObject
    {
        public string Name { get; }
        public IToCommand GetCommand(CommandDummy toCommand);
    }
}
