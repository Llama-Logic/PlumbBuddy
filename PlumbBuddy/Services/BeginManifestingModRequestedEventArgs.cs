namespace PlumbBuddy.Services;

public sealed class BeginManifestingModRequestedEventArgs :
    EventArgs
{
    public required string ModFilePath { get; init; }
}
