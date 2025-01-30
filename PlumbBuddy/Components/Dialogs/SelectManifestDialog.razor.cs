namespace PlumbBuddy.Components.Dialogs;

partial class SelectManifestDialog
{
    ResourceKey selectedResourceKey;

    [Parameter]
    public IReadOnlyDictionary<ResourceKey, ModFileManifestModel>? Manifests { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(selectedResourceKey));

    protected override void OnParametersSet()
    {
        if (selectedResourceKey == default)
            selectedResourceKey = Manifests?.FirstOrDefault().Key ?? default;
    }
}
