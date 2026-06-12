using CadProjectorSDK;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Scenes.Commands;
using Microsoft.Xaml.Behaviors.Core;
using System.Windows.Input;

namespace CadProjectorViewer.ViewModel
{
    internal class ScrollPanelViewModel : NotifyModel
    {
        public ScrollPanelViewModel(ProjectorHub projectorHub)
        {
            ProjectorHub = projectorHub;
        }

        public ProjectorHub ProjectorHub { get; }

        public ScenesCollection ScenesCollection => ProjectorHub.ScenesCollection;

        public ICommand ClearLoadedObjectsCommand => new ActionCommand(() =>
        {
            ScenesCollection.LoadedObjects.Clear();
        });

        public ICommand SelectNextCommand => new ActionCommand(() =>
        {
            ScenesCollection.SelectedScene.HistoryCommands.Add(
                new SelectNextCommand(true, ScenesCollection.SelectedScene, false));
        });

        public ICommand SelectPreviousCommand => new ActionCommand(() =>
        {
            ScenesCollection.SelectedScene.HistoryCommands.Add(
                new SelectNextCommand(false, ScenesCollection.SelectedScene, false));
        });
    }
}
