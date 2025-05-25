namespace PlumbBuddy.Components.Controls.Layout;

public partial class HUDProxyHostStatus
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ProxyHost.ClientError -= HandleProxyHostClientError;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ProxyHost.ClientError += HandleProxyHostClientError;
    }

    void HandleProxyHostClientError(object? sender, ProxyHostClientErrorEventArgs e)
    {
        SuperSnacks.OfferRefreshments(new MarkupString($"The phone call I was on with your mods just <em>suddenly</em> got interrupted. Yikes.<br /><code>{e.Exception.GetType().Name}: {e.Exception.Message}</code>"), Severity.Warning, options =>
        {
            options.Icon = MaterialDesignIcons.Normal.Alert;
            options.RequireInteraction = true;
        });
    }
}
