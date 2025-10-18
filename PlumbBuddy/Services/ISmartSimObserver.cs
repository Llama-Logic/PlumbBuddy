namespace PlumbBuddy.Services;

public interface ISmartSimObserver :
    IDisposable,
    INotifyPropertyChanged
{
    IReadOnlyList<string> DisabledPackCodes { get; }
    Version? GameVersion { get; }
    IReadOnlyList<string> InstalledPackCodes { get; }
    bool IsModsDisabledGameSettingOn { get; }
    bool IsPerformanceProcessorAffinityInEffect { get; }
    bool IsScanning { get; }
    bool IsScriptModsEnabledGameSettingOn { get; }
    bool IsShowModListStartupGameSettingOn { get; }
    bool IsSteamInstallation { get; }
    IReadOnlyList<ScanIssue> ScanIssues { get; }

    Task<bool> ClearCacheAsync();
    Task HelpWithPackPurchaseAsync(string packCode, IDialogService dialogService, IReadOnlyList<string>? creators, string? electronicArtsPromoCode);
    Task OpenDownloadsFolderAsync();
    void OpenModsFolder();
    void Scan();
}
