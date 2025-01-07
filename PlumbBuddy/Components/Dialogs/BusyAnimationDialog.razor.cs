namespace PlumbBuddy.Components.Dialogs;

partial class BusyAnimationDialog
{
    readonly string animationClassName = $"busy-animation-dialog-{Guid.NewGuid()}";

    [Parameter]
    public string AnimationHeight { get; set; } = string.Empty;

    [Parameter]
    public string AnimationPath { get; set; } = string.Empty;

    [Parameter]
    public string AnimationWidth { get; set; } = string.Empty;

    [Parameter]
    public string Caption { get; set; } = string.Empty;

    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Parameter]
    public MudBlazor.Color Color { get; set; } = MudBlazor.Color.Default;

    [Parameter]
    public string IconSvg { get; set; } = string.Empty;

    [Parameter]
    public Task? ProcessComplete { get; set; }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (ProcessComplete is { } processComplete)
        {
            await JSRuntime.InvokeVoidAsync("loadLottie", AnimationPath, animationClassName);
            try
            {
                await processComplete;
            }
            catch
            {
                // all we care about is completion, not faults
            }
            MudDialog?.Close(DialogResult.Ok(true));
        }
    }
}
