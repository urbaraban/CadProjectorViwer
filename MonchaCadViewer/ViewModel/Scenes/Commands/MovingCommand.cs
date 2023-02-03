using CadProjectorSDK.CadObjects.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes.Commands
{
    public class MovingCommand : SceneCommand, ISceneCommand
    {
        private readonly double dX, dY, dZ;

        private readonly IList<UidObject> Objects;

        public MovingCommand(IList<UidObject> uidObjects, double dX, double dY, double dZ = 0)
        {
            this.dX = dX;
            this.dY = dY;
            this.dZ = dZ;
            this.Objects = uidObjects;
        }

        public MovingCommand(UidObject uidObject, double dX, double dY, double dZ = 0) 
            : this(new List<UidObject>() { uidObject }, dX, dY, dZ)
        {

        }

        public void Run()
        {
            if (Status == false)
            {
                foreach (UidObject uidObject in Objects)
                {
                    uidObject.MX += dX;
                    uidObject.MY += dY;
                    uidObject.MZ += dZ;
                }
                Status = true;
            }
        }

        public void Undo()
        {
            if (Status == true)
            {
                foreach (UidObject uidObject in Objects)
                {
                    uidObject.MX -= dX;
                    uidObject.MY -= dY;
                    uidObject.MZ -= dZ;
                }
                Status = false;
            }
        }
    }
}
