namespace PlumbBuddy.Components.Controls.Catalog;

partial class CatalogDisplayModDetailsDependencies
{
    string dependenciesSearchText = string.Empty;

    [Parameter]
    public IEnumerable<CatalogModKey>? Dependencies { get; set; }

    bool IncludeDependency(CatalogModKey key)
    {
        if (string.IsNullOrWhiteSpace(dependenciesSearchText))
            return true;
        if (key.Name.Contains(dependenciesSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (key.Creators?.Contains(dependenciesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (key.Url?.ToString().Contains(dependenciesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        return false;
    }
}
