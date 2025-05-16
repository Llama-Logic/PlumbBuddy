namespace PlumbBuddy.Components.Controls.ModHound;

partial class ModHoundDisplay
{
    MudTable<ModHoundReportNotTrackedRecord>? notTrackedRecordsTable;
    MudTable<ModHoundReportRecord>? recordsTable;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            ModHoundClient.PropertyChanged -= HandleModHoundClientPropertyChanged;
    }

    async Task DownloadModHoundReportRecordUpdateAsync(ModHoundReportRecord record)
    {
        if (record.ModLinkOrIndexHref is not { } url)
            return;
        await Browser.OpenAsync(url, BrowserLaunchMode.External);
    }

    void HandleModHoundClientPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModHoundClient.SearchText) or nameof(IModHoundClient.SelectedReport) or nameof(IModHoundClient.SelectedReportSection))
        {
            recordsTable?.ReloadServerData();
            notTrackedRecordsTable?.ReloadServerData();
        }
    }

    async Task HandleShowSettingsAsync() =>
        await DialogService.ShowSettingsDialogAsync(4);

    async Task<TableData<ModHoundReportNotTrackedRecord>> LoadNotTrackedRecordsAsync(TableState state, CancellationToken token)
    {
        if (ModHoundClient.SelectedReport is not { } selectedReport)
            return new TableData<ModHoundReportNotTrackedRecord> { TotalItems = 0, Items = [] };
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync(token);
        var recordsInScope = pbDbContext.ModHoundReportNotTrackedRecords.Where(mhrr => mhrr.ModHoundReportId == selectedReport.Id);
        if (state.SortLabel is { } sortLabel
            && !string.IsNullOrWhiteSpace(sortLabel)
            && state.SortDirection is { } sortDirection
            && sortDirection is SortDirection.Ascending or SortDirection.Descending)
            recordsInScope = sortLabel switch
            {
                "FileName" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.FileName.ToUpper()),
                "FileName" => recordsInScope.OrderBy(mhrr => mhrr.FileName.ToUpper()),
                "FileDate" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.FileDate),
                "FileDate" => recordsInScope.OrderBy(mhrr => mhrr.FileDate),
                "FileType" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.FileType),
                "FileType" => recordsInScope.OrderBy(mhrr => mhrr.FileType),
                _ => throw new Exception("Unsupported sort configuration")
            };
        if (ModHoundClient.SearchText is { } searchText
            && !string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToUpperInvariant();
            recordsInScope = recordsInScope.Where(mhrr =>
                   mhrr.FileName.ToUpper().Contains(searchText)
                || mhrr.FileDateString != null
                && mhrr.FileDateString.ToUpper().Contains(searchText));
        }
        return new TableData<ModHoundReportNotTrackedRecord>
        {
            TotalItems = await recordsInScope.CountAsync(token),
            Items = await recordsInScope.Skip(state.Page * state.PageSize).Take(state.PageSize).ToListAsync(token)
        };
    }

    async Task<TableData<ModHoundReportRecord>> LoadRecordsAsync(TableState state, CancellationToken token)
    {
        if (ModHoundClient.SelectedReport is not { } selectedReport
            || ModHoundClient.SelectedReportSection switch
            {
                IModHoundClient.SectionOutdated => ModHoundReportRecordStatus.OutdatedMatches,
                IModHoundClient.SectionDuplicates => ModHoundReportRecordStatus.DuplicateMatches,
                IModHoundClient.SectionBrokenObsolete => ModHoundReportRecordStatus.BrokenObsoleteMatches,
                IModHoundClient.SectionUnknownStatus => ModHoundReportRecordStatus.UnknownStatusMatches,
                IModHoundClient.SectionUpToDate => ModHoundReportRecordStatus.UpToDateOkayMatches,
                _ => (ModHoundReportRecordStatus?)null
            } is not { } recordStatus)
            return new TableData<ModHoundReportRecord> { TotalItems = 0, Items = [] };
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync(token);
        var recordsInScope = pbDbContext.ModHoundReportRecords.Where(mhrr => mhrr.ModHoundReportId == selectedReport.Id && mhrr.Status == recordStatus);
        if (state.SortLabel is { } sortLabel
            && !string.IsNullOrWhiteSpace(sortLabel)
            && state.SortDirection is { } sortDirection
            && sortDirection is SortDirection.Ascending or SortDirection.Descending)
            recordsInScope = sortLabel switch
            {
                "FileName" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.FileName.ToUpper()),
                "FileName" => recordsInScope.OrderBy(mhrr => mhrr.FileName.ToUpper()),
                "ModName" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.ModName.ToUpper()),
                "ModName" => recordsInScope.OrderBy(mhrr => mhrr.ModName.ToUpper()),
                "CreatorName" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.CreatorName.ToUpper()),
                "CreatorName" => recordsInScope.OrderBy(mhrr => mhrr.CreatorName.ToUpper()),
                "LastUpdateDate" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.LastUpdateDate),
                "LastUpdateDate" => recordsInScope.OrderBy(mhrr => mhrr.LastUpdateDate),
                "DateOfInstalledFile" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mhrr => mhrr.DateOfInstalledFile),
                "DateOfInstalledFile" => recordsInScope.OrderBy(mhrr => mhrr.DateOfInstalledFile),
                _ => throw new Exception("Unsupported sort configuration")
            };
        if (ModHoundClient.SearchText is { } searchText
            && !string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToUpperInvariant();
            recordsInScope = recordsInScope.Where(mhrr =>
                   mhrr.CreatorName.ToUpper().Contains(searchText)
                || mhrr.DateOfInstalledFileString != null
                && mhrr.DateOfInstalledFileString.ToUpper().Contains(searchText)
                || mhrr.FileName.ToUpper().Contains(searchText)
                || mhrr.LastUpdateDateString != null
                && mhrr.LastUpdateDateString.ToUpper().Contains(searchText)
                || mhrr.ModName.ToUpper().Contains(searchText)
                || mhrr.UpdateNotes != null
                && mhrr.UpdateNotes.ToUpper().Contains(searchText));
        }
        return new TableData<ModHoundReportRecord>
        {
            TotalItems = await recordsInScope.CountAsync(token),
            Items = await recordsInScope.Skip(state.Page * state.PageSize).Take(state.PageSize).ToListAsync(token)
        };
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModHoundClient.PropertyChanged += HandleModHoundClientPropertyChanged;
    }

    void ViewFile(FileInfo file)
    {
        if (!file.Exists)
        {
            SuperSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundDisplay_Snack_Error_CannotViewRemovedFile), Severity.Error, options =>
            {
                options.Icon = MaterialDesignIcons.Normal.FileAlert;
                options.RequireInteraction = true;
            });
            return;
        }
        PlatformFunctions.ViewFile(file);
    }

    void ViewModHoundReportIncompatibilityRecordPartFile(ModHoundReportIncompatibilityRecordPart part) =>
        ViewFile(new FileInfo(Path.Combine(Settings.UserDataFolderPath, part.FilePath)));

    void ViewModHoundReportRecordFile(ModHoundReportRecord record) =>
        ViewFile(new FileInfo(Path.Combine(Settings.UserDataFolderPath, record.FilePath)));
}
