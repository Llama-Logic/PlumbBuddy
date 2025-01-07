namespace PlumbBuddy.Components.Dialogs;

class CreateBranchDialogResult
{
    public required string ChronicleName { get; init; }
    public required string? GameNameOverride { get; init; }
    public required string? Notes { get; init; }
    public required ImmutableArray<byte> Thumbnail { get; init; }
}
