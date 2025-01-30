namespace PlumbBuddy.Components.Dialogs;

partial class CautionDialog
{
    [Parameter]
    public string Caption { get; set; } = string.Empty;

    [Parameter]
    public string Text { get; set; } = string.Empty;

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(true));
}
