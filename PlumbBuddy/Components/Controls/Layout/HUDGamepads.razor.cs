namespace PlumbBuddy.Components.Controls.Layout;

partial class HUDGamepads
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            GamepadInterop.Updated -= HandleGamepadInteropUpdated;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
            GamepadInterop.Updated += HandleGamepadInteropUpdated;
    }

    void HandleGamepadInteropUpdated(object? sender, EventArgs e) =>
        StaticDispatcher.Dispatch(() => StateHasChanged());
}
