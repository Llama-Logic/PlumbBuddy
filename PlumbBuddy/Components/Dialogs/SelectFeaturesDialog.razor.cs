namespace PlumbBuddy.Components.Dialogs;

partial class SelectFeaturesDialog
{
    ICollection<string>? availableFeatures;
    ICollection<string>? requiredFeatures;

    [Parameter]
    public ModFileManifestModel? Manifest { get; set; }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(requiredFeatures?.ToImmutableArray()));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Manifest is { } manifest)
        {
            availableFeatures = [..manifest.Features];
            requiredFeatures = [];
        }
    }
}
