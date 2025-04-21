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

    async Task<TableData<ModHoundReportNotTrackedRecord>> LoadNotTrackedRecordsAsync(TableState state, CancellationToken token)
    {
        if (ModHoundClient.SelectedReport is not { } selectedReport)
            return new TableData<ModHoundReportNotTrackedRecord> { TotalItems = 0, Items = [] };
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync(token);
        var recordsInScope = pbDbContext.ModHoundReportNotTrackedRecords.Where(mhrr => mhrr.ModHoundReportId == selectedReport.Id);
        var recordsToShow = recordsInScope;
        if (state.SortLabel is { } sortLabel
            && !string.IsNullOrWhiteSpace(sortLabel)
            && state.SortDirection is { } sortDirection
            && sortDirection is SortDirection.Ascending or SortDirection.Descending)
            recordsToShow = sortLabel switch
            {
                "FileName" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.FileName),
                "FileName" => recordsToShow.OrderBy(mhrr => mhrr.FileName),
                "FileDate" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.FileDate),
                "FileDate" => recordsToShow.OrderBy(mhrr => mhrr.FileDate),
                "FileType" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.FileType),
                "FileType" => recordsToShow.OrderBy(mhrr => mhrr.FileType),
                _ => throw new Exception("Unsupported sort configuration")
            };
        if (ModHoundClient.SearchText is { } searchText
            && !string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToUpperInvariant();
            recordsToShow = recordsToShow.Where(mhrr =>
                   mhrr.FileName.ToUpper().Contains(searchText)
                || mhrr.FileDateString != null
                && mhrr.FileDateString.ToUpper().Contains(searchText));
        }
        recordsToShow = recordsToShow.Skip(state.Page * state.PageSize).Take(state.PageSize);
        return new TableData<ModHoundReportNotTrackedRecord>
        {
            TotalItems = await recordsInScope.CountAsync(token),
            Items = await recordsToShow.ToListAsync(token)
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
        var recordsToShow = recordsInScope;
        if (state.SortLabel is { } sortLabel
            && !string.IsNullOrWhiteSpace(sortLabel)
            && state.SortDirection is { } sortDirection
            && sortDirection is SortDirection.Ascending or SortDirection.Descending)
            recordsToShow = sortLabel switch
            {
                "FileName" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.FileName),
                "FileName" => recordsToShow.OrderBy(mhrr => mhrr.FileName),
                "ModName" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.ModName),
                "ModName" => recordsToShow.OrderBy(mhrr => mhrr.ModName),
                "CreatorName" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.CreatorName),
                "CreatorName" => recordsToShow.OrderBy(mhrr => mhrr.CreatorName),
                "LastUpdateDate" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.LastUpdateDate),
                "LastUpdateDate" => recordsToShow.OrderBy(mhrr => mhrr.LastUpdateDate),
                "DateOfInstalledFile" when sortDirection is SortDirection.Descending => recordsToShow.OrderByDescending(mhrr => mhrr.DateOfInstalledFile),
                "DateOfInstalledFile" => recordsToShow.OrderBy(mhrr => mhrr.DateOfInstalledFile),
                _ => throw new Exception("Unsupported sort configuration")
            };
        if (ModHoundClient.SearchText is { } searchText
            && !string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToUpperInvariant();
            recordsToShow = recordsToShow.Where(mhrr =>
                   mhrr.CreatorName.ToUpper().Contains(searchText)
                || mhrr.DateOfInstalledFileString != null
                && mhrr.DateOfInstalledFileString.ToUpper().Contains(searchText)
                || mhrr.FileName.ToUpper().Contains(searchText)
                || mhrr.LastUpdateDateString != null
                && mhrr.LastUpdateDateString.ToUpper().Contains(searchText)
                || mhrr.ModName.ToUpper().Contains(searchText)
                || mhrr.UpdateNotes != null
                && mhrr.UpdateNotes.Contains(searchText));
        }
        recordsToShow = recordsToShow.Skip(state.Page * state.PageSize).Take(state.PageSize);
        return new TableData<ModHoundReportRecord>
        {
            TotalItems = await recordsInScope.CountAsync(token),
            Items = await recordsToShow.ToListAsync(token)
        };
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModHoundClient.PropertyChanged += HandleModHoundClientPropertyChanged;
    }

    void ViewModHoundReportRecordFile(ModHoundReportRecord record)
    {
        var file = new FileInfo(Path.Combine(Settings.UserDataFolderPath, record.FilePath));
        if (!file.Exists)
        {
            SuperSnacks.OfferRefreshments(new MarkupString("The file no longer exists, so I can't show it to you."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileAlert);
            return;
        }
        PlatformFunctions.ViewFile(file);
    }
}
