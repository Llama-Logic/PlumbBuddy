namespace PlumbBuddy.Components.Dialogs;

partial class SelectCatalogedModFileDialog
{
    static string lastSearchText = string.Empty;

    record ModFileForDisplay(string ModDescription, string Path);

    readonly TableGroupDefinition<ModFileForDisplay?> groupDefinition = new()
    {
        Expandable = true,
        GroupName = nameof(ModFileForDisplay.ModDescription),
        Indentation = false,
        IsInitiallyExpanded = false,
        Selector = element => element?.ModDescription ?? string.Empty
    };
    IReadOnlyList<ModFileForDisplay>? modFilesForDisplay;
    string searchText = lastSearchText;
    ModFileForDisplay? selectedModFileForDisplay;
    MudTable<ModFileForDisplay?>? table;

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    IReadOnlyList<BreadcrumbItem> CreateBreadcrumbs(ModFileForDisplay? modFileForDisplay)
    {
        if (modFileForDisplay?.Path is { } path)
        {
            var segments = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            return
            [
                ..segments.Take(segments.Length - 1).Select(segment => new BreadcrumbItem(segment, "javascript:void(0)", icon: MaterialDesignIcons.Normal.Folder)),
                ..segments.Skip(segments.Length - 1).Select(segment => new BreadcrumbItem(segment, "javascript:void(0)", icon: segment.EndsWith(".ts4script", StringComparison.OrdinalIgnoreCase) ? MaterialDesignIcons.Normal.SourceBranch : MaterialDesignIcons.Normal.PackageVariant))
            ];
        }
        return [];
    }

    bool FilterFunc(ModFileForDisplay? modFileForDisplay)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;
        if (modFileForDisplay?.ModDescription.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (modFileForDisplay?.Path.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        return false;
    }

    void HandleDebounceIntervalEllapsed(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            table?.CollapseAllGroups();
        else
            table?.ExpandAllGroups();
    }

    void OkOnClickHandler()
    {
        lastSearchText = searchText;
        MudDialog?.Close(DialogResult.Ok(selectedModFileForDisplay?.Path));
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !string.IsNullOrWhiteSpace(lastSearchText))
        {
            table?.ExpandAllGroups();
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var modFilesForDisplay = new List<ModFileForDisplay>();
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        foreach (var manifestedModFile in await pbDbContext.ModFileManifests
            .Where(mfm => mfm.ModFileHash.ModFiles.Any())
            .OrderBy(mfm => mfm.Name)
            .Include(mfm => mfm.Creators)
            .Include(mfm => mfm.ModFileHash)
                .ThenInclude(mfh => mfh.ModFiles)
            .ToListAsync()
            .ConfigureAwait(false))
        {
            var modDescription = string.Format(AppText.SelectCatalogedModFileDialog_ModDescription, manifestedModFile.Name, string.IsNullOrWhiteSpace(manifestedModFile.Version) ? string.Empty : string.Format(AppText.SelectCatalogedModFileDialog_ModDescription_ModVersion, manifestedModFile.Version), manifestedModFile.Creators is { } creators ? string.Format(AppText.SelectCatalogedModFileDialog_ModDescription_ByLine, creators.Select(creator => creator.Name).Humanize()) : string.Empty);
            var comparer = PlatformFunctions.FileSystemStringComparison is StringComparison.OrdinalIgnoreCase
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
            if (manifestedModFile.ModFileHash is { } modFileHash)
                foreach (var modFile in (modFileHash.ModFiles ?? Enumerable.Empty<ModFile>()).OrderBy(mf => mf.Path, comparer))
                    modFilesForDisplay.Add(new ModFileForDisplay(modDescription, modFile.Path));
        }
        this.modFilesForDisplay = [..modFilesForDisplay];
    }

    string SelectedRowClassFunc(ModFileForDisplay? modFileForDisplay, int rowNumber) =>
        selectedModFileForDisplay is not null
            && selectedModFileForDisplay.Path == modFileForDisplay?.Path
            ? "mud-background-tertiary-selected"
            : string.Empty;
}
