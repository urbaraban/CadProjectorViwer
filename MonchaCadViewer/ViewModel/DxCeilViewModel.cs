using CadProjectorSDK.Scenes;
using CadProjectorViewer.Services;
using System;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel
{
    internal class DxCeilViewModel : NotifyModel
    {
        public DxCeilViewModel(ScenesCollection scenesCollection)
        {
            ScenesCollection = scenesCollection;
        }

        public ScenesCollection ScenesCollection { get; }

        public string InputText
        {
            get => _inputText;
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged(nameof(InputText));
                    OnPropertyChanged(nameof(ParseCommand));
                }
            }
        }
        private string _inputText;

        public ICommand ParseCommand => new RelayCommand(
        (obj) => string.IsNullOrWhiteSpace(InputText) == false,
        (obj) =>
        {
            if (!string.IsNullOrWhiteSpace(InputText))
            {
                try
                {
                    var parsedObject = DxCeilParser.Parse(InputText);
                    if (parsedObject != null)
                    {
                        SceneTask sceneTask = new SceneTask()
                        {
                            TableID = this.ScenesCollection.SelectedScene.TableID,
                            Object = parsedObject
                        };
                        ScenesCollection.LoadedObjects.Add(sceneTask);
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during parsing
                    System.Diagnostics.Debug.WriteLine($"Parsing error: {ex.Message}");
                }

            }
        });

        public ICommand ClearCommand => new RelayCommand(
            p => true,
            p => InputText = string.Empty);

    }
}
