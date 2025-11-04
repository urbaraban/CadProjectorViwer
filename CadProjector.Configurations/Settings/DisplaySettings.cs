using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjector.Configurations
{
    public class DisplaySettings : INotifyPropertyChanged
    {
        private bool showGrid = true;
     public bool ShowGrid
        {
            get => showGrid;
   set
  {
   showGrid = value;
     OnPropertyChanged();
   }
  }

        private bool showHiddenObjects = false;
        public bool ShowHiddenObjects
 {
         get => showHiddenObjects;
  set
            {
  showHiddenObjects = value;
           OnPropertyChanged();
   }
        }

        private double lineThickness = 1.0;
        public double LineThickness
    {
        get => lineThickness;
            set
         {
           lineThickness = value;
      OnPropertyChanged();
         }
        }

        private string defaultSaveFolder = "Saves";
        public string DefaultSaveFolder
        {
        get => defaultSaveFolder;
            set
            {
     defaultSaveFolder = value;
   OnPropertyChanged();
       }
        }

        private bool autoSave = true;
     public bool AutoSave
        {
        get => autoSave;
            set
            {
                autoSave = value;
       OnPropertyChanged();
    }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}