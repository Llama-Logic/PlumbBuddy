namespace PlumbBuddy.Components.Dialogs;

partial class DeleteErrorLogsDialog
{
    [Parameter]
    public IReadOnlyList<string>? FilePaths { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public IEnumerable<string>? SelectedFilePaths { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(SelectedFilePaths));
}
