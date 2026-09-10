using CadProjector.App.ViewModels;

namespace CadProjector.Tests;

public class WorkspaceViewModelTests
{
    [Fact]
    public void Toggle_LeftPanels_KeepsSingleActive()
    {
        var ws = new WorkspaceViewModel();
        ws.Toggle(PanelId.Objects);
        Assert.Equal(PanelId.Objects, ws.ActiveLeft);

        ws.Toggle(PanelId.Scene);
        Assert.Equal(PanelId.Scene, ws.ActiveLeft);
        Assert.True(ws.IsOpen(PanelId.Scene));
        Assert.False(ws.IsOpen(PanelId.Objects));
    }

    [Fact]
    public void Toggle_SamePanel_Closes()
    {
        var ws = new WorkspaceViewModel();
        ws.Toggle(PanelId.WorkFolder);
        ws.Toggle(PanelId.WorkFolder);
        Assert.Null(ws.ActiveLeft);
    }

    [Fact]
    public void Reveal_Transform_ReplacesDevice()
    {
        var ws = new WorkspaceViewModel();
        ws.Reveal(PanelId.Device);
        Assert.Equal(PanelId.Device, ws.ActiveRight);

        ws.Reveal(PanelId.Transform);
        Assert.Equal(PanelId.Transform, ws.ActiveRight);
        Assert.Equal(0, ws.RightTabIndex);
    }

    [Fact]
    public void Reveal_Calibrate_OpensDeviceModules()
    {
        var ws = new WorkspaceViewModel();
        ws.Reveal(PanelId.Calibrate);
        Assert.Equal(PanelId.Device, ws.ActiveRight);
        Assert.Equal(2, ws.DeviceTabIndex);
    }

    [Fact]
    public void Bottom_ToggleSameTab_Closes()
    {
        var ws = new WorkspaceViewModel();
        ws.Toggle(PanelId.Logs);
        Assert.True(ws.IsBottomOpen);
        Assert.True(ws.IsOpen(PanelId.Logs));

        ws.Toggle(PanelId.Logs);
        Assert.False(ws.IsBottomOpen);
    }

    [Fact]
    public void CloseFocusedOrBottom_ClosesRightThenLeftThenBottom()
    {
        var ws = new WorkspaceViewModel();
        ws.Reveal(PanelId.Objects);
        ws.Reveal(PanelId.Transform);
        ws.Reveal(PanelId.Logs);

        ws.CloseFocusedOrBottom();
        Assert.Null(ws.ActiveRight);
        Assert.Equal(PanelId.Objects, ws.ActiveLeft);
        Assert.True(ws.IsBottomOpen);

        ws.CloseFocusedOrBottom();
        Assert.Null(ws.ActiveLeft);
        Assert.True(ws.IsBottomOpen);

        ws.CloseFocusedOrBottom();
        Assert.False(ws.IsBottomOpen);
    }
}
