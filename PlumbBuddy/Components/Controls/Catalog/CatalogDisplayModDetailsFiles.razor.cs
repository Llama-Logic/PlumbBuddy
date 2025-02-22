namespace PlumbBuddy.Components.Controls.Catalog;

partial class CatalogDisplayModDetailsFiles
{
    string filesSearchText = string.Empty;
    string modsFolderPath = string.Empty;

    [Parameter]
    public IEnumerable<CatalogModValue>? CatalogModValues { get; set; }

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

    IReadOnlyList<BreadcrumbItem> GetModFileBreadcrumbs(FileInfo modFile)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var segments = modFile.FullName[modsFolderPath.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
        for (var i = 0; i < segments.Length - 1; ++i)
            breadcrumbs.Add(new(segments[i], null, icon: MaterialDesignIcons.Normal.Folder));
        breadcrumbs.Add(new(segments[^1], null, icon: modFile.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase) ? MaterialDesignIcons.Normal.SourceBranch : MaterialDesignIcons.Normal.PackageVariantClosed));
        return [.. breadcrumbs];
    }

    bool IncludeFile(ValueTuple<ModFileManifestModel, FileInfo> record)
    {
        if (string.IsNullOrWhiteSpace(filesSearchText))
            return true;
        if (record.Item2.FullName[(modsFolderPath.Length + 1)..].Contains(filesSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        foreach (var translator in record.Item1.Translators)
        {
            if (translator.Name.Contains(filesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
            if (translator.Language.Contains(filesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
            if (translator.Culture?.NativeName.Contains(filesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
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
