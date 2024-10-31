using Octokit;

namespace PlumbBuddy.Services;

public sealed class UpdateManager :
    IUpdateManager
{
    public UpdateManager(ILogger<UpdateManager> logger, IPlatformFunctions platformFunctions, IPlayer player, ISuperSnacks superSnacks, IBlazorFramework blazorFramework)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.superSnacks = superSnacks;
        this.blazorFramework = blazorFramework;
        automaticLock = new();
        this.player.PropertyChanged += HandlePlayerPropertyChanged;
        var mauiVersion = AppInfo.Version;
        CurrentVersion = new Version(mauiVersion.Major, mauiVersion.Minor, mauiVersion.Build);
        player.VersionAtLastStartup = CurrentVersion;
        ScheduleAutomaticUpdateCheck();
    }

    ~UpdateManager() =>
        Dispose(false);

    readonly AsyncLock automaticLock;
    readonly IBlazorFramework blazorFramework;
    readonly ILogger<UpdateManager> logger;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
    readonly ISuperSnacks superSnacks;

    public Version CurrentVersion { get; }

    async Task AutomaticUpdateCheckAsync()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var heldAutomaticLock = await automaticLock.LockAsync(cts.Token).ConfigureAwait(false);
        if (heldAutomaticLock is null)
            return;
        try
        {
            if (player.LastCheckForUpdate is { } lastCheckForUpdate)
            {
                var delay = lastCheckForUpdate + TimeSpan.FromDays(1) - DateTimeOffset.Now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay).ConfigureAwait(false);
                if (player.LastCheckForUpdate is { } lastCheckForUpdateRevisited && lastCheckForUpdateRevisited != lastCheckForUpdate || !player.AutomaticallyCheckForUpdates)
                    return;
            }
            var (version, releaseNotes, downloadUrl) = await CheckForUpdateAsync().ConfigureAwait(false);
            if (version is null)
                return;
            await platformFunctions.SendLocalNotificationAsync("An Update is Available", "Oh my, there's a better version of me than me now! Click here to see more.").ConfigureAwait(false);
            superSnacks.OfferRefreshments(new MarkupString($"PlumbBuddy {version} is now available to download."), Severity.Info, options =>
            {
                options.Icon = MaterialDesignIcons.Normal.Update;
                options.Onclick = async _ => await PresentUpdateAsync(version, releaseNotes, downloadUrl);
                options.RequireInteraction = true;
                options.ShowCloseIcon = true;
            });
        }
        finally
        {
            heldAutomaticLock.Dispose();
            ScheduleAutomaticUpdateCheck();
        }
    }

    public async Task<(Version? version, string? releaseNotes, Uri? downloadUrl)> CheckForUpdateAsync()
    {
        try
        {
            var releases = await new GitHubClient(new ProductHeaderValue("PlumbBuddy.app")).Repository.Release.GetAll("Llama-Logic", "PlumbBuddy");
            var latestMostStableRelease = releases
                .OrderBy(release => release.TagName switch
                {
                    string alphaReleaseTagName when alphaReleaseTagName.StartsWith("release/") => 0,
                    string alphaReleaseTagName when alphaReleaseTagName.StartsWith("release-preview/") => 1,
                    string alphaReleaseTagName when alphaReleaseTagName.StartsWith("release-beta/") => 2,
                    string alphaReleaseTagName when alphaReleaseTagName.StartsWith("release-alpha/") => 3,
                    _ => int.MaxValue
                })
                .ThenByDescending(release => release.PublishedAt ?? release.CreatedAt)
                .FirstOrDefault();
            player.LastCheckForUpdate = DateTimeOffset.Now;
            if (latestMostStableRelease is null
                || latestMostStableRelease.TagName[(latestMostStableRelease.TagName.IndexOf('/', StringComparison.Ordinal) + 1)..] is not string versionStr
                || !Version.TryParse(versionStr, out var version)
                || version <= CurrentVersion)
                return (null, null, null);
            return
            (
                version,
                latestMostStableRelease.Body,
                latestMostStableRelease.Assets?.FirstOrDefault(a => a.Name.EndsWith
                (
#if MACCATALYST
                    ".zip",
#else
                    ".msix",
#endif
                    StringComparison.OrdinalIgnoreCase
                ))?.BrowserDownloadUrl is string downloadUrlStr
                && Uri.TryCreate(downloadUrlStr, UriKind.Absolute, out var downloadUrl)
                ? downloadUrl
                : null
            );
        }
        catch (Exception ex)
        {
            player.LastCheckForUpdate = DateTimeOffset.Now;
            logger.LogWarning(ex, "checking for an update failed");
            return (null, null, null);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
            player.PropertyChanged -= HandlePlayerPropertyChanged;
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.AutomaticallyCheckForUpdates))
            ScheduleAutomaticUpdateCheck();
    }

    public async Task PresentUpdateAsync(Version version, string? releaseNotes, Uri? downloadUrl)
    {
        var dialogService = blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>();
        if (releaseNotes is null)
        {
            if (downloadUrl is null)
            {
                if (await dialogService.ShowQuestionDialogAsync($"Would you like to download PlumbBuddy {version}?",
                    $"""
                        I was unable to get the release notes for this version.
                        """) is { } saidYes && saidYes)
                    await Browser.OpenAsync("https://plumbbuddy.app/download", BrowserLaunchMode.External);
            }
            else
            {
                if (await dialogService.ShowQuestionDialogAsync($"Would you like to download PlumbBuddy {version}?",
                    $"""
                        I was unable to get the release notes for this version.
                        """) is { } saidYes && saidYes)
                    await Browser.OpenAsync(downloadUrl.ToString(), BrowserLaunchMode.External);
            }
        }
        else
        {
            if (downloadUrl is null)
            {
                if (await dialogService.ShowQuestionDialogAsync($"Would you like to download PlumbBuddy {version}?",
                    $"""
                        Here are the release notes for this new version:

                        {releaseNotes}
                        """, big: true) is { } saidYes && saidYes)
                    await Browser.OpenAsync("https://plumbbuddy.app/download", BrowserLaunchMode.External);
            }
            else
            {
                if (await dialogService.ShowQuestionDialogAsync($"Would you like to download PlumbBuddy {version}?",
                    $"""
                        Here are the release notes for this new version:

                        {releaseNotes}
                        """, big: true) is { } saidYes && saidYes)
                    await Browser.OpenAsync(downloadUrl.ToString(), BrowserLaunchMode.External);
            }
        }
    }

    void ScheduleAutomaticUpdateCheck()
    {
        if (player.AutomaticallyCheckForUpdates)
            _ = Task.Run(AutomaticUpdateCheckAsync);
    }
}
