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

    async Task OkOnClickHandlerAsync()
    {
        if (requiredFeatures?.Count is 0 && !await DialogService.ShowCautionDialogAsync("Proceeding with No Required Features", "You didn't move any available features to the required section. If this was a mistake, click Cancel and you can do so."))
            return;
        MudDialog?.Close(DialogResult.Ok(requiredFeatures?.ToImmutableArray()));
    }

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
