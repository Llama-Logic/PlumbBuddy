namespace PlumbBuddy.Components.Dialogs;

partial class InfoDialog
{
    [Parameter]
    public string Caption { get; set; } = "Success";

    [Parameter]
    public string Text { get; set; } = "All done.";

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(true));
}
