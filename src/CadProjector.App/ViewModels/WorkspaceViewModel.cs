using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadProjector.App.ViewModels;

public enum DockSlot
{
    Left,
    Right,
    Bottom,
    Dialog
}

public enum PanelId
{
    Objects,
    Scene,
    WorkFolder,
    Transform,
    Device,
    StlAlign,
    Logs,
    Endpoints,
    Clients,
    Commands,
    Hotkeys,
    /// <summary>Merged into Device / Modules; kept for CloseOverlay compatibility.</summary>
    Calibrate
}

public sealed partial class WorkspaceViewModel : ViewModelBase
{
    [ObservableProperty] public partial PanelId? ActiveLeft { get; set; }
    [ObservableProperty] public partial PanelId? ActiveRight { get; set; }
    [ObservableProperty] public partial PanelId? ActiveBottom { get; set; }
    [ObservableProperty] public partial bool IsHotkeysOpen { get; set; }
    [ObservableProperty] public partial double LeftWidth { get; set; } = 300;
    [ObservableProperty] public partial double RightWidth { get; set; } = 340;
    [ObservableProperty] public partial double BottomHeight { get; set; } = 200;
    [ObservableProperty] public partial bool IsBottomOpen { get; set; }

    /// <summary>Sub-tab inside Device panel: 0 Card, 1 FOV, 2 Modules.</summary>
    [ObservableProperty] public partial int DeviceTabIndex { get; set; }

    /// <summary>Sub-tab inside Right dock: 0 Transform, 1 Device, 2 STL.</summary>
    [ObservableProperty] public partial int RightTabIndex { get; set; }

    /// <summary>Sub-tab inside Bottom console: 0 Logs, 1 Endpoints, 2 Clients, 3 Commands.</summary>
    [ObservableProperty] public partial int BottomTabIndex { get; set; }

    public static DockSlot SlotOf(PanelId id) => id switch
    {
        PanelId.Objects or PanelId.Scene or PanelId.WorkFolder => DockSlot.Left,
        PanelId.Transform or PanelId.Device or PanelId.StlAlign or PanelId.Calibrate => DockSlot.Right,
        PanelId.Logs or PanelId.Endpoints or PanelId.Clients or PanelId.Commands => DockSlot.Bottom,
        PanelId.Hotkeys => DockSlot.Dialog,
        _ => DockSlot.Left
    };

    public bool IsOpen(PanelId id) => id switch
    {
        PanelId.Objects => ActiveLeft == PanelId.Objects,
        PanelId.Scene => ActiveLeft == PanelId.Scene,
        PanelId.WorkFolder => ActiveLeft == PanelId.WorkFolder,
        PanelId.Transform => ActiveRight == PanelId.Transform,
        PanelId.Device or PanelId.Calibrate => ActiveRight is PanelId.Device or PanelId.Calibrate,
        PanelId.StlAlign => ActiveRight == PanelId.StlAlign,
        PanelId.Logs => IsBottomOpen && BottomTabIndex == 0,
        PanelId.Endpoints => IsBottomOpen && BottomTabIndex == 1,
        PanelId.Clients => IsBottomOpen && BottomTabIndex == 2,
        PanelId.Commands => IsBottomOpen && BottomTabIndex == 3,
        PanelId.Hotkeys => IsHotkeysOpen,
        _ => false
    };

    [RelayCommand]
    public void Toggle(PanelId id)
    {
        switch (SlotOf(id))
        {
            case DockSlot.Left:
                ActiveLeft = ActiveLeft == id ? null : id;
                break;
            case DockSlot.Right:
                if (id == PanelId.Calibrate)
                {
                    RevealDeviceModules();
                    break;
                }

                if (ActiveRight == NormalizeRight(id))
                    ActiveRight = null;
                else
                    RevealRight(id);
                break;
            case DockSlot.Bottom:
                RevealBottom(id, toggle: true);
                break;
            case DockSlot.Dialog:
                IsHotkeysOpen = !IsHotkeysOpen;
                break;
        }
    }

