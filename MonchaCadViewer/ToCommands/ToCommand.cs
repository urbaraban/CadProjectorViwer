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
        internal AppMainModel mainModel { get; }
        public ToCommand(AppMainModel appMainModel)
        {
            this.mainModel = appMainModel;
        }

        public static void RunCommands(string message, IEnumerable<IToCommand> commands)
        {
            foreach (string substring in message.Split(';'))
            {
                foreach(IToCommand command in commands)
                {
                    if (command.Name == substring.Split(':')[0])
                    {
                        command.Run(substring.Split(':')[1]);
                    }
                }
            }
        }
    }
}
