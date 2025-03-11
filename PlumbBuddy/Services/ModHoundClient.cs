namespace PlumbBuddy.Services;

public class ModHoundClient :
    IModHoundClient
{
    static readonly Uri baseAddress = new($"{schemeAndAuthority}/", UriKind.Absolute);
    static readonly Uri referer = new($"{schemeAndAuthority}{visitorLoad}", UriKind.Absolute);
    const string schemeAndAuthority = "https://app.ts4modhound.com";
    const string visitorLoad = "/visitor/load";
    const string visitorLoadDirectory = "/visitor/load/directory";
    static readonly JsonSerializerOptions visitorLoadDirectoryRequestBodyJsonSerializerOptions = new() { WriteIndented = false };

    public ModHoundClient(ILogger<ModHoundClient> logger, IPlatformFunctions platformFunctions, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        requestLock = new();
    }

    readonly ILogger<ModHoundClient> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    int? processingCurrent;
    int? processingTotal;
    readonly AsyncLock requestLock;
    readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    public int? ProcessingCurrent
    {
        get => processingCurrent;
        private set
        {
            if (processingCurrent == value)
                return;
            processingCurrent = value;
            OnPropertyChanged();
        }
    }

    public int? ProcessingTotal
    {
        get => processingTotal;
        private set
        {
            if (processingTotal == value)
                return;
            processingTotal = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
            superSnacks.OfferRefreshments(new MarkupString("I'm already communicating with Mod Hound on your behalf. I'll let you know when I'm done."), Severity.Normal, options => options.Icon = MaterialDesignIcons.Normal.HandBackLeft);
            return;
        }
        try
        {
            await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var postDirectoryRequestBodyObject = new
            {
                files = (await pbDbContext.ModFiles // get me dem mod files MDC
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
                        mf.LastWrite
                    })
                    .ToListAsync()
                    .ConfigureAwait(false))
                    .Select(mf => new
                    {
                        name = mf.Path[(mf.Path.LastIndexOf('/') + 1)..mf.Path.LastIndexOf('.')],
                        date = mf.LastWrite!.Value.LocalDateTime.ToString("MM/dd/yyyy, hh:mm:ss tt"),
                        extension = mf.Path[(mf.Path.LastIndexOf('.') + 1)..],
                        fullPath = $"Mods/{mf.Path}"
                    })
                    .ToImmutableArray(),
                ignoreFolder = false,
                ignoreFolderName = "CC",
                timeZone = TZConvert.WindowsToIana(TimeZoneInfo.Local.StandardName)
            };
            var postDirectoryRequestBodyJson = JsonSerializer.Serialize(postDirectoryRequestBodyObject, visitorLoadDirectoryRequestBodyJsonSerializerOptions);
            byte[] postDirectoryRequestBodyJsonSha256;
            using (var postDirectoryRequestBodyJsonStream = new MemoryStream(Encoding.UTF8.GetBytes(postDirectoryRequestBodyJson)))
                postDirectoryRequestBodyJsonSha256 = await SHA256.HashDataAsync(postDirectoryRequestBodyJsonStream).ConfigureAwait(false);
            var freshnessDuration = TimeSpan.FromDays(1);
            var stalenessTime = DateTimeOffset.UtcNow.Subtract(freshnessDuration);
            if (await pbDbContext.ModHoundReports.Where(mhr => mhr.RequestSha256 == postDirectoryRequestBodyJsonSha256 && mhr.Retrieved >= stalenessTime).OrderByDescending(mhr => mhr.Retrieved).FirstOrDefaultAsync().ConfigureAwait(false) is { } freshIdenticalReport)
            {
                superSnacks.OfferRefreshments(new MarkupString($"Good news, I already asked Mod Hound about this exact configuration of your Mods folder in the past {freshnessDuration.Humanize()}. I can just show you that report right now!"), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.TableSync);
                return;
            }
            var cookieContainer = new CookieContainer();
            using var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
            };
            using var httpClient = new HttpClient(httpClientHandler) { BaseAddress = baseAddress };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(platformFunctions.UserAgent);
            httpClient.DefaultRequestHeaders.Add("Origin", schemeAndAuthority);
            using var checkModFilesPageRequest = await httpClient.GetAsync(visitorLoad).ConfigureAwait(false);
            try
            {
                checkModFilesPageRequest.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Mod Hound's response from GET {visitorLoad} returned non-successful status code: {{StatusCode}}", checkModFilesPageRequest.StatusCode);
                superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var checkModFilesPageResponse = await checkModFilesPageRequest.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var checkModFilesPageBrowsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
            var checkModFilesPageDocument = await checkModFilesPageBrowsingContext.OpenAsync(dr => dr.Content(checkModFilesPageResponse)).ConfigureAwait(false);
            if (checkModFilesPageDocument.QuerySelector("meta[name=\"csrf-token\"]") is not { } csrfTokenMetaTag
                || csrfTokenMetaTag.GetAttribute("content") is not { } csrfToken
                || string.IsNullOrWhiteSpace(csrfToken))
                return;
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
                superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var postDirectoryResponse = await postDirectoryRequest.Content.ReadFromJsonAsync<PostDirectoryResponse>().ConfigureAwait(false);
            if (postDirectoryResponse?.TaskId is not { } taskId)
            {
                logger.LogWarning($"Mod Hound's response from POST {visitorLoadDirectory} did not contain a task ID: {{Response}}", await postDirectoryRequest.Content.ReadAsStringAsync().ConfigureAwait(false));
                superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            GetTaskStatusResponse? lastTaskStatusResponse = null;
            var visitorTaskStatusUrl = $"/visitor/task-status/{taskId}";
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
                    superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                    return;
                }
                lastTaskStatusResponse = await taskStatusRequest.Content.ReadFromJsonAsync<GetTaskStatusResponse>().ConfigureAwait(false);
                if (lastTaskStatusResponse is null)
                {
                    logger.LogWarning("Mod Hound's response from getting the task status for {TaskId} could not be understood: {Response}", taskId, await taskStatusRequest.Content.ReadAsStringAsync().ConfigureAwait(false));
                    superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                    break;
                }
                if (lastTaskStatusResponse.State is not (TaskStatusState.PENDING or TaskStatusState.STARTED or TaskStatusState.PROCESSING))
                    break;
                ProcessingTotal = lastTaskStatusResponse.Total;
                ProcessingCurrent = lastTaskStatusResponse.Current;
            }
            ProcessingTotal = null;
            ProcessingCurrent = null;
            if (lastTaskStatusResponse is null)
                return;
            if (lastTaskStatusResponse.State is TaskStatusState.FAILURE)
            {
                logger.LogWarning("Mod Hound's response from getting the task status for {TaskId} returned failure state with error: {Error}", taskId, lastTaskStatusResponse.Error);
                superSnacks.OfferRefreshments(new MarkupString($"Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later.<br /><br />Specifically, Mod Hound said:<br /><pre>{WebUtility.HtmlEncode(lastTaskStatusResponse.Error)}</pre>"), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            if (lastTaskStatusResponse.ResultId is not { } resultId
                || string.IsNullOrWhiteSpace(resultId))
            {
                logger.LogWarning("Mod Hound's response from getting the task status for {TaskId} produced a blank result ID", taskId);
                superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var getReportRequest = await httpClient.GetAsync($"/visitor/track/{lastTaskStatusResponse.ResultId}/{lastTaskStatusResponse.Type}").ConfigureAwait(false);
            try
            {
                getReportRequest.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mod Hound's response from getting the report for result {ResultId} of type {Type} returned non-successful status code: {StatusCode}", lastTaskStatusResponse.ResultId, lastTaskStatusResponse.Type, getReportRequest.StatusCode);
                superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
                return;
            }
            var getReportResponse = await getReportRequest.Content.ReadAsStringAsync().ConfigureAwait(false);
            var modHoundReport = new ModHoundReport
            {
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
            var records = new List<ModHoundReportRecord>();
            for (var i = 0; i <= 4; ++i)
            {
                if (reportPageDocument.GetElementById(statusedRecordTableIds[i]) is not { } table)
                    continue;
                var status = (ModHoundReportRecordStatus)i;
                foreach (var htmlRecord in table.QuerySelectorAll("tbody > tr"))
                {
                    if (htmlRecord is null)
                        continue;
                    var record = new ModHoundReportRecord()
                    {
                        CreatorName = htmlRecord.QuerySelector("td:nth-child(3)")?.TextContent.Trim() ?? string.Empty,
                        FilePath = htmlRecord.QuerySelector("td:nth-child(1) > div[title]")?.GetAttribute("title") ?? string.Empty,
                        ModName = htmlRecord.QuerySelector("td:nth-child(2)")?.TextContent.Trim() ?? string.Empty,
                        Status = status
                    };
                    if (htmlRecord.QuerySelector("td:nth-child(5) > div[data-utc-time-date-only]")?.GetAttribute("data-utc-time-date-only") is { } dateOfInstalledFileStr
                        && !string.IsNullOrWhiteSpace(dateOfInstalledFileStr)
                        && DateTimeOffset.TryParse(dateOfInstalledFileStr, out var dateOfInstalledFile))
                        record.DateOfInstalledFile = dateOfInstalledFile;
                    else if (htmlRecord.QuerySelector("td:nth-child(5)")?.TextContent is { } lessPrecisedateOfInstalledFileStr
                        && !string.IsNullOrWhiteSpace(lessPrecisedateOfInstalledFileStr)
                        && DateTimeOffset.TryParse(lessPrecisedateOfInstalledFileStr.Trim(), out var lessPrecisedateOfInstalledFile))
                        record.DateOfInstalledFile = lessPrecisedateOfInstalledFile;
                    if (htmlRecord.QuerySelector("td:nth-child(4) > div[data-utc-time-date-only]")?.GetAttribute("data-utc-time-date-only") is { } lastUpdateDateStr
                        && !string.IsNullOrWhiteSpace(lastUpdateDateStr)
                        && DateTimeOffset.TryParse(lastUpdateDateStr, out var lastUpdateDate))
                        record.LastUpdateDate = lastUpdateDate;
                    else if (htmlRecord.QuerySelector("td:nth-child(4)")?.TextContent is { } lessPreciseLastUpdateDateStr
                        && !string.IsNullOrWhiteSpace(lessPreciseLastUpdateDateStr)
                        && DateTimeOffset.TryParse(lessPreciseLastUpdateDateStr, out var lessPreciseLastUpdateDate))
                        record.LastUpdateDate = lessPreciseLastUpdateDate;
                    if (htmlRecord.QuerySelector("td:nth-child(7)") is { } modLinkOrIndexCell)
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
                    if (htmlRecord.QuerySelector("td:nth-child(8)")?.TextContent is { } updateNotes
                        && !string.IsNullOrWhiteSpace(updateNotes))
                        record.UpdateNotes = updateNotes.Trim();
                    records.Add(record);
                }
            }
            modHoundReport.Records = records;
            var incompatibilityRecords = new List<ModHoundReportIncompatibilityRecord>();
            if (reportPageDocument.GetElementById("incompsDetailsPane") is { } incompatibilityDetailsPane)
                foreach (var orderedList in incompatibilityDetailsPane.QuerySelectorAll("ol"))
                {
                    if (orderedList is null)
                        continue;
                    var record = new ModHoundReportIncompatibilityRecord { Parts = [] };
                    foreach (var listItem in orderedList.QuerySelectorAll("li"))
                        if (listItem.TextContent is { } textContent
                            && !string.IsNullOrWhiteSpace(textContent))
                            record.Parts.Add(new() { Label = textContent.Trim() });
                    if (record.Parts.Count is > 0)
                        incompatibilityRecords.Add(record);
                }
            modHoundReport.IncompatibilityRecords = incompatibilityRecords;
            var missingRequirementsRecords = new List<ModHoundReportMissingRequirementsRecord>();
            if (reportPageDocument.GetElementById("missingreqsDetailsPane") is { } missingRequirementsDetailsPane)
                foreach (var htmlRecord in missingRequirementsDetailsPane.QuerySelectorAll("div"))
                {
                    if (htmlRecord is null)
                        continue;
                    var unorderedLists = htmlRecord.QuerySelectorAll("ul").ToImmutableArray();
                    if (unorderedLists.Length is not 2)
                        continue;
                    var record = new ModHoundReportMissingRequirementsRecord
                    {
                        Dependencies = [],
                        Dependents = []
                    };
                    foreach (var listItem in unorderedLists[0].QuerySelectorAll("li"))
                        if (listItem.TextContent is { } textContent
                            && !string.IsNullOrWhiteSpace(textContent))
                            record.Dependents.Add(new() { Label = textContent.Trim() });
                    foreach (var listItem in unorderedLists[1].QuerySelectorAll("li"))
                    {
                        if (listItem.QuerySelector("b") is not { } boldTag
                            || boldTag.TextContent is not { } label
                            || string.IsNullOrWhiteSpace(label))
                            continue;
                        var dependency = new ModHoundReportMissingRequirementsRecordDependency { Label = label };
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
                    if (record.Dependencies.Count is > 0
                        && record.Dependents.Count is > 0)
                        missingRequirementsRecords.Add(record);
                }
            modHoundReport.MissingRequirementsRecords = missingRequirementsRecords;
            var notTrackedRecords = new List<ModHoundReportNotTrackedRecord>();
            if (reportPageDocument.GetElementById("nottrackedsTable") is { } notTrackedTable)
                foreach (var htmlRecord in notTrackedTable.QuerySelectorAll("tbody > tr"))
                {
                    if (htmlRecord is null)
                        continue;
                    var record = new ModHoundReportNotTrackedRecord()
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
                        record.FileDate = fileDate;
                    notTrackedRecords.Add(record);
                }
            modHoundReport.NotTrackedRecords = notTrackedRecords;
            await pbDbContext.ModHoundReports.AddAsync(modHoundReport).ConfigureAwait(false);
            await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            superSnacks.OfferRefreshments(new MarkupString($"Good news, your Mod Hound report is finished and ready to view!"), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.TableCheck);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"unhandled exception while attempting to request, parse, and commit Mod Hound report");
            superSnacks.OfferRefreshments(new MarkupString("Something went wrong in my communication with Mod Hound. I've made a note about it in my log. Perhaps you should try again later."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.Alert);
            return;
        }
        finally
        {
            requestLockHeld.Dispose();
        }
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
