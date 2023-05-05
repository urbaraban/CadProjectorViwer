using System;

namespace CadProjectorViewer.Interfaces
{
    public interface IToRemoveObject
    {
        public delegate void RemoveDelegate(IToRemoveObject toRemoveObject);
        public Guid Guid { get; }

        public RemoveDelegate Remove { get; set; }
    }
}
