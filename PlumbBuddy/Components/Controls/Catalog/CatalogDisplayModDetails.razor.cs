namespace PlumbBuddy.Components.Controls.Catalog;

partial class CatalogDisplayModDetails
{
    Task LaunchUrlAsync(Uri url) =>
        Browser.OpenAsync(url.ToString(), BrowserLaunchMode.External);
}
