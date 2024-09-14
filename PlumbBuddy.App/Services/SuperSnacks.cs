namespace PlumbBuddy.App.Services;

public class SuperSnacks :
    ISuperSnacks
{
    public event EventHandler<NummyEventArgs>? RefreshmentsOffered;

    public void OfferRefreshments(MarkupString message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null) =>
        RefreshmentsOffered?.Invoke(this, new NummyEventArgs(message, severity, configure, key));
}
