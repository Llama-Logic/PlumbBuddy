namespace PlumbBuddy.Components.Controls.Catalog;

partial class CatalogDisplayModDetailsDependents
{
    string dependentsSearchText = string.Empty;

    [Parameter]
    public IEnumerable<CatalogModKey>? Dependents { get; set; }

    bool IncludeDependent(CatalogModKey key)
    {
        if (string.IsNullOrWhiteSpace(dependentsSearchText))
            return true;
        if (key.Name.Contains(dependentsSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (key.Creators?.Contains(dependentsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (key.Url?.ToString().Contains(dependentsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        return false;
    }
}
