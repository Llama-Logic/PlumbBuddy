namespace PlumbBuddy.Components.Dialogs;

partial class ErrorDialog
{
    [Parameter]
    public string Caption { get; set; } = "Error";

    [Parameter]
    public string Text { get; set; } = "Something broke.";

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(true));
}
