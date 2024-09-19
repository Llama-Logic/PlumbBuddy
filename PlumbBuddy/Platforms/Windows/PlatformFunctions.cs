using Windows.UI.Notifications;

namespace PlumbBuddy.Platforms.Windows;

class PlatformFunctions :
    IPlatformFunctions
{
    public PlatformFunctions(ILifetimeScope lifetimeScope)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        this.lifetimeScope = lifetimeScope;
        toastNotifier = ToastNotificationManager.CreateToastNotifier();
    }

    readonly ILifetimeScope lifetimeScope;
    readonly ToastNotifier toastNotifier;

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

    public void SendLocalNotification(string caption, string text)
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
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("explorer.exe", directoryInfo.FullName);

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
}
