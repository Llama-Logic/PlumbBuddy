using UIKit;
using UserNotifications;

namespace PlumbBuddy.Platforms.MacCatalyst;

partial class PlatformFunctions :
    IPlatformFunctions
{
    static readonly TimeSpan gameProcessScanGracePeriod = TimeSpan.FromSeconds(5);

    #region Domestic Patterns

    [GeneratedRegex(@"^\.DS_Store$", RegexOptions.Singleline)]
    private static partial Regex GetDotDsUnderscoreStorePattern();

    [GeneratedRegex(@"^\.fseventsd$", RegexOptions.Singleline)]
    private static partial Regex GetDotFsEventsDPattern();

    [GeneratedRegex(@"^\.localized$", RegexOptions.Singleline)]
    private static partial Regex GetDotLocalizedPattern();

    [GeneratedRegex(@"^\.TemporaryItems$", RegexOptions.Singleline)]
    private static partial Regex GetDotTemporaryItemsPattern();

    #endregion

    #region Foreign Patterns

    [GeneratedRegex(@"^desktop\.ini$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetDesktopDotIniPattern();

    [GeneratedRegex(@"^iconcache.*\.db$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetIconCacheDotDbPattern();

    [GeneratedRegex(@"^thumbs\.db$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetThumbsDotDbPattern();

    #endregion

    static readonly PropertyChangedEventArgs progressMaximumChangedEventArgs = new(nameof(ProgressMaximum));
    static readonly PropertyChangedEventArgs progressStateChangedEventArgs = new(nameof(ProgressState));
    static readonly PropertyChangedEventArgs progressValueChangedEventArgs = new(nameof(ProgressValue));

    const string etcLocalTimeSymlinkPath = "/etc/localtime";
    const string etcLocalTimePrefix = "/var/db/timezone/zoneinfo/";
    const string readLinkBinaryPath = "readlink";

    public PlatformFunctions(ILogger<PlatformFunctions> logger, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        this.logger = logger;
        this.settings = settings;
        userNotificationCenter = UNUserNotificationCenter.Current;
        Task.Run(InitializeNotificationsAsync);
    }

    DateTimeOffset? lastGameProcessScan;
    Process? lastGameProcess;
    readonly ILogger<PlatformFunctions> logger;
    int progressMaximum;
    AppProgressState progressState;
    int progressValue;
    readonly ISettings settings;
    bool userNotificationsAllowed;
    readonly UNUserNotificationCenter userNotificationCenter;

    public nint DefaultProcessorAffinity { get; } = default;

    public IReadOnlyList<Regex> DiscardableDirectoryNamePatterns { get; } =
    [
        GetDotTemporaryItemsPattern(),
        GetDotFsEventsDPattern()
    ];

    public IReadOnlyList<Regex> DiscardableFileNamePatterns { get; } =
    [
        GetDotDsUnderscoreStorePattern(),
        GetDotLocalizedPattern()
    ];

    public string FileSystemSQliteCollation { get; } = "BINARY";

    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public IReadOnlyList<Regex> ForeignDirectoryNamePatterns { get; } =
    [
    ];

    public IReadOnlyList<Regex> ForeignFileNamePatterns { get; } =
    [
        GetDesktopDotIniPattern(),
        GetIconCacheDotDbPattern(),
        GetThumbsDotDbPattern()
    ];

    public string HostOperatingSystemUserAgentName =>
        "macOS";

    public bool IsGameProcessOptimizationSupported =>
        false;

    public nint PerformanceProcessorAffinity =>
        default;

    public bool ProcessorsHavePerformanceVarianceAndConfigurableAffinity =>
        false;

    public int ProgressMaximum
    {
        get => progressMaximum;
        set
        {
            var inDomainValue = Math.Max(0, value);
            if (progressMaximum == inDomainValue)
                return;
            ProgressValue = Math.Min(progressValue, inDomainValue);
            progressMaximum = inDomainValue;
            OnPropertyChanged(progressMaximumChangedEventArgs);
        }
    }

    public AppProgressState ProgressState
    {
        get => progressState;
        set
        {
            if (progressState == value)
                return;
            if (value.HasFlag(AppProgressState.Indeterminate))
            {
                ProgressValue = 0;
                ProgressMaximum = 1;
            }
            progressState = value;
            OnPropertyChanged(progressStateChangedEventArgs);
        }
    }

    public int ProgressValue
    {
        get => progressValue;
        set
        {
            var inDomainValue = Math.Max(0, value);
            if (progressValue == inDomainValue)
                return;
            ProgressMaximum = Math.Max(progressMaximum, inDomainValue);
            progressValue = inDomainValue;
            OnPropertyChanged(progressValueChangedEventArgs);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task<bool> ForegroundGameAsync(DirectoryInfo installationDirectory)
    {
        if (await GetGameProcessAsync(installationDirectory).ConfigureAwait(false) is not { } gameProcess
            || gameProcess.HasExited)
            return false;
        using var osascriptProcess = Process.Start(new ProcessStartInfo("/usr/bin/osascript", $"-e 'tell application id \"com.ea.mac.thesims4\" to activate'")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (osascriptProcess is not null)
        {
            await osascriptProcess.WaitForExitAsync().ConfigureAwait(false);
            return osascriptProcess.ExitCode is 0;
        }
        return false;
    }

    public async Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        if (lastGameProcessScan is { } last
            && last + gameProcessScanGracePeriod > DateTimeOffset.UtcNow)
            return lastGameProcess;
        lastGameProcessScan = DateTimeOffset.UtcNow;
        lastGameProcess = null;
        using var zshProcess = Process.Start(new ProcessStartInfo("/bin/zsh", $"-c \"pgrep -f 'The Sims 4'\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (zshProcess is not null)
        {
            using var zshOutputReader = zshProcess.StandardOutput;
            var zshOutput = await zshOutputReader.ReadToEndAsync().ConfigureAwait(false);
            var maybeNullProcessId = zshOutput
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => int.TryParse(line, out var processId) ? processId : (int?)null)
                .Where(maybeNullProcessId => maybeNullProcessId is not null)
                .FirstOrDefault();
            if (maybeNullProcessId is { } processId)
            {
                lastGameProcess = Process.GetProcessById(processId);
                return lastGameProcess;
            }
        }
        return null;
    }

    public async Task<string> GetTimezoneIanaNameAsync()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = readLinkBinaryPath,
                    Arguments = etcLocalTimeSymlinkPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var target = (await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false)).Trim();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return !string.IsNullOrEmpty(target) && target.StartsWith(etcLocalTimePrefix)
                ? target[etcLocalTimePrefix.Length..]
                : TimeZoneInfo.Local.Id;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to execute and/or read the standard output from: {Binary} {Arguments}", readLinkBinaryPath, etcLocalTimeSymlinkPath);
            return TimeZoneInfo.Local.Id;
        }
    }

    public async Task<Version?> GetTS4InstallationVersionAsync()
    {
        var appBundle = new DirectoryInfo(settings.InstallationFolderPath);
        if (!appBundle.Exists)
            return null;
        var defaultIni = new FileInfo(Path.Combine(appBundle.FullName, "Contents", "Resources", "Default.ini"));
        if (!defaultIni.Exists)
            return null;
        var parser = new IniDataParser();
        var data = parser.Parse(await File.ReadAllTextAsync(defaultIni.FullName).ConfigureAwait(false));
        var versionData = data["Version"];
        if (versionData["gameversion"] is { } gameVersion
            && !string.IsNullOrWhiteSpace(gameVersion)
            && Version.TryParse(gameVersion, out var version))
            return version;
        return null;
    }

    async Task InitializeNotificationsAsync()
    {
        (userNotificationsAllowed, _) = await userNotificationCenter.RequestAuthorizationAsync(UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound).ConfigureAwait(false);
        if (userNotificationsAllowed)
            userNotificationCenter.Delegate = new NotificationCenterDelegate();
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public async Task<bool> SendLocalNotificationAsync(string caption, string text)
    {
        if (!userNotificationsAllowed)
            return false;
        using var content = new UNMutableNotificationContent
        {
            Title = caption,
            Body = text,
            Sound = UNNotificationSound.Default
        };
        using var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), content, trigger)).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetBadgeNumberAsync(int number)
    {
        if (!userNotificationsAllowed)
            return false;
        using var content = new UNMutableNotificationContent
        {
            Badge = number,
            Sound = UNNotificationSound.Default
        };
        using var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), content, trigger)).ConfigureAwait(false);
        return true;
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("open", $"\"{directoryInfo.FullName}\"");

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("open", $"-R \"{fileInfo.FullName}\"");

    class NotificationCenterDelegate :
        UNUserNotificationCenterDelegate
    {
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            UIApplication
                .SharedApplication
                .ConnectedScenes
                .OfType<UIWindowScene>()
                .FirstOrDefault()
                ?.Windows
                .FirstOrDefault()
                ?.MakeKeyAndVisible();
            completionHandler();
        }

        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
        }
    }
}
