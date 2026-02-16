namespace PlumbBuddy.Components.Dialogs;

partial class ScrapbookDialog
{
    [Parameter]
    public Chronicle? Chronicle { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Chronicle is { } chronicle)
            chronicle.LoadScrapbookImages();
    }

    void OkOnClickHandler()
    {
        Chronicle?.UnloadScrapbookImages();
        MudDialog?.Close(DialogResult.Ok(true));
    }
}
