using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CadProjector.Configurations
{
  public class ProjectorSettings : INotifyPropertyChanged
    {
   private string ipAddress = "127.0.0.1";
        public string IpAddress 
        { 
     get => ipAddress;
            set
 {
                ipAddress = value;
  OnPropertyChanged();
            }
        }

        private int port = 8888;
      public int Port
        {
            get => port;
            set
    {
   port = value;
     OnPropertyChanged();
       }
        }

      private double powerPercent = 100;
public double PowerPercent
{
            get => powerPercent;
            set
      {
                powerPercent = Math.Max(0, Math.Min(100, value));
      OnPropertyChanged();
      }
     }

private double width = 100;
        public double Width
        {
      get => width;
 set
      {
      width = Math.Max(0, value);
       OnPropertyChanged();
  }
        }

        private double height = 100;
   public double Height
        {
            get => height;
      set
     {
      height = Math.Max(0, value);
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