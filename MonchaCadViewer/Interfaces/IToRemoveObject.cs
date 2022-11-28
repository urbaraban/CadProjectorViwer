using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.Interfaces
{
    public interface IToRemoveObject
    {
        public delegate void RemoveDelegate(IToRemoveObject toRemoveObject);
        public Guid Guid { get; }

        public RemoveDelegate Remove { get; set; }
    }
}
