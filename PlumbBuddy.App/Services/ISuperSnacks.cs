namespace PlumbBuddy.App.Services;

public interface ISuperSnacks
{
    event EventHandler<NummyEventArgs>? RefreshmentsOffered;

    void OfferRefreshments(MarkupString message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null);
}
