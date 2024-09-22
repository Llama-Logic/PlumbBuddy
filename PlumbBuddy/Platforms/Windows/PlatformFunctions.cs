using Windows.UI.Notifications;

namespace PlumbBuddy.Platforms.Windows;

partial class PlatformFunctions :
    IPlatformFunctions
{
    [GeneratedRegex(@"^desktop\.ini$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetDesktopDotIniPattern();

    [GeneratedRegex(@"^iconcache.*\.db$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetIconCacheDotDbPattern();

    [GeneratedRegex(@"^thumbs\.db$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetThumbsDotDbPattern();

    public PlatformFunctions(ILifetimeScope lifetimeScope)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        this.lifetimeScope = lifetimeScope;
        badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
        toastNotifier = ToastNotificationManager.CreateToastNotifier();
    }

    readonly BadgeUpdater badgeUpdater;
    readonly ILifetimeScope lifetimeScope;
    readonly ToastNotifier toastNotifier;

    public IReadOnlyList<Regex> DiscardableDirectoryNamePatterns { get; } =
    [
    ];

    public IReadOnlyList<Regex> DiscardableFileNamePatterns { get; } =
    [
        GetDesktopDotIniPattern(),
        GetIconCacheDotDbPattern(),
        GetThumbsDotDbPattern()
    ];

    public StringComparison FileSystemStringComparison =>
        StringComparison.OrdinalIgnoreCase;

    public Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        var gameProcess = Process.GetProcessesByName("TS4_x64").FirstOrDefault()
            ?? Process.GetProcessesByName("TS4_DX9_x64").FirstOrDefault();
        if (gameProcess is null
            || gameProcess.MainModule is not { } mainModule
            || !mainModule.FileName.StartsWith(installationDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<Process?>(null);
        return Task.FromResult<Process?>(gameProcess);
    }

    void HandleToastActivated(ToastNotification toastNotification, object args) =>
        lifetimeScope.Resolve<IAppLifecycleManager>().ShowWindow();

    public Task SendLocalNotificationAsync(string caption, string text)
    {
        var toastXml = new global::Windows.Data.Xml.Dom.XmlDocument();
        toastXml.LoadXml
        (
            $"""
            <toast duration="long">
                <visual>
                    <binding template="ToastGeneric">
                        <text>{caption}</text>
                        <text>{text}</text>
                    </binding>
                </visual>
            </toast>
            """
        );
        var toast = new ToastNotification(toastXml);
        toast.Activated += HandleToastActivated;
        toastNotifier.Show(toast);
        return Task.CompletedTask;
    }

    public Task SetBadgeNumberAsync(int number)
    {
        if (number <= 0)
        {
            badgeUpdater.Clear();
            return Task.CompletedTask;
        }
        var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
        var badgeElement = (global::Windows.Data.Xml.Dom.XmlElement)badgeXml.SelectSingleNode("/badge");
        badgeElement.SetAttribute("value", number.ToString());
        badgeUpdater.Update(new BadgeNotification(badgeXml));
        return Task.CompletedTask;
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("explorer.exe", directoryInfo.FullName);

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
}
