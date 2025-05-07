namespace PlumbBuddy.Services;

public class ModHoundClient :
    IModHoundClient
{
    static readonly Uri baseAddress = new($"{schemeAndAuthority}/", UriKind.Absolute);
    const string getReportUrlFormat = "/visitor/track/{0}/{1}";
    const string latestEditedAtAny = "/integrations/latest_edited_at_any";
    static readonly Uri referer = new($"{schemeAndAuthority}{visitorLoad}", UriKind.Absolute);
    const string schemeAndAuthority = "https://app.ts4modhound.com";
    const string visitorLoad = "/visitor/load";
    const string visitorLoadDirectory = "/visitor/load/directory";
    const string visitorTaskStatusUrlFormat = "/visitor/task-status/{0}";
    static readonly JsonSerializerOptions visitorLoadDirectoryRequestBodyJsonSerializerOptions = new() { WriteIndented = false };

    public ModHoundClient(ILogger<ModHoundClient> logger, IPlatformFunctions platformFunctions, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks, IBlazorFramework blazorFramework)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        this.blazorFramework = blazorFramework;
        requestLock = new();
        availableReports = [];
        AvailableReports = new(availableReports);
        searchText = string.Empty;
        selectedReportIncompatibilityRecords = [];
        SelectedReportIncompatibilityRecords = new(selectedReportIncompatibilityRecords);
        selectedReportIncompatibilityRecordsLock = new();
        selectedReportMissingRequirementsRecords = [];
        SelectedReportMissingRequirementsRecords = new(selectedReportMissingRequirementsRecords);
        selectedReportMissingRequirementsRecordsLock = new();
        LoadAvailableReports();
    }

    readonly ObservableCollection<ModHoundReportSelection> availableReports;
    readonly IBlazorFramework blazorFramework;
    int? brokenObsoleteCount;
    int? duplicatesCount;
    int? incompatibleCount;
    readonly ILogger<ModHoundClient> logger;
    int? missingRequirementsCount;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    int? notTrackedCount;
    int? outdatedCount;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    int? progressValue;
    int? progressMax;
    readonly AsyncLock requestLock;
    int? requestPhase;
    string searchText;
    ModHoundReportSelection? selectedReport;
    readonly ObservableRangeCollection<ModHoundReportIncompatibilityRecord> selectedReportIncompatibilityRecords;
    readonly AsyncLock selectedReportIncompatibilityRecordsLock;
    readonly ObservableRangeCollection<ModHoundReportMissingRequirementsRecord> selectedReportMissingRequirementsRecords;
    readonly AsyncLock selectedReportMissingRequirementsRecordsLock;
    string? selectedReportSection;
    readonly ISettings settings;
    string? status;
    readonly ISuperSnacks superSnacks;
    int? unknownStatusCount;
    int? upToDateCount;

    public ReadOnlyObservableCollection<ModHoundReportSelection> AvailableReports { get; }

    public int? BrokenObsoleteCount
    {
        get => brokenObsoleteCount;
        private set
        {
            if (brokenObsoleteCount == value)
                return;
            brokenObsoleteCount = value;
            OnPropertyChanged();
        }
    }

    public int? DuplicatesCount
    {
        get => duplicatesCount;
        private set
        {
            if (duplicatesCount == value)
                return;
            duplicatesCount = value;
            OnPropertyChanged();
        }
    }

    public int? IncompatibleCount
    {
        get => incompatibleCount;
        private set
        {
            if (incompatibleCount == value)
                return;
            incompatibleCount = value;
            OnPropertyChanged();
        }
    }

    public int? MissingRequirementsCount
    {
        get => missingRequirementsCount;
        private set
        {
            if (missingRequirementsCount == value)
                return;
            missingRequirementsCount = value;
            OnPropertyChanged();
        }
    }

    public int? NotTrackedCount
    {
        get => notTrackedCount;
        private set
        {
            if (notTrackedCount == value)
                return;
            notTrackedCount = value;
            OnPropertyChanged();
        }
    }

    public int? OutdatedCount
    {
        get => outdatedCount;
        private set
        {
            if (outdatedCount == value)
                return;
            outdatedCount = value;
            OnPropertyChanged();
        }
    }

    public int? ProgressMax
    {
        get => progressMax;
        private set
        {
            if (progressMax == value)
                return;
            progressMax = value;
            OnPropertyChanged();
        }
    }

    public int? ProgressValue
    {
        get => progressValue;
        private set
        {
            if (progressValue == value)
                return;
            progressValue = value;
            OnPropertyChanged();
        }
    }

    public int? RequestPhase
    {
        get => requestPhase;
        private set
        {
            if (requestPhase == value)
                return;
            requestPhase = value;
            OnPropertyChanged();
        }
    }

    public string SearchText
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

    public ModHoundReportSelection? SelectedReport
    {
        get => selectedReport;
        set
        {
            if (selectedReport == value)
                return;
            selectedReport = value;
            OnPropertyChanged();
            UpdateSectionCounts();
            UpdateSelectedReportIncompatibilityRecords();
            UpdateSelectedReportMissingRequirementsRecords();
        }
    }

    public ReadOnlyObservableCollection<ModHoundReportIncompatibilityRecord> SelectedReportIncompatibilityRecords { get; }

    public ReadOnlyObservableCollection<ModHoundReportMissingRequirementsRecord> SelectedReportMissingRequirementsRecords { get; }

    public string? SelectedReportSection
    {
        get => selectedReportSection;
        set
        {
            if (selectedReportSection == value)
                return;
            selectedReportSection = value;
            OnPropertyChanged();
            UpdateSelectedReportIncompatibilityRecords();
            UpdateSelectedReportMissingRequirementsRecords();
        }
    }

    public string? Status
    {
        get => status;
        private set
        {
            if (status == value)
                return;
            status = value;
            OnPropertyChanged();
        }
    }

    public int? UnknownStatusCount
    {
        get => unknownStatusCount;
        private set
        {
            if (unknownStatusCount == value)
                return;
            unknownStatusCount = value;
            OnPropertyChanged();
        }
    }

    public int? UpToDateCount
    {
        get => upToDateCount;
        private set
        {
            if (upToDateCount == value)
                return;
            upToDateCount = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void ClearCounts()
    {
        BrokenObsoleteCount = null;
        DuplicatesCount = null;
        IncompatibleCount = null;
        MissingRequirementsCount = null;
        OutdatedCount = null;
        UnknownStatusCount = null;
        UpToDateCount = null;
    }

    void LoadAvailableReports() =>
        _ = Task.Run(LoadAvailableReportsAsync);

    async Task LoadAvailableReportsAsync()
    {
        var existingReports = availableReports.ToDictionary(mhrs => mhrs.Id);
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var report in pbDbContext.ModHoundReports.AsAsyncEnumerable().ConfigureAwait(false))
        {
            var wasSelected = false;
            if (existingReports.TryGetValue(report.Id, out var existingReport))
            {
                wasSelected = selectedReport == existingReport;
                existingReports.Remove(report.Id);
                availableReports.Remove(existingReport);
            }
            var newReport = new ModHoundReportSelection(report.Id, report.Retrieved);
            availableReports.Add(newReport);
            if (wasSelected)
                SelectedReport = newReport;
        }
        foreach (var remainingReport in existingReports.Values)
            availableReports.Remove(remainingReport);
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public void RequestReport() =>
        _ = Task.Run(RequestReportAsync);

    async Task RequestReportAsync()
    {
        IDisposable? requestLockHeld = null;
        try
        {
            requestLockHeld = await requestLock.LockAsync(new CancellationToken(true)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        if (requestLockHeld is null)
        {
            superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Normal_RequestAlreadyInProgress), Severity.Normal, options => options.Icon = MaterialDesignIcons.Normal.HandBackLeft);
            return;
        }
        try
        {
            Status = AppText.ModHoundClient_Status_WaitingForModsDirectoryCataloger;
            RequestPhase = 1;
            await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
            Status = AppText.ModHoundClient_Status_PreparingRequest;
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var files = (await pbDbContext.ModFiles // get me dem mod files MDC
                .Where
                (
                    mf =>
                    mf.FileType == ModsDirectoryFileType.Package // if it's a package
                    && mf.Path.Length - mf.Path.Replace("/", string.Empty).Replace("\\", string.Empty).Length <= 5 // and 5 folders deep or less
                    || mf.FileType == ModsDirectoryFileType.ScriptArchive // or it's a script mod
                    && mf.Path.Length - mf.Path.Replace("/", string.Empty).Replace("\\", string.Empty).Length <= 1 // and 1 folder deep or less
                )
                .OrderBy(mf => mf.Path) // put them in order like a gentleman
                .Select(mf => new
                {
                    Path = mf.Path.Replace("\\", "/"), // MH thinks in POSIX
                    mf.Creation,
                    mf.LastWrite
                })
                .ToListAsync()
                .ConfigureAwait(false))
                .Select(mf => new
                {
                    name = mf.Path[(mf.Path.LastIndexOf('/') + 1)..mf.Path.LastIndexOf('.')],
                    date = (mf.Creation < mf.LastWrite ? mf.Creation : mf.LastWrite).LocalDateTime.ToString("MM/dd/yyyy, hh:mm:ss tt", CultureInfo.InvariantCulture),
                    extension = mf.Path[(mf.Path.LastIndexOf('.') + 1)..],
                    fullPath = $"Mods/{mf.Path}"
                })
                .ToImmutableArray();
            var exclusionTests = settings.ModHoundExcludePackagesMode switch
            {
                ModHoundExcludePackagesMode.Patterns => settings.ModHoundPackagesExclusions.Select(exclusion =>
                {
                    try
                    {
                        var pattern = new Regex(exclusion, RegexOptions.IgnoreCase);
                        return (Func<string, bool>)(path => pattern.IsMatch(path));
                    }
                    catch (RegexParseException ex)
                    {
                        logger.LogWarning(ex, "failed to parse Mod Hound packages exclusion regular expression: {Pattern}", exclusion);
                        superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModHoundClient_Snack_Error_InvalidPackageExclusionRegex, exclusion)), Severity.Error, options =>
                        {
                            options.Icon = MaterialDesignIcons.Normal.Regex;
                            options.Action = AppText.ModHoundClient_SnackAction_LoadSandbox;
                            options.OnClick = async _ =>
                            {
                                if (await blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().ShowQuestionDialogAsync(AppText.ModHoundClient_Question_PackageFilesInClipboard_Caption, AppText.ModHoundClient_Question_PackageFilesInClipboard_Text) ?? false)
                                    await Clipboard.SetTextAsync(string.Join(Environment.NewLine, files.Select(file => file.fullPath[5..])));
                                await Browser.OpenAsync($"https://regex101.com/?regex={Uri.EscapeDataString(exclusion)}&flags=gim&flavor=dotnet", BrowserLaunchMode.External);
                            };
                            options.RequireInteraction = true;
                        });
                        throw;
                    }
                }),
                _ => settings.ModHoundPackagesExclusions.Select(exclusion => (Func<string, bool>)(path => path.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase)))
            };
            var postDirectoryRequestBodyObject = new
            {
                files = files.Where(file => !file.extension.Equals("package", StringComparison.OrdinalIgnoreCase) || !exclusionTests.Any(exclusionTest => exclusionTest(file.fullPath[5..]))).ToImmutableArray(),
                ignoreFolder = false,
                ignoreFolderName = "CC",
                timeZone = TZConvert.WindowsToIana(TimeZoneInfo.Local.StandardName)
            };
            var postDirectoryRequestBodyJson = JsonSerializer.Serialize(postDirectoryRequestBodyObject, visitorLoadDirectoryRequestBodyJsonSerializerOptions);
            byte[] postDirectoryRequestBodyJsonSha256;
            using (var postDirectoryRequestBodyJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(postDirectoryRequestBodyJson)))
                postDirectoryRequestBodyJsonSha256 = await SHA256.HashDataAsync(postDirectoryRequestBodyJsonStream).ConfigureAwait(false);
            Status = AppText.ModHoundClient_Status_SubmittingRequest;
            var cookieContainer = new CookieContainer();
            using var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
            };
            using var httpClient = new HttpClient(httpClientHandler) { BaseAddress = baseAddress };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(platformFunctions.UserAgent);
            httpClient.DefaultRequestHeaders.Add("Origin", schemeAndAuthority);
            var latestEditedAtAnyRequest = await httpClient.GetAsync(latestEditedAtAny).ConfigureAwait(false);
            try
            {
                latestEditedAtAnyRequest.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Mod Hound's response from GET {latestEditedAtAny} returned non-successful status code: {{StatusCode}}", latestEditedAtAnyRequest.StatusCode);
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var latestEditedAtAnyResponse = await latestEditedAtAnyRequest.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!DateTimeOffset.TryParse(latestEditedAtAnyResponse, null, DateTimeStyles.AssumeUniversal, out var latestEditedAtAnyDateTimeOffset))
            {
                logger.LogWarning($"Mod Hound's response from GET {latestEditedAtAny} returned text not intelligible as a UTC date: {{Text}}", latestEditedAtAnyResponse);
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            if (await pbDbContext.ModHoundReports.Where(mhr => mhr.RequestSha256 == postDirectoryRequestBodyJsonSha256 && mhr.LastEditedAtAny >= latestEditedAtAnyDateTimeOffset).OrderByDescending(mhr => mhr.Retrieved).FirstOrDefaultAsync().ConfigureAwait(false) is { } freshIdenticalReport)
            {
                if (availableReports.FirstOrDefault(mhrs => mhrs.Id == freshIdenticalReport.Id) is { } availableReport)
                    SelectedReport = availableReport;
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModHoundClient_Snack_Success_MatchingCachedReport, freshIdenticalReport.Retrieved.Humanize())), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.TableSync);
                return;
            }
            var packageFilesBeingSent = postDirectoryRequestBodyObject.files.Count(file => file.extension.Equals("package", StringComparison.OrdinalIgnoreCase));
            if (packageFilesBeingSent > IModHoundClient.PackagesBatchHardLimit)
            {
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModHoundClient_Snack_Error_PackageHardLimitExceeded, packageFilesBeingSent)), Severity.Error, options =>
                {
                    options.Action = AppText.ModHoundClient_SnackAction_ExcludeCC;
                    options.Icon = MaterialDesignIcons.Normal.TableOff;
                    options.OnClick = _ => blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().ShowSettingsDialogAsync(4);
                    options.RequireInteraction = true;
                });
                return;
            }
            if (packageFilesBeingSent > IModHoundClient.PackagesBatchWarningThreshold)
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModHoundClient_Snack_Warning_ExcessiveNumberOfPackages, packageFilesBeingSent)), Severity.Warning, options =>
                {
                    options.Action = AppText.ModHoundClient_SnackAction_ExcludeCC;
                    options.Icon = MaterialDesignIcons.Normal.TableClock;
                    options.OnClick = _ => blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().ShowSettingsDialogAsync(4);
                    options.RequireInteraction = true;
                });
            using var checkModFilesPageRequest = await httpClient.GetAsync(visitorLoad).ConfigureAwait(false);
            try
            {
                checkModFilesPageRequest.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Mod Hound's response from GET {visitorLoad} returned non-successful status code: {{StatusCode}}", checkModFilesPageRequest.StatusCode);
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var checkModFilesPageResponse = await checkModFilesPageRequest.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var checkModFilesPageBrowsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
            var checkModFilesPageDocument = await checkModFilesPageBrowsingContext.OpenAsync(dr => dr.Content(checkModFilesPageResponse)).ConfigureAwait(false);
            if (checkModFilesPageDocument.QuerySelector("meta[name=\"csrf-token\"]") is not { } csrfTokenMetaTag
                || csrfTokenMetaTag.GetAttribute("content") is not { } csrfToken
                || string.IsNullOrWhiteSpace(csrfToken))
            {
                logger.LogWarning($"Mod Hound's response from GET {visitorLoad} returned markup without a discoverable CSRF token");
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            httpClient.DefaultRequestHeaders.Referrer = referer;
            httpClient.DefaultRequestHeaders.Add("X-CSRFToken", csrfToken);
            using var postDirectoryRequestBody = new StringContent(postDirectoryRequestBodyJson, new MediaTypeHeaderValue("application/json"));
            using var postDirectoryRequest = await httpClient.PostAsync(visitorLoadDirectory, postDirectoryRequestBody).ConfigureAwait(false);
            try
            {
                postDirectoryRequest.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Mod Hound's response from POST {visitorLoadDirectory} returned non-successful status code: {{StatusCode}}", postDirectoryRequest.StatusCode);
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var postDirectoryResponse = await postDirectoryRequest.Content.ReadFromJsonAsync<PostDirectoryResponse>().ConfigureAwait(false);
            if (postDirectoryResponse?.TaskId is not { } taskId)
            {
                logger.LogWarning($"Mod Hound's response from POST {visitorLoadDirectory} did not contain a task ID: {{Response}}", await postDirectoryRequest.Content.ReadAsStringAsync().ConfigureAwait(false));
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            Status = AppText.ModHoundClient_Status_AwaitingResponse;
            RequestPhase = 2;
            GetTaskStatusResponse? lastTaskStatusResponse = null;
            var visitorTaskStatusUrl = string.Format(visitorTaskStatusUrlFormat, taskId);
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                var taskStatusRequest = await httpClient.GetAsync(visitorTaskStatusUrl).ConfigureAwait(false);
                try
                {
                    taskStatusRequest.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Mod Hound's response from getting the task status for {TaskId} returned non-successful status code: {StatusCode}", taskId, taskStatusRequest.StatusCode);
                    superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                    return;
                }
                lastTaskStatusResponse = await taskStatusRequest.Content.ReadFromJsonAsync<GetTaskStatusResponse>().ConfigureAwait(false);
                if (lastTaskStatusResponse is null)
                {
                    logger.LogWarning("Mod Hound's response from getting the task status for {TaskId} could not be understood: {Response}", taskId, await taskStatusRequest.Content.ReadAsStringAsync().ConfigureAwait(false));
                    superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                    break;
                }
                if (lastTaskStatusResponse.State is not (TaskStatusState.PENDING or TaskStatusState.STARTED or TaskStatusState.PROCESSING))
                    break;
                Status = string.Format(AppText.ModHoundClient_Status_RequestQueueState, lastTaskStatusResponse.State switch
                {
                    TaskStatusState.PENDING => AppText.ModHoundClient_Status_RequestQueueState_Pending,
                    TaskStatusState.STARTED => AppText.ModHoundClient_Status_RequestQueueState_Started,
                    _ => AppText.ModHoundClient_Status_RequestQueueState_Default
                });
                ProgressMax = lastTaskStatusResponse.Total;
                ProgressValue = lastTaskStatusResponse.Current;
            }
            Status = AppText.ModHoundClient_Status_ReadingResponse;
            RequestPhase = 3;
            ProgressMax = 9;
            ProgressValue = 0;
            if (lastTaskStatusResponse is null)
                return;
            if (lastTaskStatusResponse.State is TaskStatusState.FAILURE)
            {
                logger.LogWarning("Mod Hound's response from getting the task status for {TaskId} returned failure state with error: {Error}", taskId, lastTaskStatusResponse.Error);
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModHoundClient_Snack_Error_DetailedScrapWithTheDog, WebUtility.HtmlEncode(lastTaskStatusResponse.Error))), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            if (lastTaskStatusResponse.ResultId is not { } resultId
                || string.IsNullOrWhiteSpace(resultId))
            {
                logger.LogWarning("Mod Hound's response from getting the task status for {TaskId} produced a blank result ID", taskId);
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var getReportRequest = await httpClient.GetAsync(string.Format(getReportUrlFormat, lastTaskStatusResponse.ResultId, lastTaskStatusResponse.Type)).ConfigureAwait(false);
            try
            {
                getReportRequest.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mod Hound's response from getting the report for result {ResultId} of type {Type} returned non-successful status code: {StatusCode}", lastTaskStatusResponse.ResultId, lastTaskStatusResponse.Type, getReportRequest.StatusCode);
                superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var getReportResponse = await getReportRequest.Content.ReadAsStringAsync().ConfigureAwait(false);
            var modHoundReport = new ModHoundReport
            {
                LastEditedAtAny = latestEditedAtAnyDateTimeOffset,
                ReportHtml = getReportResponse,
                RequestSha256 = postDirectoryRequestBodyJsonSha256,
                ResultId = lastTaskStatusResponse.ResultId,
                Retrieved = DateTimeOffset.UtcNow,
                TaskId = taskId
            };
            using var reportPageBrowsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
            var reportPageDocument = await checkModFilesPageBrowsingContext.OpenAsync(dr => dr.Content(getReportResponse)).ConfigureAwait(false);
            string[] statusedRecordTableIds =
            [
                "outdatedTable",
                "duplicatesTable",
                "brokensTable",
                "unknownstatusesTable",
                "uptodatesTable"
            ];
            for (var i = 0; i <= 4; ++i)
            {
                ++ProgressValue;
                if (reportPageDocument.GetElementById(statusedRecordTableIds[i]) is not { } table)
                    continue;
                var status = (ModHoundReportRecordStatus)i;
                foreach (var htmlRecord in table.QuerySelectorAll("tbody > tr"))
                {
                    if (htmlRecord is null)
                        continue;
                    var record = new ModHoundReportRecord(modHoundReport)
                    {
                        CreatorName = htmlRecord.QuerySelector("td:nth-child(3)")?.TextContent.Trim() ?? string.Empty,
                        FileName = htmlRecord.QuerySelector("td:nth-child(1)")?.TextContent?.Trim() ?? string.Empty,
                        FilePath = htmlRecord.QuerySelector("td:nth-child(1) > div[title]")?.GetAttribute("title")?.Trim() ?? string.Empty,
                        ModName = htmlRecord.QuerySelector("td:nth-child(2)")?.TextContent.Trim() ?? string.Empty,
                        Status = status
                    };
                    if (record.FilePath.StartsWith("Location:"))
                        record.FilePath = record.FilePath[9..].Trim();
                    if (htmlRecord.QuerySelector("td:nth-child(5) > div[data-utc-time-date-only]")?.GetAttribute("data-utc-time-date-only") is { } dateOfInstalledFileStr
                        && !string.IsNullOrWhiteSpace(dateOfInstalledFileStr)
                        && DateTimeOffset.TryParse(dateOfInstalledFileStr, out var dateOfInstalledFile))
                    {
                        record.DateOfInstalledFile = dateOfInstalledFile;
                        record.DateOfInstalledFileString = dateOfInstalledFile.ToLocalTime().ToString("g");
                    }
                    else if (htmlRecord.QuerySelector("td:nth-child(5)")?.TextContent is { } lessPreciseDateOfInstalledFileStr
                        && !string.IsNullOrWhiteSpace(lessPreciseDateOfInstalledFileStr)
                        && DateTimeOffset.TryParse(lessPreciseDateOfInstalledFileStr.Trim(), out var lessPreciseDateOfInstalledFile))
                    {
                        record.DateOfInstalledFile = lessPreciseDateOfInstalledFile;
                        record.DateOfInstalledFileString = lessPreciseDateOfInstalledFile.ToString("d");
                    }
                    if (htmlRecord.QuerySelector("td:nth-child(4) > div[data-utc-time-date-only]")?.GetAttribute("data-utc-time-date-only") is { } lastUpdateDateStr
                        && !string.IsNullOrWhiteSpace(lastUpdateDateStr)
                        && DateTimeOffset.TryParse(lastUpdateDateStr, out var lastUpdateDate))
                    {
                        record.LastUpdateDate = lastUpdateDate;
                        record.LastUpdateDateString = lastUpdateDate.ToLocalTime().ToString("g");
                    }
                    else if (htmlRecord.QuerySelector("td:nth-child(4)")?.TextContent is { } lessPreciseLastUpdateDateStr
                        && !string.IsNullOrWhiteSpace(lessPreciseLastUpdateDateStr)
                        && DateTimeOffset.TryParse(lessPreciseLastUpdateDateStr, out var lessPreciseLastUpdateDate))
                    {
                        record.LastUpdateDate = lessPreciseLastUpdateDate;
                        record.LastUpdateDateString = lessPreciseLastUpdateDate.ToString("d");
                    }
                    if (htmlRecord.QuerySelector("td:nth-last-child(2)") is { } modLinkOrIndexCell)
                    {
                        if (modLinkOrIndexCell.QuerySelector("div > a[href]") is { } modLinkOrIndexAnchor
                            && modLinkOrIndexAnchor.GetAttribute("href") is { } modLinkOrIndexHrefStr
                            && !string.IsNullOrWhiteSpace(modLinkOrIndexHrefStr)
                            && Uri.TryCreate(modLinkOrIndexHrefStr, UriKind.Absolute, out var modLinkOrIndexHref))
                            record.ModLinkOrIndexHref = modLinkOrIndexHref;
                        if (modLinkOrIndexCell.TextContent is { } modLinkOrIndexText
                            && !string.IsNullOrWhiteSpace(modLinkOrIndexText))
                            record.ModLinkOrIndexText = modLinkOrIndexText.Trim();
                    }
                    if (htmlRecord.QuerySelector("td:nth-last-child(1)")?.TextContent is { } updateNotes
                        && !string.IsNullOrWhiteSpace(updateNotes))
                        record.UpdateNotes = updateNotes.Trim();
                    modHoundReport.Records.Add(record);
                }
            }
            ++ProgressValue;
            if (reportPageDocument.GetElementById("incompsDetailsPane") is { } incompatibilityDetailsPane)
                foreach (var orderedList in incompatibilityDetailsPane.QuerySelectorAll("ol"))
                {
                    if (orderedList is null)
                        continue;
                    var record = new ModHoundReportIncompatibilityRecord(modHoundReport);
                    foreach (var listItem in orderedList.QuerySelectorAll("li"))
                        if (listItem.TextContent is { } textContent
                            && !string.IsNullOrWhiteSpace(textContent))
                            record.Parts.Add(new(record)
                            {
                                FilePath = listItem.GetAttribute("title")?.Trim() ?? string.Empty,
                                Label = textContent.Trim()
                            });
                    modHoundReport.IncompatibilityRecords.Add(record);
                }
            ++ProgressValue;
            if (reportPageDocument.GetElementById("missingreqsDetailsPane") is { } missingRequirementsDetailsPane)
                foreach (var htmlRecord in missingRequirementsDetailsPane.QuerySelectorAll("div"))
                {
                    if (htmlRecord is null)
                        continue;
                    var unorderedLists = htmlRecord.QuerySelectorAll("ul").ToImmutableArray();
                    if (unorderedLists.Length is not 2)
                        continue;
                    var record = new ModHoundReportMissingRequirementsRecord(modHoundReport);
                    foreach (var listItem in unorderedLists[0].QuerySelectorAll("li"))
                        if (listItem.TextContent is { } textContent
                            && !string.IsNullOrWhiteSpace(textContent))
                            record.Dependents.Add(new(record) { Label = textContent.Trim() });
                    foreach (var listItem in unorderedLists[1].QuerySelectorAll("li"))
                    {
                        if (listItem.QuerySelector("b") is not { } boldTag
                            || boldTag.TextContent is not { } label
                            || string.IsNullOrWhiteSpace(label))
                            continue;
                        var dependency = new ModHoundReportMissingRequirementsRecordDependency(record) { Label = label };
                        if (listItem.QuerySelector("span.basic-hint a[href]") is { } anchorTag
                            && anchorTag.TextContent is { } anchorTextContent
                            && !string.IsNullOrWhiteSpace(anchorTextContent)
                            && anchorTag.GetAttribute("href") is { } hrefStr
                            && !string.IsNullOrWhiteSpace(hrefStr)
                            && Uri.TryCreate(hrefStr, UriKind.Absolute, out var href))
                        {
                            dependency.ModLinkOrIndexText = anchorTextContent.Trim();
                            dependency.ModLinkOrIndexHref = href;
                        }
                        record.Dependencies.Add(dependency);
                    }
                    modHoundReport.MissingRequirementsRecords.Add(record);
                }
            ++ProgressValue;
            if (reportPageDocument.GetElementById("nottrackedsTable") is { } notTrackedTable)
                foreach (var htmlRecord in notTrackedTable.QuerySelectorAll("tbody > tr"))
                {
                    if (htmlRecord is null)
                        continue;
                    var record = new ModHoundReportNotTrackedRecord(modHoundReport)
                    {
                        FileName = htmlRecord.QuerySelector("td:nth-child(1)")?.TextContent.Trim() ?? string.Empty
                    };
                    if (htmlRecord.QuerySelector("td:nth-child(2)")?.TextContent is { } fileTypeStr
                        && !string.IsNullOrWhiteSpace(fileTypeStr)
                        && Enum.TryParse<ModHoundReportNotTrackedRecordFileType>(fileTypeStr.Trim(), true, out var fileType))
                        record.FileType = fileType;
                    if (htmlRecord.QuerySelector("td:nth-child(3)")?.TextContent is { } fileDateStr
                        && !string.IsNullOrWhiteSpace(fileDateStr)
                        && DateTimeOffset.TryParse(fileDateStr.Trim(), out var fileDate))
                    {
                        record.FileDate = fileDate;
                        record.FileDateString = fileDate.ToString("d");
                    }
                    modHoundReport.NotTrackedRecords.Add(record);
                }
            Status = AppText.ModHoundClient_Status_SavingReport;
            RequestPhase = 4;
            ProgressMax = null;
            ProgressValue = null;
            await pbDbContext.ModHoundReports.AddAsync(modHoundReport).ConfigureAwait(false);
            await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            await LoadAvailableReportsAsync().ConfigureAwait(false);
            if (availableReports.FirstOrDefault(mhrs => mhrs.Id == modHoundReport.Id) is { } newlyAvailableReport)
                SelectedReport = newlyAvailableReport;
            superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Success_ReportReady), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.TableCheck);
        }
        catch (RegexParseException)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"unhandled exception while attempting to request, parse, and commit Mod Hound report");
            superSnacks.OfferRefreshments(new MarkupString(AppText.ModHoundClient_Snack_Error_ScrapWithTheDog), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
            return;
        }
        finally
        {
            Status = null;
            RequestPhase = null;
            ProgressMax = null;
            ProgressValue = null;
            requestLockHeld.Dispose();
        }
    }

    void UpdateSectionCounts() =>
        _ = Task.Run(UpdateSectionCountsAsync);

    async Task UpdateSectionCountsAsync()
    {
        if (selectedReport is not { } report)
        {
            ClearCounts();
            return;
        }
        try
        {
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var counts = await pbDbContext.ModHoundReports
                .Where(mhr => mhr.Id == report.Id)
                .Select(mhr => new
                {
                    BrokenObsoleteCount = mhr.Records.Where(mhrr => mhrr.Status == ModHoundReportRecordStatus.BrokenObsoleteMatches).Count(),
                    DuplicatesCount = mhr.Records.Where(mhrr => mhrr.Status == ModHoundReportRecordStatus.DuplicateMatches).Count(),
                    IncompatibleCount = mhr.IncompatibilityRecords.Count(),
                    MissingRequirementsCount = mhr.MissingRequirementsRecords.Count(),
                    NotTrackedCount = mhr.NotTrackedRecords.Count(),
                    OutdatedCount = mhr.Records.Where(mhrr => mhrr.Status == ModHoundReportRecordStatus.OutdatedMatches).Count(),
                    UnknownStatusCount = mhr.Records.Where(mhrr => mhrr.Status == ModHoundReportRecordStatus.UnknownStatusMatches).Count(),
                    UpToDateCount = mhr.Records.Where(mhrr => mhrr.Status == ModHoundReportRecordStatus.UpToDateOkayMatches).Count()
                })
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if (counts is null)
            {
                ClearCounts();
                return;
            }
            BrokenObsoleteCount = counts.BrokenObsoleteCount;
            DuplicatesCount = counts.DuplicatesCount;
            IncompatibleCount = counts.IncompatibleCount;
            MissingRequirementsCount = counts.MissingRequirementsCount;
            NotTrackedCount = counts.NotTrackedCount;
            OutdatedCount = counts.OutdatedCount;
            UnknownStatusCount = counts.UnknownStatusCount;
            UpToDateCount = counts.UpToDateCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to retrieve section counts for report {ReportId}", report.Id);
        }
    }

    void UpdateSelectedReportIncompatibilityRecords() =>
        _ = Task.Run(UpdateSelectedReportIncompatibilityRecordsAsync);

    async Task UpdateSelectedReportIncompatibilityRecordsAsync()
    {
        using var selectedReportIncompatibilityRecordsLockHeld = await selectedReportIncompatibilityRecordsLock.LockAsync().ConfigureAwait(false);
        if (selectedReport?.Id is not { } reportId
            || reportId is <= 0
            || selectedReportSection != IModHoundClient.SectionIncompatible)
        {
            if (selectedReportIncompatibilityRecords.Any())
                selectedReportIncompatibilityRecords.Clear();
            return;
        }
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        selectedReportIncompatibilityRecords.Reset(await pbDbContext.ModHoundReportIncompatibilityRecords
            .Where(mhrir => mhrir.ModHoundReportId == reportId)
            .Include(mhrir => mhrir.Parts)
            .ToListAsync()
            .ConfigureAwait(false));
    }

    void UpdateSelectedReportMissingRequirementsRecords() =>
        _ = Task.Run(UpdateSelectedReportMissingRequirementsRecordsAsync);

    async Task UpdateSelectedReportMissingRequirementsRecordsAsync()
    {
        using var selectedReportMissingRequirementsRecordsLockHeld = await selectedReportMissingRequirementsRecordsLock.LockAsync().ConfigureAwait(false);
        if (selectedReport?.Id is not { } reportId
            || reportId is <= 0
            || selectedReportSection != IModHoundClient.SectionMissingRequirements)
        {
            if (selectedReportMissingRequirementsRecords.Any())
                selectedReportMissingRequirementsRecords.Clear();
            return;
        }
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        selectedReportMissingRequirementsRecords.Reset(await pbDbContext.ModHoundReportMissingRequirementsRecords
            .Where(mhrmr => mhrmr.ModHoundReportId == reportId)
            .Include(mhrmr => mhrmr.Dependencies)
            .Include(mhrmr => mhrmr.Dependents)
            .ToListAsync()
            .ConfigureAwait(false));
    }
}

file class PostDirectoryResponse
{
    [JsonPropertyName("task_id")]
    public Guid TaskId { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
file enum TaskStatusState
{
    PENDING,
    STARTED,
    PROCESSING,
    SUCCESS,
    FAILURE
}

file class GetTaskStatusResponse
{
    [JsonPropertyName("current")]
    public int? Current { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("result_id")]
    public string? ResultId { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("state")]
    public TaskStatusState State { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

file class Regex101CreateEntryResponse
{
    [JsonPropertyName("deleteCode")]
    public string? DeleteCode { get; set; }

    [JsonPropertyName("permalinkFragment")]
    public string? PermalinkFragment { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("isLibraryEntry")]
    public bool IsLibraryEntry { get; set; }
}