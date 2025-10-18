namespace PlumbBuddy.Services;

public class PersonalNotes :
    IPersonalNotes
{
    public PersonalNotes(ILogger<PersonalNotes> logger, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        this.logger = logger;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
    }

    string? batchNotes;
    DateTime? batchPersonalDate;
    string? editNotes;
    DateTime? editPersonalDate;
    DateTime? fileDateLowerBound;
    DateTime? fileDateUpperBound;
    readonly ILogger<PersonalNotes> logger;
    DateTime? modFilesDateLowerBound;
    DateTime? modFilesDateUpperBound;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    DateTime? personalDateLowerBound;
    DateTime? personalDateUpperBound;
    DateTime? playerDataDateLowerBound;
    DateTime? playerDataDateUpperBound;
    string? searchText;
    readonly ISettings settings;

    public string? BatchNotes
    {
        get => batchNotes;
        set
        {
            if (batchNotes == value)
                return;
            batchNotes = value;
            OnPropertyChanged();
        }
    }

    public DateTime? BatchPersonalDate
    {
        get => batchPersonalDate;
        set
        {
            if (batchPersonalDate == value)
                return;
            batchPersonalDate = value;
            OnPropertyChanged();
        }
    }

    public string? EditNotes
    {
        get => editNotes;
        set
        {
            if (editNotes == value)
                return;
            editNotes = value;
            OnPropertyChanged();
        }
    }

    public DateTime? EditPersonalDate
    {
        get => editPersonalDate;
        set
        {
            if (editPersonalDate == value)
                return;
            editPersonalDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime? FileDateLowerBound
    {
        get => fileDateLowerBound;
        set
        {
            if (fileDateLowerBound == value)
                return;
            fileDateLowerBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? FileDateUpperBound
    {
        get => fileDateUpperBound;
        set
        {
            if (fileDateUpperBound == value)
                return;
            fileDateUpperBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? ModFilesDateLowerBound
    {
        get => modFilesDateLowerBound;
        private set
        {
            if (modFilesDateLowerBound == value)
                return;
            modFilesDateLowerBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? ModFilesDateUpperBound
    {
        get => modFilesDateUpperBound;
        private set
        {
            if (modFilesDateUpperBound == value)
                return;
            modFilesDateUpperBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? PersonalDateLowerBound
    {
        get => personalDateLowerBound;
        set
        {
            if (personalDateLowerBound == value)
                return;
            personalDateLowerBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? PersonalDateUpperBound
    {
        get => personalDateUpperBound;
        set
        {
            if (personalDateUpperBound == value)
                return;
            personalDateUpperBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? PlayerDataDateLowerBound
    {
        get => playerDataDateLowerBound;
        private set
        {
            if (playerDataDateLowerBound == value)
                return;
            playerDataDateLowerBound = value;
            OnPropertyChanged();
        }
    }

    public DateTime? PlayerDataDateUpperBound
    {
        get => playerDataDateUpperBound;
        private set
        {
            if (playerDataDateUpperBound == value)
                return;
            playerDataDateUpperBound = value;
            OnPropertyChanged();
        }
    }

    public string? SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value)
                return;
            searchText = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler? DataAltered;
    public event PropertyChangedEventHandler? PropertyChanged;

    IQueryable<ModFile> ApplyFilters(IQueryable<ModFile> records)
    {
        var copiedSearchText = searchText;
        if (!string.IsNullOrWhiteSpace(copiedSearchText))
        {
            var likePattern = $"%{copiedSearchText.Trim()}%";
            records = records
                .Where
                (
                    mf =>
                    EF.Functions.Like(mf.Path, likePattern)
                    || mf.ModFileHash.ModFileManifests.Any(mfm => EF.Functions.Like(mfm.Name, likePattern))
                    || mf.ModFileHash.ModFilePlayerRecords.Any(mfpr => EF.Functions.Like(mfpr.Notes, likePattern))
                );
        }
        if (fileDateLowerBound is { } copiedFileDateLowerBound)
            records = records.Where(mf => mf.LastWrite >= copiedFileDateLowerBound);
        if (fileDateUpperBound is { } copiedFileDateUpperBound)
            records = records.Where(mf => mf.LastWrite <= copiedFileDateUpperBound);
        if (personalDateLowerBound is { } copiedPersonalDateLowerBound)
            records = records.Where(mf => mf.ModFileHash.ModFilePlayerRecords.Any(mfpr => mfpr.PersonalDate != null && mfpr.PersonalDate >= copiedPersonalDateLowerBound));
        if (personalDateUpperBound is { } copiedPersonalDateUpperBound)
            records = records.Where(mf => mf.ModFileHash.ModFilePlayerRecords.Any(mfpr => mfpr.PersonalDate != null && mfpr.PersonalDate <= copiedPersonalDateUpperBound));
        return records;
    }

    public void HandleRecordOnPreviewEditClick(object? item)
    {
        if (item is PersonalNotesRecord record)
        {
            EditNotes = record.Notes;
            EditPersonalDate = record.PersonalDate?.LocalDateTime;
        }
    }

    public async void HandleRecordRowEditCommit(object? item)
    {
        if (item is PersonalNotesRecord record)
        {
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var modFileHash = await pbDbContext.ModFiles
                .Where(mf => mf.FoundAbsent == null && mf.Path == record.ModsFolderPath)
                .Select(mf => mf.ModFileHash)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (modFileHash is null)
                return;
            var modFilePlayerRecords = await pbDbContext.ModFilePlayerRecords
                .Where(mfpr => mfpr.ModFileHashes.Any(mfh => mfh.Id == modFileHash.Id))
                .Include(mfpr => mfpr.ModFileHashes)
                .Include(mfpr => mfpr.ModFilePlayerRecordPaths)
                .ToListAsync()
                .ConfigureAwait(false);
            if (modFilePlayerRecords.Count is 0
                && (!string.IsNullOrWhiteSpace(editNotes) || editPersonalDate is not null))
            {
                var newModFilePlayerRecord = new ModFilePlayerRecord();
                newModFilePlayerRecord.ModFileHashes.Add(modFileHash);
                newModFilePlayerRecord.ModFilePlayerRecordPaths.Add(new ModFilePlayerRecordPath(newModFilePlayerRecord) { Path = record.ModsFolderPath });
                pbDbContext.ModFilePlayerRecords.Add(newModFilePlayerRecord);
                modFilePlayerRecords.Add(newModFilePlayerRecord);
            }
            foreach (var modFilePlayerRecord in modFileHash.ModFilePlayerRecords)
            {
                if (string.IsNullOrWhiteSpace(editNotes)
                    && editPersonalDate is null)
                {
                    pbDbContext.ModFilePlayerRecords.Remove(modFilePlayerRecord);
                    continue;
                }
                modFilePlayerRecord.Notes = string.IsNullOrWhiteSpace(editNotes)
                    ? null
                    : editNotes;
                modFilePlayerRecord.PersonalDate = editPersonalDate is null
                    ? null
                    : editPersonalDate;
                if (!modFilePlayerRecord.ModFilePlayerRecordPaths.Any(mfprp => mfprp.Path == record.ModsFolderPath))
                    modFilePlayerRecord.ModFilePlayerRecordPaths.Add(new(modFilePlayerRecord) { Path = record.ModsFolderPath });
                if (!modFilePlayerRecord.ModFileHashes.Any(mfh => mfh.Id == modFileHash.Id))
                    modFilePlayerRecord.ModFileHashes.Add(modFileHash);
            }
            await pbDbContext.SaveChangesAsync();
            DataAltered?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<TableData<PersonalNotesRecord>> LoadRecordsAsync(TableState state, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(state);
        try
        {
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var recordsInScope = pbDbContext.ModFiles.Where(mf => mf.FoundAbsent == null).AsQueryable();
            if (state.SortLabel is { } sortLabel
                && !string.IsNullOrWhiteSpace(sortLabel)
                && state.SortDirection is { } sortDirection
                && sortDirection is SortDirection.Ascending or SortDirection.Descending)
            {
                var orderedRecordsInScope = sortLabel switch
                {
                    "FolderPath" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.Path.ToUpper()),
                    "FolderPath" => recordsInScope.OrderBy(mhrr => mhrr.Path.ToUpper()),
                    "FileDate" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.LastWrite),
                    "FileDate" => recordsInScope.OrderBy(mf => mf.LastWrite),
                    "ManifestedName" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.ModFileHash.ModFileManifests.First().Name.ToUpper()),
                    "ManifestedName" => recordsInScope.OrderBy(mhrr => mhrr.ModFileHash.ModFileManifests.First().Name.ToUpper()),
                    "Notes" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.ModFileHash.ModFilePlayerRecords.First().Notes!.ToUpper()),
                    "Notes" => recordsInScope.OrderBy(mf => mf.ModFileHash.ModFilePlayerRecords.First().Notes!.ToUpper()),
                    "PersonalDate" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.ModFileHash.ModFilePlayerRecords.First().PersonalDate),
                    "PersonalDate" => recordsInScope.OrderBy(mf => mf.ModFileHash.ModFilePlayerRecords.First().PersonalDate),
                    _ => throw new Exception("Unsupported sort configuration")
                };
                if (sortDirection is SortDirection.Descending)
                    recordsInScope = orderedRecordsInScope.ThenByDescending(mf => mf.Path.ToUpper());
                else
                    recordsInScope = orderedRecordsInScope.ThenBy(mf => mf.Path.ToUpper());
            }
            else
                recordsInScope = recordsInScope.OrderBy(mf => mf.Path.ToUpper());
            recordsInScope = ApplyFilters(recordsInScope);
            var items = new List<PersonalNotesRecord>();
            await foreach (var dbRecord in recordsInScope
                .Skip(state.Page * state.PageSize)
                .Take(state.PageSize)
                .Select(mf => new
                {
                    mf.Path,
                    mf.LastWrite,
                    ModFilePlayerRecord = mf.ModFileHash.ModFilePlayerRecords.Select(mfpr => new
                    {
                        mfpr.Notes,
                        mfpr.PersonalDate
                    }).FirstOrDefault(),
                    ManifestedName = mf.ModFileHash.ModFileManifests.Select(mfm => mfm.Name).FirstOrDefault()
                }).AsAsyncEnumerable().ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                items.Add(new PersonalNotesRecord
                (
                    new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", dbRecord.Path)),
                    dbRecord.Path,
                    dbRecord.LastWrite,
                    dbRecord.ManifestedName,
                    dbRecord.ModFilePlayerRecord?.Notes,
                    dbRecord.ModFilePlayerRecord?.PersonalDate
                ));
            }
            ModFilesDateLowerBound = await pbDbContext.ModFiles.Where(mf => mf.FoundAbsent == null).MinAsync(mf => mf.LastWrite, token).ConfigureAwait(false) is { } minLastWrite
                && minLastWrite != default
                ? minLastWrite.LocalDateTime
                : null;
            ModFilesDateUpperBound = await pbDbContext.ModFiles.Where(mf => mf.FoundAbsent == null).MaxAsync(mf => mf.LastWrite, token).ConfigureAwait(false) is { } maxLastWrite
                && maxLastWrite != default
                ? maxLastWrite.LocalDateTime
                : null;
            PlayerDataDateLowerBound = await pbDbContext.ModFilePlayerRecords.MinAsync(mfpr => mfpr.PersonalDate, token).ConfigureAwait(false) is { } minPersonalDate
                && minPersonalDate != default
                ? minPersonalDate.LocalDateTime
                : null;
            PlayerDataDateUpperBound = await pbDbContext.ModFilePlayerRecords.MaxAsync(mfpr => mfpr.PersonalDate, token).ConfigureAwait(false) is { } maxPersonalDate
                && maxPersonalDate != default
                ? maxPersonalDate.LocalDateTime
                : null;
            return new TableData<PersonalNotesRecord>
            {
                TotalItems = await recordsInScope.CountAsync(token),
                Items = items
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unhandled exception while loading records");
            return new TableData<PersonalNotesRecord>
            {
                TotalItems = 0,
                Items = []
            };
        }
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public async Task SetAllNotesAsync()
    {
        var copiedBatchNotes = batchNotes;
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var modFileHash in ApplyFilters(pbDbContext.ModFiles.Where(mf => mf.FoundAbsent == null).AsQueryable())
            .Include(mf => mf.ModFileHash)
            .ThenInclude(mfh => mfh.ModFilePlayerRecords)
            .Select(mf => mf.ModFileHash)
            .AsAsyncEnumerable()
            .ConfigureAwait(false))
        {
            if (modFileHash.ModFilePlayerRecords.Count is 0
                && !string.IsNullOrEmpty(copiedBatchNotes))
                modFileHash.ModFilePlayerRecords.Add(new ModFilePlayerRecord());
            foreach (var modFilePlayerRecord in modFileHash.ModFilePlayerRecords)
            {
                modFilePlayerRecord.Notes = copiedBatchNotes;
                if (modFilePlayerRecord.Id != default
                    && string.IsNullOrEmpty(modFilePlayerRecord.Notes)
                    && modFilePlayerRecord.PersonalDate is null)
                    pbDbContext.ModFilePlayerRecords.Remove(modFilePlayerRecord);
            }
        }
        await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
        DataAltered?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetAllPersonalDatesAsync()
    {
        var copiedBatchPersonalDate = batchPersonalDate;
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var modFileHash in ApplyFilters(pbDbContext.ModFiles.Where(mf => mf.FoundAbsent == null).AsQueryable())
            .Include(mf => mf.ModFileHash)
            .ThenInclude(mfh => mfh.ModFilePlayerRecords)
            .Select(mf => mf.ModFileHash)
            .AsAsyncEnumerable()
            .ConfigureAwait(false))
        {
            if (modFileHash.ModFilePlayerRecords.Count is 0
                && copiedBatchPersonalDate is not null)
                modFileHash.ModFilePlayerRecords.Add(new ModFilePlayerRecord());
            foreach (var modFilePlayerRecord in modFileHash.ModFilePlayerRecords)
            {
                modFilePlayerRecord.PersonalDate = copiedBatchPersonalDate;
                if (modFilePlayerRecord.Id != default
                    && string.IsNullOrEmpty(modFilePlayerRecord.Notes)
                    && modFilePlayerRecord.PersonalDate is null)
                    pbDbContext.ModFilePlayerRecords.Remove(modFilePlayerRecord);
            }
        }
        await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
        DataAltered?.Invoke(this, EventArgs.Empty);
    }
}
