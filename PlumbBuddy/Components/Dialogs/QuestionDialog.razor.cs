namespace PlumbBuddy.Components.Dialogs;

partial class QuestionDialog
{
    [Parameter]
    public string Caption { get; set; } = string.Empty;

    [Parameter]
    public string Text { get; set; } = string.Empty;

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public bool UserCanCancel { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void NoOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(false));

    void YesOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(true));
}
