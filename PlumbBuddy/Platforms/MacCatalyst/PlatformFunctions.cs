using UIKit;
using UserNotifications;

namespace PlumbBuddy.Platforms.MacCatalyst;

partial class PlatformFunctions :
    IPlatformFunctions
{
    static readonly TimeSpan gameProcessScanGracePeriod = TimeSpan.FromSeconds(5);

    [GeneratedRegex(@"^\.DS_Store$", RegexOptions.Singleline)]
    private static partial Regex GetDotDsUnderscoreStorePattern();

    [GeneratedRegex(@"^\.fseventsd$", RegexOptions.Singleline)]
    private static partial Regex GetDotFsEventsDPattern();

    [GeneratedRegex(@"^\.localized$", RegexOptions.Singleline)]
    private static partial Regex GetDotLocalizedPattern();

    [GeneratedRegex(@"^\.TemporaryItems$", RegexOptions.Singleline)]
    private static partial Regex GetDotTemporaryItemsPattern();

    public PlatformFunctions()
    {
        userNotificationCenter = UNUserNotificationCenter.Current;
        Task.Run(InitializeNotificationsAsync);
    }

    DateTimeOffset? lastGameProcessScan;
    bool userNotificationsAllowed;
    readonly UNUserNotificationCenter userNotificationCenter;

    /*
     * This looks alarming but it is absolutely necessary due to how the .NET CLR has to negotiate with the macOS Task Scheduler.
     * You can either believe me and leave it alone, or mess with it, break shit, and get Meg on your case.
     * It is greatly mitigated by two factors:
     * (1) macOS does what it damn well pleases and will throttle and sleep PB's threads to its heart's content regardless; and,
     * (2) unlike in Windows, PB does NOT run in the background at startup on Macs.
     */
    public int DataflowBoundedCapacity =>
        DataflowBlockOptions.Unbounded;

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

    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public async Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        if (lastGameProcessScan is { } last
            && last + gameProcessScanGracePeriod > DateTimeOffset.UtcNow)
            return null;
        lastGameProcessScan = DateTimeOffset.UtcNow;
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
                return Process.GetProcessById(processId);
        }
        return null;
    }

    async Task InitializeNotificationsAsync()
    {
        (userNotificationsAllowed, _) = await userNotificationCenter.RequestAuthorizationAsync(UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound).ConfigureAwait(false);
        if (userNotificationsAllowed)
            userNotificationCenter.Delegate = new NotificationCenterDelegate();
    }

    public async Task SendLocalNotificationAsync(string caption, string text)
    {
        if (!userNotificationsAllowed)
            return;
        using var content = new UNMutableNotificationContent
        {
            Title = caption,
            Body = text,
            Sound = UNNotificationSound.Default
        };
        using var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), content, trigger)).ConfigureAwait(false);
    }

    public async Task SetBadgeNumberAsync(int number)
    {
        if (!userNotificationsAllowed)
            return;
        using var content = new UNMutableNotificationContent
        {
            Badge = number,
            Sound = UNNotificationSound.Default
        };
        using var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), content, trigger)).ConfigureAwait(false);
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