    public void Reveal(PanelId id)
    {
        switch (SlotOf(id))
        {
            case DockSlot.Left:
                ActiveLeft = id;
                break;
            case DockSlot.Right:
                if (id == PanelId.Calibrate)
                    RevealDeviceModules();
                else
                    RevealRight(id);
                break;
            case DockSlot.Bottom:
                RevealBottom(id, toggle: false);
                break;
            case DockSlot.Dialog:
                IsHotkeysOpen = true;
                break;
        }
    }

    public void Close(PanelId id)
    {
        switch (SlotOf(id))
        {
            case DockSlot.Left when ActiveLeft == id:
                ActiveLeft = null;
                break;
            case DockSlot.Right when ActiveRight == NormalizeRight(id) || id == PanelId.Calibrate:
                if (id == PanelId.Calibrate)
                    break;
                ActiveRight = null;
                break;
            case DockSlot.Bottom:
                IsBottomOpen = false;
                break;
            case DockSlot.Dialog:
                IsHotkeysOpen = false;
                break;
        }
    }

    [RelayCommand]
    public void CloseSlot(DockSlot slot)
    {
        switch (slot)
        {
            case DockSlot.Left:
                ActiveLeft = null;
                break;
            case DockSlot.Right:
                ActiveRight = null;
                break;
            case DockSlot.Bottom:
                IsBottomOpen = false;
                break;
            case DockSlot.Dialog:
                IsHotkeysOpen = false;
                break;
        }
    }

    public void CloseFocusedOrBottom()
    {
        if (IsHotkeysOpen)
        {
            IsHotkeysOpen = false;
            return;
        }

        if (ActiveRight is not null)
        {
            ActiveRight = null;
            return;
        }

        if (ActiveLeft is not null)
        {
            ActiveLeft = null;
            return;
        }

        if (IsBottomOpen)
            IsBottomOpen = false;
    }

    public void RevealDeviceModules()
    {
        ActiveRight = PanelId.Device;
        RightTabIndex = 1; // Device hub
        DeviceTabIndex = 2; // Modules
    }

    private void RevealRight(PanelId id)
    {
        var normalized = NormalizeRight(id);
        ActiveRight = normalized;
        RightTabIndex = normalized switch
        {
            PanelId.Transform => 0,
            PanelId.Device => 1,
            PanelId.StlAlign => 2,
            _ => RightTabIndex
        };
    }

    private void RevealBottom(PanelId id, bool toggle)
    {
        var tab = id switch
        {
            PanelId.Logs => 0,
            PanelId.Endpoints => 1,
            PanelId.Clients => 2,
            PanelId.Commands => 3,
            _ => 0
        };

        if (toggle && IsBottomOpen && BottomTabIndex == tab)
        {
            IsBottomOpen = false;
            return;
        }

        BottomTabIndex = tab;
        IsBottomOpen = true;
    }

    private static PanelId NormalizeRight(PanelId id) => id switch
    {
        PanelId.Calibrate => PanelId.Device,
        _ => id
    };

    partial void OnActiveLeftChanged(PanelId? value) => RaisePanelProxies();
    partial void OnActiveRightChanged(PanelId? value) => RaisePanelProxies();
    partial void OnIsBottomOpenChanged(bool value) => RaisePanelProxies();
    partial void OnBottomTabIndexChanged(int value) => RaisePanelProxies();
    partial void OnIsHotkeysOpenChanged(bool value) => RaisePanelProxies();
    partial void OnRightTabIndexChanged(int value)
    {
        // Keep ActiveRight in sync when user clicks inspector tabs.
        ActiveRight = value switch
        {
            0 => PanelId.Transform,
            1 => PanelId.Device,
            2 => PanelId.StlAlign,
            _ => ActiveRight
        };
        RaisePanelProxies();
    }

    private void RaisePanelProxies()
    {
        OnPropertyChanged(new PropertyChangedEventArgs("Panels"));
    }
}
