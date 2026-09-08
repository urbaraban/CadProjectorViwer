using CadProjector.Automation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public partial class EndpointItemViewModel : ObservableObject
{
    private readonly AutomationHub _hub;
    private readonly Action _changed;

    public EndpointItemViewModel(AutomationHub hub, RemoteEndpointInfo model, Action changed)
    {
        _hub = hub;
        Model = model;
        _changed = changed;
        Refresh();
    }

    public RemoteEndpointInfo Model { get; }
    public string DisplayName => Model.DisplayName;
    public string TypeLabel => Model.Type switch
    {
        RemoteEndpointType.UdpBinary => "UDP Binary",
        RemoteEndpointType.UdpText => "UDP Text",
        RemoteEndpointType.TcpText => "TCP Text",
        _ => Model.Type.ToString()
    };
    public string BindSummary => $"{Model.BindIp}:{Model.Port}";

    [ObservableProperty] public partial bool IsListening { get; set; }

    public void Refresh() => IsListening = _hub.IsListening(Model);

    [RelayCommand]
    private async Task StartAsync()
    {
        await _hub.StartEndpointAsync(Model);
        Refresh();
        _changed();
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        await _hub.StopEndpointAsync(Model);
        Refresh();
        _changed();
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        await _hub.RemoveEndpointAsync(Model);
        _changed();
    }
}
