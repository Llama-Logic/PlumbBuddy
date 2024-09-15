namespace PlumbBuddy.Services;

public class NummyEventArgs(MarkupString message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null)
{
    public Action<SnackbarOptions>? Configure { get; } = configure;
    public string? Key { get; } = key;
    public MarkupString Message { get; } = message;
    public Severity Severity { get; } = severity;
}
