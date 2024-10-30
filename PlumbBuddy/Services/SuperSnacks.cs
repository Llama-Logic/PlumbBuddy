namespace PlumbBuddy.Services;

public class SuperSnacks :
    ISuperSnacks
{
    List<NummyEventArgs>? pendingSnacks = [];
    readonly object pendingSnacksLock = new();

    public event EventHandler<NummyEventArgs>? RefreshmentsOffered;

    public void OfferRefreshments(MarkupString message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null)
    {
        var snack = new NummyEventArgs(message, severity, configure, key);
        lock (pendingSnacksLock)
            if (pendingSnacks is not null)
            {
                pendingSnacks.Add(snack);
                return;
            }
        RefreshmentsOffered?.Invoke(this, new NummyEventArgs(message, severity, configure, key));
    }

    public void StopHoarding()
    {
        lock (pendingSnacksLock)
        {
            if (pendingSnacks is null)
                throw new Exception("Some other fat kid already cleaned me out"); // I can say this; ask my endocrinologist
            foreach (var pendingSnack in pendingSnacks)
                RefreshmentsOffered?.Invoke(this, pendingSnack);
            pendingSnacks = null;
        }
    }
}
