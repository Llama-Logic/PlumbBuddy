namespace PlumbBuddy.Services;

public interface ISmartSimObserver :
    IDisposable,
    INotifyPropertyChanged
{
    Version? GameVersion { get; }
    IReadOnlyList<string> InstalledPackCodes { get; }
    bool IsModsDisabledGameSettingOn { get; }
    bool IsScanning { get; }
    bool IsScriptModsEnabledGameSettingOn { get; }
    bool IsShowModListStartupGameSettingOn { get; }
    bool IsSteamInstallation { get; }
    IReadOnlyList<ScanIssue> ScanIssues { get; }

    void ClearCache();
    Task HelpWithPackPurchaseAsync(string packCode, IDialogService dialogService, IReadOnlyList<string>? creators, string? electronicArtsPromoCode);
    Task OpenDownloadsFolderAsync();
    void OpenModsFolder();
}
