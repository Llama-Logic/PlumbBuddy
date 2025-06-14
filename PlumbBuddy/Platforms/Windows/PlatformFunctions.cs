using Microsoft.WindowsAPICodePack.Taskbar;
using Windows.UI.Notifications;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.UI.WindowsAndMessaging;

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

    static unsafe List<SYSTEM_CPU_SET_INFORMATION._Anonymous_e__Union._CpuSet_e__Struct> GetLogicalProcessors()
    {
        var logicalProcessors = new List<SYSTEM_CPU_SET_INFORMATION._Anonymous_e__Union._CpuSet_e__Struct>();
        uint bufferLength = 0;
        var hProcNull = new HANDLE(IntPtr.Zero);
        PInvoke.GetSystemCpuSetInformation((SYSTEM_CPU_SET_INFORMATION*)IntPtr.Zero, 0, &bufferLength, hProcNull, 0);
        var buffer = Marshal.AllocHGlobal((int)bufferLength);
        try
        {
            if (PInvoke.GetSystemCpuSetInformation((SYSTEM_CPU_SET_INFORMATION*)buffer, bufferLength, &bufferLength, hProcNull, 0))
            {
                nint offset = 0;
                while (offset < bufferLength)
                {
                    logicalProcessors.Add(Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(buffer + offset).Anonymous.CpuSet);
                    offset += Marshal.SizeOf<SYSTEM_CPU_SET_INFORMATION>();
                }
            }
            else
                throw new Win32Exception("she's being completely unreasnable, you speak to her, Jeez...");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        return logicalProcessors;
    }

    public PlatformFunctions(ILogger<PlatformFunctions> logger, IAppLifecycleManager appLifecycleManager, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(settings);
        this.logger = logger;
        this.appLifecycleManager = appLifecycleManager;
        this.settings = settings;
        badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
        toastNotifier = ToastNotificationManager.CreateToastNotifier();
    }

    readonly IAppLifecycleManager appLifecycleManager;
    readonly BadgeUpdater badgeUpdater;
    readonly ILogger<PlatformFunctions> logger;
    int progressMaximum;
    AppProgressState progressState;
    int progressValue;
    readonly ISettings settings;
    readonly ToastNotifier toastNotifier;

    public nint DefaultProcessorAffinity
    {
        get
        {
            nint result = 0;
            foreach (var logicalProcessor in GetLogicalProcessors().OrderBy(lp => lp.LogicalProcessorIndex))
                result |= (nint)(1UL << logicalProcessor.LogicalProcessorIndex);
            return result;
        }
    }

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

    public string HostOperatingSystemUserAgentName =>
        "Windows";

    public bool IsGameProcessOptimizationSupported =>
        true;

    /// <summary>
    /// Gets a platform-specific <see cref="IntPtr"/> that when set to a <see cref="Process.ProcessorAffinity"/> will cause it to avoid any E-cores...
    /// ⚡🔥⚡ UNLIMITED POWER! ⚡🔥⚡
    /// </summary>
    public nint PerformanceProcessorAffinity
    {
        get
        {
            nint result = 0;
            foreach (var logicalProcessor in GetLogicalProcessors().OrderBy(lp => lp.LogicalProcessorIndex))
                if (logicalProcessor.EfficiencyClass is not 0)
                    result |= (nint)(1UL << logicalProcessor.LogicalProcessorIndex);
            return result;
        }
    }

    public bool ProcessorsHavePerformanceVarianceAndConfigurableAffinity =>
        GetLogicalProcessors().GroupBy(lp => lp.EfficiencyClass).Count() is > 1;

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

    public async Task<bool> ForegroundGameAsync(DirectoryInfo installationDirectory)
    {
        if (await GetGameProcessAsync(installationDirectory).ConfigureAwait(false) is not { } gameProcess
            || gameProcess.HasExited)
            return false;
        var gameMainWindowHandle = gameProcess.MainWindowHandle;
        if (gameMainWindowHandle == IntPtr.Zero)
            return false;
        var gameHwnd = new HWND(gameMainWindowHandle);
        if (PInvoke.IsIconic(gameHwnd))
            PInvoke.ShowWindow(gameHwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        return PInvoke.SetForegroundWindow(gameHwnd);        
    }

    public Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        var gameProcess = Process.GetProcessesByName("TS4_x64").FirstOrDefault()
            ?? Process.GetProcessesByName("TS4_DX9_x64").FirstOrDefault();
        try
        {
            if (gameProcess is null
                || gameProcess.MainModule is not { } mainModule
                || !mainModule.FileName.StartsWith(installationDirectory.FullName, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<Process?>(null);
            return Task.FromResult<Process?>(gameProcess);
        }
        catch (Win32Exception)
        {
            return Task.FromResult<Process?>(null);
        }
    }

    public Task<string> GetTimezoneIanaNameAsync() =>
        Task.FromResult(TZConvert.WindowsToIana(TimeZoneInfo.Local.Id));

    public Task<Version?> GetTS4InstallationVersionAsync()
    {
        var executable = new FileInfo(Path.Combine(settings.InstallationFolderPath, "Game", "Bin", "TS4_x64.exe"));
        if (!executable.Exists)
            return Task.FromResult<Version?>(null);
        var versionInfo = FileVersionInfo.GetVersionInfo(executable.FullName);
        if (versionInfo.FileVersion is { } fileVersion
            && !string.IsNullOrWhiteSpace(fileVersion)
            && Version.TryParse(fileVersion, out var version))
            return Task.FromResult<Version?>(version);
        return Task.FromResult<Version?>(null);
    }

    void HandleToastActivated(ToastNotification toastNotification, object args) =>
        appLifecycleManager.ShowWindow();

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
