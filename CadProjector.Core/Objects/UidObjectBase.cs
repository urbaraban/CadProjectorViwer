using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjector.Core.Objects
{
    public abstract class UidObjectBase : IUidObject, INotifyPropertyChanged
    {
        public Guid Uid { get; } = Guid.NewGuid();
        
     private string nameId = string.Empty;
        public string NameID 
     { 
            get => nameId;
      set
      {
                nameId = value;
          OnPropertyChanged();
            }
 }

        private double mx;
        public double MX 
        { 
       get => mx;
         set
      {
     mx = value;
      OnPropertyChanged();
            }
        }

        private double my;
  public double MY 
        { 
    get => my;
    set
            {
           my = value;
       OnPropertyChanged();
  }
        }

        private double mz;
     public double MZ 
        { 
            get => mz;
       set
            {
    mz = value;
        OnPropertyChanged();
          }
    }

        private bool isSelected;
        public bool IsSelected 
        { 
   get => isSelected;
   set
            {
    isSelected = value;
                OnPropertyChanged();
  }
        }

        private bool isRender = true;
        public bool IsRender 
      { 
   get => isRender;
            set
 {
         isRender = value;
           OnPropertyChanged();
            }
    }

        private bool isBlank;
   public bool IsBlank 
        { 
  get => isBlank;
      set
        {
  isBlank = value;
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