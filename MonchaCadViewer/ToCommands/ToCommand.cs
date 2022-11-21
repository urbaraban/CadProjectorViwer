using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ToCommands
{
    internal abstract class ToCommand
    {
        public virtual string Name => "EmptyCommand";
        public virtual string Description { get; } = string.Empty;
        internal object OperableObj { get; }
        public ToCommand(object operableObj, string description)
        {
            this.OperableObj = operableObj;
            this.Description = description;
        }

        public static IToCommand GetToCommand(string Name, IEnumerable<IToCommand> toCommands)
            => toCommands.FirstOrDefault(e => e.Name == Name);

        public static IEnumerable<CommandDummy> ParseDummys(string message)
        {
            foreach(var command in message.Split(new char[] { ';' })) 
            {
                string[] splitstr = command.Split(new char[] {':', ' '});
                string name = splitstr.Length > 0 ? splitstr[0] : string.Empty;
                string description = splitstr.Length > 1 ? splitstr[1] : string.Empty;
                yield return new CommandDummy(name, description);
            }
        }
    }

    internal struct CommandDummy
    {
        public string Name { get; }
        public string Description { get; set; } = string.Empty;

        public CommandDummy(string name)
        {
            Name = name;
        }

        public CommandDummy(string name, string description) : this(name)
        {
            Description = description;
        }
    }
}
