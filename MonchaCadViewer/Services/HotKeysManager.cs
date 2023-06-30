using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CadProjectorViewer.Services
{
    public static class HotKeysManager
    {
        public static List<KeyAction> KeyActions { get; } = new List<KeyAction>();

        public static async void RunAsync(Key[] keys)
        {
            foreach(KeyAction keyAction in KeyActions)
            {
                if (keys.All(s => keyAction.Keys.Contains(s)))
                {
                   await keyAction.GetAction();
                }
            }
        }
    }


    public class KeyAction
    {
        public delegate Task Action();

        public virtual Action GetAction { get; set; }

        public Key[] Keys { get; set; }
    }
}
