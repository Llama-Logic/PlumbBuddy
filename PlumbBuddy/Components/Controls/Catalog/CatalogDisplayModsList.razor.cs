namespace PlumbBuddy.Components.Controls.Catalog;

partial class CatalogDisplayModsList
{
    string modsFolderPath = string.Empty;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            Settings.PropertyChanged -= HandleSettingsPropertyChanged;
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.UserDataFolderPath))
            StaticDispatcher.Dispatch(() => modsFolderPath = Path.Combine(Settings.UserDataFolderPath));
    }

    bool IncludeMod(KeyValuePair<CatalogModKey, IReadOnlyList<(ModFileManifestModel manifest, IReadOnlyList<FileInfo> files, IReadOnlyList<CatalogModKey> dependencies, IReadOnlyList<CatalogModKey> dependents)>> kv)
    {
        var modsSearchText = Catalog.ModsSearchText;
        var (key, value) = kv;
        if (string.IsNullOrWhiteSpace(modsSearchText))
            return true;
        if (key.Name.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (key.Creators?.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (key.Url?.ToString().Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        foreach (var (manifest, files, dependencies, dependents) in value)
        {
            if (manifest.RequiredPacks.Any(rp => rp.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (manifest.IncompatiblePacks.Any(ip => ip.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (files.Any(file => file.FullName[modsFolderPath.Length..].Contains(modsSearchText, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (dependencies.Any(dependency => dependency.Name.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) || (dependency.Creators?.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false) || (dependency.Url?.ToString().Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)))
                return true;
            if (dependents.Any(dependent => dependent.Name.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) || (dependent.Creators?.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false) || (dependent.Url?.ToString().Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)))
                return true;
        }
        return false;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        modsFolderPath = Path.Combine(Settings.UserDataFolderPath, "Mods");
    }
}
