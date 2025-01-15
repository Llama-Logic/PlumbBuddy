using Microsoft.WindowsAPICodePack.Taskbar;
using Windows.UI.Notifications;

namespace PlumbBuddy.Platforms.Windows;

partial class PlatformFunctions :
    IPlatformFunctions
{
    #region Domestic Patterns

    [GeneratedRegex(@"^desktop\.ini$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetDesktopDotIniPattern();

    [GeneratedRegex(@"^iconcache.*\.db$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetIconCacheDotDbPattern();

    [GeneratedRegex(@"^thumbs\.db$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetThumbsDotDbPattern();

    #endregion

    #region Foreign Patterns

    [GeneratedRegex(@"^\.DS_Store$", RegexOptions.Singleline)]
    private static partial Regex GetDotDsUnderscoreStorePattern();

    [GeneratedRegex(@"^\.fseventsd$", RegexOptions.Singleline)]
    private static partial Regex GetDotFsEventsDPattern();

    [GeneratedRegex(@"^\.localized$", RegexOptions.Singleline)]
    private static partial Regex GetDotLocalizedPattern();

    [GeneratedRegex(@"^\.TemporaryItems$", RegexOptions.Singleline)]
    private static partial Regex GetDotTemporaryItemsPattern();

    #endregion

    static readonly PropertyChangedEventArgs progressMaximumChangedEventArgs = new(nameof(ProgressMaximum));
    static readonly PropertyChangedEventArgs progressStateChangedEventArgs = new(nameof(ProgressState));
    static readonly PropertyChangedEventArgs progressValueChangedEventArgs = new(nameof(ProgressValue));

    public PlatformFunctions(ILifetimeScope lifetimeScope, ILogger<PlatformFunctions> logger, IAppLifecycleManager appLifecycleManager)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        this.lifetimeScope = lifetimeScope;
        this.logger = logger;
        this.appLifecycleManager = appLifecycleManager;
        badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
        toastNotifier = ToastNotificationManager.CreateToastNotifier();
    }

    readonly IAppLifecycleManager appLifecycleManager;
    readonly BadgeUpdater badgeUpdater;
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<PlatformFunctions> logger;
    int progressMaximum;
    AppProgressState progressState;
    int progressValue;
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

    public string FileSystemSQliteCollation { get; } = "NOCASE";

    public StringComparison FileSystemStringComparison =>
        StringComparison.OrdinalIgnoreCase;

    public IReadOnlyList<Regex> ForeignDirectoryNamePatterns { get; } =
    [
        GetDotTemporaryItemsPattern(),
        GetDotFsEventsDPattern()
    ];

    public IReadOnlyList<Regex> ForeignFileNamePatterns { get; } =
    [
        GetDotDsUnderscoreStorePattern(),
        GetDotLocalizedPattern()
    ];

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
            if (appLifecycleManager.IsVisible)
                TaskbarManager.Instance.SetProgressValue(progressValue, progressMaximum);
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
            if (appLifecycleManager.IsVisible)
                TaskbarManager.Instance.SetProgressState((TaskbarProgressBarState)progressState);
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
            if (appLifecycleManager.IsVisible)
                TaskbarManager.Instance.SetProgressValue(progressValue, progressMaximum);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public Task<bool> SendLocalNotificationAsync(string caption, string text)
    {
        try
        {
            var toastXml = new global::Windows.Data.Xml.Dom.XmlDocument();
            toastXml.LoadXml
            (
                $"""
            <toast duration="long">
                <visual>
                    <binding template="ToastGeneric">
                        <text>{SecurityElement.Escape(caption)}</text>
                        <text>{SecurityElement.Escape(text)}</text>
                    </binding>
                </visual>
            </toast>
            """
            );
            var toast = new ToastNotification(toastXml);
            toast.Activated += HandleToastActivated;
            toastNotifier.Show(toast);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to send a local notification with caption \"{Caption}\" and text \"{Text}\"", caption, text);
            return Task.FromResult(false);
        }
    }

    public Task<bool> SetBadgeNumberAsync(int number)
    {
        try
        {
            if (number <= 0)
            {
                badgeUpdater.Clear();
                return Task.FromResult(true);
            }
            var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            var badgeElement = (global::Windows.Data.Xml.Dom.XmlElement)badgeXml.SelectSingleNode("/badge");
            badgeElement.SetAttribute("value", number.ToString());
            badgeUpdater.Update(new BadgeNotification(badgeXml));
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to set Task Bar badge number to {BadgeNumber}", number);
            return Task.FromResult(false);
        }
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("explorer.exe", directoryInfo.FullName);

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
}
