using Octokit;

namespace PlumbBuddy.Services;

public sealed class UpdateManager :
    IUpdateManager
{
    public UpdateManager(ILogger<UpdateManager> logger, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks, IBlazorFramework blazorFramework)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.superSnacks = superSnacks;
        this.blazorFramework = blazorFramework;
        var mauiVersion = AppInfo.Version;
        CurrentVersion = new Version(mauiVersion.Major, mauiVersion.Minor, mauiVersion.Build);
        if (CurrentVersion != settings.VersionAtLastStartup)
        {
            if (settings.VersionAtLastStartup is { } lastVersion)
            {
                logger.LogInformation("This is the first time starting after the upgrade from {LastVersion}.", lastVersion);
                if (lastVersion is { Major: < 1 } or { Major: 1, Minor: 0, Build: <= 8 } && settings.Type is UserType.Creator)
                {
                    settings.ScanForCorruptMods = false;
                    settings.ScanForCorruptScriptMods = false;
                    superSnacks.OfferRefreshments(new MarkupString("Two new scans to detect corrupt mod files have been added, but since you've self-identified as a Mod Creator, I have turned them off by default."), Severity.Info, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.NewBox;
                        options.Action = "Turn Them On";
                        options.ActionColor = MudBlazor.Color.Success;
                        options.Onclick = _ =>
                        {
                            settings.ScanForCorruptMods = true;
                            settings.ScanForCorruptScriptMods = true;
                            return Task.CompletedTask;
                        };
                        options.RequireInteraction = true;
                    });
                }
                if (lastVersion is { Major: < 1 } or { Major: 1, Minor: < 1 } && settings.Type is UserType.Creator)
                {
                    settings.OfferPatchDayModUpdatesHelp = false;
                    superSnacks.OfferRefreshments(new MarkupString("My offer to help you with mod patch day updates is now optional, but since you've a self-identified as a Mod Creator, I have turned it off by default."), Severity.Info, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.NewBox;
                        options.Action = "Turn It On";
                        options.ActionColor = MudBlazor.Color.Success;
                        options.Onclick = _ =>
                        {
                            settings.OfferPatchDayModUpdatesHelp = true;
                            return Task.CompletedTask;
                        };
                        options.RequireInteraction = true;
                    });
                }
            }
            else
                logger.LogInformation("This is the first time this installation is starting.");
        }
        settings.VersionAtLastStartup = CurrentVersion;
        _ = Task.Run(PeriodicCheckForUpdateAsync);
    }

    readonly IBlazorFramework blazorFramework;
    readonly ILogger<UpdateManager> logger;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    public Version CurrentVersion { get; }

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
            settings.LastCheckForUpdate = DateTimeOffset.Now;
            if (latestMostStableRelease is null
                || latestMostStableRelease.TagName[(latestMostStableRelease.TagName.IndexOf('/', StringComparison.Ordinal) + 1)..] is not string versionStr
                || !Version.TryParse(versionStr, out var version)
                || Comparer<Version>.Default.Compare(version, CurrentVersion) <= 0)
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
            settings.LastCheckForUpdate = DateTimeOffset.Now;
            logger.LogWarning(ex, "checking for an update failed");
            return (null, null, null);
        }
    }

    async Task PeriodicCheckForUpdateAsync()
    {
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (true)
        {
            try
            {
                if (!settings.AutomaticallyCheckForUpdates || settings.LastCheckForUpdate is { } lastCheckForUpdate && lastCheckForUpdate + TimeSpan.FromDays(1) > DateTimeOffset.Now)
                    continue;
                var (version, releaseNotes, downloadUrl) = await CheckForUpdateAsync().ConfigureAwait(false);
                if (version is null)
                    continue;
                if (settings.SkipUpdateVersion is { } skipUpdateVersion)
                {
                    if (Comparer<Version>.Default.Compare(version, skipUpdateVersion) <= 0)
                        continue;
                    settings.SkipUpdateVersion = null;
                }
                await platformFunctions.SendLocalNotificationAsync(AppText.UpdateManager_Notification_Available_Caption, AppText.UpdateManager_Notification_Available_Text).ConfigureAwait(false);
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.UpdateManager_Info_Available, version)), Severity.Info, options =>
                {
                    options.Icon = MaterialDesignIcons.Normal.Update;
                    options.Onclick = async _ => await PresentUpdateAsync(version, releaseNotes, downloadUrl);
                    options.RequireInteraction = true;
                    options.ShowCloseIcon = true;
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "encountered unhandled exception while periodically checking for updates");
            }
            finally
            {
                await periodicTimer.WaitForNextTickAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task PresentUpdateAsync(Version version, string? releaseNotes, Uri? downloadUrl)
    {
        var dialogService = blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>();
        if (releaseNotes is null)
        {
            if (downloadUrl is null)
            {
                if (await dialogService.ShowQuestionDialogAsync(string.Format(AppText.UpdateManager_PresentUpdate_Caption, version), AppText.UpdateManager_PresentUpdate_Text_NoReleaseNotes) is { } saidYes && saidYes)
                {
                    await Browser.OpenAsync("https://plumbbuddy.app/download", BrowserLaunchMode.External);
                    return;
                }
            }
            else
            {
                if (await dialogService.ShowQuestionDialogAsync(string.Format(AppText.UpdateManager_PresentUpdate_Caption, version), AppText.UpdateManager_PresentUpdate_Text_NoReleaseNotes) is { } saidYes && saidYes)
                {
                    await Browser.OpenAsync(downloadUrl.ToString(), BrowserLaunchMode.External);
                    return;
                }
            }
        }
        else
        {
            if (downloadUrl is null)
            {
                if (await dialogService.ShowQuestionDialogAsync(string.Format(AppText.UpdateManager_PresentUpdate_Caption, version), string.Format(AppText.UpdateManager_PresentUpdate_Text, releaseNotes), big: true) is { } saidYes && saidYes)
                {
                    await Browser.OpenAsync("https://plumbbuddy.app/download", BrowserLaunchMode.External);
                    return;
                }
            }
            else
            {
                if (await dialogService.ShowQuestionDialogAsync(string.Format(AppText.UpdateManager_PresentUpdate_Caption, version), string.Format(AppText.UpdateManager_PresentUpdate_Text, releaseNotes), big: true) is { } saidYes && saidYes)
                {
                    await Browser.OpenAsync(downloadUrl.ToString(), BrowserLaunchMode.External);
                    return;
                }
            }
        }
        if (await dialogService.ShowQuestionDialogAsync(string.Format(AppText.UpdateManager_PresentUpdate_OfferToSkipVersion_Caption, version), AppText.UpdateManager_PresentUpdate_OfferToSkipVersion_Text) ?? false)
        {
            settings.SkipUpdateVersion = version;
            superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.UpdateManager_PresentUpdate_OfferToSkipVersion_Success, version)), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.DownloadOff);
        }
    }
}
