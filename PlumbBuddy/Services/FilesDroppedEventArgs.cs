namespace PlumbBuddy.Services;

public class FilesDroppedEventArgs :
    EventArgs
{
    public required IReadOnlyList<string> Paths { get; init; }
}
