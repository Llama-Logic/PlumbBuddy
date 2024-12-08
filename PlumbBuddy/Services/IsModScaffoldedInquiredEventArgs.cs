namespace PlumbBuddy.Services;

public sealed class IsModScaffoldedInquiredEventArgs :
    EventArgs
{
    public bool? IsModScaffolded { get; set; }
    public required string ModFilePath { get; init; }
}
