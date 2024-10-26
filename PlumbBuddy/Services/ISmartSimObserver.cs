namespace PlumbBuddy.Services;

public interface ISmartSimObserver :
    IDisposable,
    INotifyPropertyChanged
{
    IReadOnlyList<string> InstalledPackCodes { get; }
    bool IsCurrentlyScanning { get; }
    bool IsModsDisabledGameSettingOn { get; }
    bool IsScriptModsEnabledGameSettingOn { get; }
    bool IsShowModListStartupGameSettingOn { get; }
    bool IsSteamInstallation { get; }
    IReadOnlyList<ScanIssue> ScanIssues { get; }

    void ClearCache();
    Task HelpWithPackPurchaseAsync(string packCode, IDialogService dialogService, IReadOnlyList<string>? creators, string? electronicArtsPromoCode);
    void OpenDownloadsFolder();
    void OpenModsFolder();
}
