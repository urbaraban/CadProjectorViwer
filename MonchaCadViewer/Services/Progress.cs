using CadProjectorViewer.ViewModel;
using System.Threading;

namespace CadProjectorViewer.Services
{
    public class Progress : ViewModelBase
    {
        public static Progress Instance { get; } = new Progress();

        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }
        private string _label;

        public int MaxValue
        {
            get => _maxvalue;
            set
            {
                _maxvalue = value;
                OnPropertyChanged(nameof(MaxValue));
            }
        }
        private int _maxvalue;

        public int StatValue
        {
            get => _statvalue;
            set
            {
                _statvalue = value;
                OnPropertyChanged(nameof(StatValue));
            }
        }
        private int _statvalue;

        private Progress()
        {

        }

        public void StartTask(CancellationToken token)
        {

        }

        public void SetProgress(int Value, int MaxValue, string Text)
        {
            this.Label = Text;
            this.MaxValue = MaxValue;
            this.StatValue = Value;
        }

        public void End()
        {
            this.Label = string.Empty;
            this.MaxValue = 100;
            this.StatValue = 0;
        }
    }
}
