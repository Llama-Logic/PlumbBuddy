namespace PlumbBuddy.Services;

public class PersonalNotes :
    IPersonalNotes
{
    public PersonalNotes(ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
    }

    string? editNotes;
    DateTime? editPersonalDate;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    DateTime? personalDateLowerBound;
    DateTime? personalDateUpperBound;
    string? searchText;
    readonly ISettings settings;

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
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync();
            var path = string.IsNullOrWhiteSpace(record.FolderPath)
                ? record.FileName
                : Path.Combine(record.FolderPath, record.FileName);
            var modFileHash = await pbDbContext.ModFiles
                .Where(mf => mf.Path == path)
                .Select(mf => mf.ModFileHash)
                .FirstOrDefaultAsync();
            if (modFileHash is null)
                return;
            var modFilePlayerRecords = await pbDbContext.ModFilePlayerRecords
                .Where(mfpr => mfpr.ModFileHashes.Any(mfh => mfh.Id == modFileHash.Id))
                .Include(mfpr => mfpr.ModFileHashes)
                .Include(mfpr => mfpr.ModFilePlayerRecordPaths)
                .ToListAsync();
            if (modFilePlayerRecords.Count is 0
                && (!string.IsNullOrWhiteSpace(editNotes) || editPersonalDate is not null))
            {
                var newModFilePlayerRecord = new ModFilePlayerRecord();
                newModFilePlayerRecord.ModFileHashes.Add(modFileHash);
                newModFilePlayerRecord.ModFilePlayerRecordPaths.Add(new ModFilePlayerRecordPath(newModFilePlayerRecord) { Path = path });
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
                if (!modFilePlayerRecord.ModFilePlayerRecordPaths.Any(mfprp => mfprp.Path == path))
                    modFilePlayerRecord.ModFilePlayerRecordPaths.Add(new(modFilePlayerRecord) { Path = path });
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
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
        var recordsInScope = pbDbContext.ModFiles.AsQueryable();
        if (state.SortLabel is { } sortLabel
            && !string.IsNullOrWhiteSpace(sortLabel)
            && state.SortDirection is { } sortDirection
            && sortDirection is SortDirection.Ascending or SortDirection.Descending)
        {
            var orderedRecordsInScope = sortLabel switch
            {
                "FolderPath" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.FolderPath.ToUpper()),
                "FolderPath" => recordsInScope.OrderBy(mhrr => mhrr.FolderPath.ToUpper()),
                "FileName" when sortDirection is SortDirection.Descending => recordsInScope.OrderByDescending(mf => mf.FileName.ToUpper()),
                "FileName" => recordsInScope.OrderBy(mhrr => mhrr.FileName.ToUpper()),
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
        var copiedSearchText = searchText;
        if (!string.IsNullOrWhiteSpace(copiedSearchText))
        {
            var likePattern = $"%{copiedSearchText.Trim()}%";
            recordsInScope = recordsInScope
                .Where
                (
                    mf =>
                    EF.Functions.Like(mf.Path, likePattern)
                    || mf.ModFileHash.ModFileManifests.Any(mfm => EF.Functions.Like(mfm.Name, likePattern))
                    || mf.ModFileHash.ModFilePlayerRecords.Any(mfpr => EF.Functions.Like(mfpr.Notes, likePattern))
                );
        }
        var copiedPersonalDateLowerBound = personalDateLowerBound;
        if (copiedPersonalDateLowerBound is not null)
            recordsInScope = recordsInScope.Where(mf => mf.ModFileHash.ModFilePlayerRecords.Any(mfpr => mfpr.PersonalDate != null && mfpr.PersonalDate >= copiedPersonalDateLowerBound));
        var copiedPersonalDateUpperBound = personalDateUpperBound;
        if (copiedPersonalDateUpperBound is not null)
            recordsInScope = recordsInScope.Where(mf => mf.ModFileHash.ModFilePlayerRecords.Any(mfpr => mfpr.PersonalDate != null && mfpr.PersonalDate <= copiedPersonalDateUpperBound));
        var items = new List<PersonalNotesRecord>();
        await foreach (var dbRecord in recordsInScope
            .Skip(state.Page * state.PageSize)
            .Take(state.PageSize)
            .Select(mf => new
            {
                mf.Path,
                mf.FolderPath,
                mf.FileName,
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
                dbRecord.FolderPath,
                dbRecord.FileName,
                dbRecord.ManifestedName,
                dbRecord.ModFilePlayerRecord?.Notes,
                dbRecord.ModFilePlayerRecord?.PersonalDate
            ));
        }
        return new TableData<PersonalNotesRecord>
        {
            TotalItems = await recordsInScope.CountAsync(token),
            Items = items
        };
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}
