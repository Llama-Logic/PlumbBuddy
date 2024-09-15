namespace PlumbBuddy.Components.Dialogs;

partial class CautionDialog
{
    [Parameter]
    public string Caption { get; set; } = "Caution";

    [Parameter]
    public string Text { get; set; } = "Are you sure you want to proceed?";

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(true));
}
