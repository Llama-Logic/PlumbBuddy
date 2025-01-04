namespace PlumbBuddy.Components.Dialogs;

partial class CreateBranchDialog
{
    public CreateBranchDialog() =>
        RandomizeNewSaveGameInstanceOnClickHandler();

    uint newSaveGameInstance;

    [Parameter]
    public string ChronicleName { get; set; } = string.Empty;

    [Parameter]
    public uint PreviousSaveGameInstance { get; set; }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    [SuppressMessage("Security", "CA5394: Do not use insecure randomness", Justification = "There are no security concerns here.")]
    void RandomizeNewSaveGameInstanceOnClickHandler()
    {
        Span<byte> byteSpan = stackalloc byte[4];
        Random.Shared.NextBytes(byteSpan);
        newSaveGameInstance = MemoryMarshal.Read<uint>(byteSpan);
    }

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(new CreateBranchDialogResult { ChronicleName = ChronicleName, NewSaveGameInstance = newSaveGameInstance }));
}
