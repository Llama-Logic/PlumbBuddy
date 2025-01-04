namespace PlumbBuddy.Services;

class Settings :
    ISettings
{
    public Settings(IPreferences preferences) =>
        this.preferences = preferences;

    readonly IPreferences preferences;

    public string ArchiveFolderPath
    {
        get => preferences.Get(nameof(ArchiveFolderPath), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PlumbBuddy", "Archive"));
        set
        {
            if (ArchiveFolderPath == value)
                return;
            preferences.Set(nameof(ArchiveFolderPath), value);
            OnPropertyChanged();
        }
    }

    public bool ArchivistEnabled
    {
        get => preferences.Get(nameof(ArchivistEnabled), true);
        set
        {
            if (ArchivistEnabled == value)
                return;
            preferences.Set(nameof(ArchivistEnabled), value);
            OnPropertyChanged();
        }
    }

    public bool ArchivistAutoIngestSaves
    {
        get => preferences.Get(nameof(ArchivistAutoIngestSaves), false);
        set
        {
            if (ArchivistAutoIngestSaves == value)
                return;
            preferences.Set(nameof(ArchivistAutoIngestSaves), value);
            OnPropertyChanged();
        }
    }

    public bool AutomaticallyCheckForUpdates
    {
        get => preferences.Get(nameof(AutomaticallyCheckForUpdates), false);
        set
        {
            if (AutomaticallyCheckForUpdates == value)
                return;
            preferences.Set(nameof(AutomaticallyCheckForUpdates), value);
            OnPropertyChanged();
        }
    }

    public SmartSimCacheStatus CacheStatus
    {
        get => Get(nameof(CacheStatus), SmartSimCacheStatus.Clear);
        set
        {
            if (CacheStatus == value)
                return;
            Set(nameof(CacheStatus), value);
            OnPropertyChanged();
        }
    }

    public string DefaultCreatorsList
    {
        get => preferences.Get(nameof(DefaultCreatorsList), string.Empty);
        set
        {
            if (DefaultCreatorsList == value)
                return;
            preferences.Set(nameof(DefaultCreatorsList), value);
            OnPropertyChanged();
        }
    }

    public bool DevToolsUnlocked
    {
        get => preferences.Get(nameof(DevToolsUnlocked), false);
        set
        {
            if (DevToolsUnlocked == value)
                return;
            preferences.Set(nameof(DevToolsUnlocked), value);
            OnPropertyChanged();
        }
    }

    public string DownloadsFolderPath
    {
        get => preferences.Get(nameof(DownloadsFolderPath), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        set
        {
            if (DownloadsFolderPath == value)
                return;
            preferences.Set(nameof(DownloadsFolderPath), value);
            OnPropertyChanged();
        }
    }

    public bool GenerateGlobalManifestPackage
    {
        get => preferences.Get(nameof(GenerateGlobalManifestPackage), true);
        set
        {
            if (GenerateGlobalManifestPackage == value)
                return;
            preferences.Set(nameof(GenerateGlobalManifestPackage), value);
            OnPropertyChanged();
        }
    }

    public string InstallationFolderPath
    {
        get => preferences.Get(nameof(InstallationFolderPath), string.Empty);
        set
        {
            if (InstallationFolderPath == value)
                return;
            preferences.Set(nameof(InstallationFolderPath), value);
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? LastCheckForUpdate
    {
        get => Get<DateTimeOffset>(nameof(LastCheckForUpdate), out var dto) ? dto : null;
        set
        {
            if (LastCheckForUpdate == value)
                return;
            preferences.Set(nameof(LastCheckForUpdate), value?.ToString());
            OnPropertyChanged();
        }
    }

    public Version? LastGameVersion
    {
        get => preferences.Get(nameof(LastGameVersion), (string?)null) is string versionStr
            && Version.TryParse(versionStr, out var version)
            ? version
            : null;
        set
        {
            if (LastGameVersion == value)
                return;
            preferences.Set(nameof(LastGameVersion), value?.ToString());
            OnPropertyChanged();
        }
    }

    public bool OfferPatchDayModUpdatesHelp
    {
        get => preferences.Get(nameof(OfferPatchDayModUpdatesHelp), true);
        set
        {
            if (OfferPatchDayModUpdatesHelp == value)
                return;
            preferences.Set(nameof(OfferPatchDayModUpdatesHelp), value);
            OnPropertyChanged();
        }
    }

    public bool Onboarded
    {
        get => preferences.Get(nameof(Onboarded), false);
        set
        {
            if (Onboarded == value)
                return;
            preferences.Set(nameof(Onboarded), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForCacheStaleness
    {
        get => preferences.Get(nameof(ScanForCacheStaleness), true);
        set
        {
            if (ScanForCacheStaleness == value)
                return;
            preferences.Set(nameof(ScanForCacheStaleness), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForCorruptMods
    {
        get => preferences.Get(nameof(ScanForCorruptMods), true);
        set
        {
            if (ScanForCorruptMods == value)
                return;
            preferences.Set(nameof(ScanForCorruptMods), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForCorruptScriptMods
    {
        get => preferences.Get(nameof(ScanForCorruptScriptMods), true);
        set
        {
            if (ScanForCorruptScriptMods == value)
                return;
            preferences.Set(nameof(ScanForCorruptScriptMods), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForErrorLogs
    {
        get => preferences.Get(nameof(ScanForErrorLogs), true);
        set
        {
            if (ScanForErrorLogs == value)
                return;
            preferences.Set(nameof(ScanForErrorLogs), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForLoose7ZipArchives
    {
        get => preferences.Get(nameof(ScanForLoose7ZipArchives), true);
        set
        {
            if (ScanForLoose7ZipArchives == value)
                return;
            preferences.Set(nameof(ScanForLoose7ZipArchives), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForLooseRarArchives
    {
        get => preferences.Get(nameof(ScanForLooseRarArchives), true);
        set
        {
            if (ScanForLooseRarArchives == value)
                return;
            preferences.Set(nameof(ScanForLooseRarArchives), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForLooseZipArchives
    {
        get => preferences.Get(nameof(ScanForLooseZipArchives), true);
        set
        {
            if (ScanForLooseZipArchives == value)
                return;
            preferences.Set(nameof(ScanForLooseZipArchives), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMismatchedInscribedHashes
    {
        get => preferences.Get(nameof(ScanForMismatchedInscribedHashes), true);
        set
        {
            if (ScanForMismatchedInscribedHashes == value)
                return;
            preferences.Set(nameof(ScanForMismatchedInscribedHashes), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMissingBe
    {
        get => preferences.Get(nameof(ScanForMissingBe), false);
        set
        {
            if (ScanForMissingBe == value)
                return;
            preferences.Set(nameof(ScanForMissingBe), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMissingDependency
    {
        get => preferences.Get(nameof(ScanForMissingDependency), true);
        set
        {
            if (ScanForMissingDependency == value)
                return;
            preferences.Set(nameof(ScanForMissingDependency), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMissingMccc
    {
        get => preferences.Get(nameof(ScanForMissingMccc), false);
        set
        {
            if (ScanForMissingMccc == value)
                return;
            preferences.Set(nameof(ScanForMissingMccc), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMissingModGuard
    {
        get => preferences.Get(nameof(ScanForMissingModGuard), true);
        set
        {
            if (ScanForMissingModGuard == value)
                return;
            preferences.Set(nameof(ScanForMissingModGuard), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForInvalidModSubdirectoryDepth
    {
        get => preferences.Get(nameof(ScanForInvalidModSubdirectoryDepth), true);
        set
        {
            if (ScanForInvalidModSubdirectoryDepth == value)
                return;
            preferences.Set(nameof(ScanForInvalidModSubdirectoryDepth), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForInvalidScriptModSubdirectoryDepth
    {
        get => preferences.Get(nameof(ScanForInvalidScriptModSubdirectoryDepth), true);
        set
        {
            if (ScanForInvalidScriptModSubdirectoryDepth == value)
                return;
            preferences.Set(nameof(ScanForInvalidScriptModSubdirectoryDepth), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForModsDisabled
    {
        get => preferences.Get(nameof(ScanForModsDisabled), true);
        set
        {
            if (ScanForModsDisabled == value)
                return;
            preferences.Set(nameof(ScanForModsDisabled), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMultipleModVersions
    {
        get => preferences.Get(nameof(ScanForMultipleModVersions), true);
        set
        {
            if (ScanForMultipleModVersions == value)
                return;
            preferences.Set(nameof(ScanForMultipleModVersions), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForMutuallyExclusiveMods
    {
        get => preferences.Get(nameof(ScanForMutuallyExclusiveMods), true);
        set
        {
            if (ScanForMutuallyExclusiveMods == value)
                return;
            preferences.Set(nameof(ScanForMutuallyExclusiveMods), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForScriptModsDisabled
    {
        get => preferences.Get(nameof(ScanForScriptModsDisabled), true);
        set
        {
            if (ScanForScriptModsDisabled == value)
                return;
            preferences.Set(nameof(ScanForScriptModsDisabled), value);
            OnPropertyChanged();
        }
    }

    public bool ScanForShowModsListAtStartupEnabled
    {
        get => preferences.Get(nameof(ScanForShowModsListAtStartupEnabled), true);
        set
        {
            if (ScanForShowModsListAtStartupEnabled == value)
                return;
            preferences.Set(nameof(ScanForShowModsListAtStartupEnabled), value);
            OnPropertyChanged();
        }
    }

    public bool ShowSystemTrayIcon
    {
        get => preferences.Get(nameof(ShowSystemTrayIcon), false);
        set
        {
            if (ShowSystemTrayIcon == value)
                return;
            preferences.Set(nameof(ShowSystemTrayIcon), value);
            OnPropertyChanged();
        }
    }

    public bool ShowThemeManager
    {
        get
        {
            try
            {
                return preferences.Get(nameof(ShowThemeManager), false);
            }
            catch
            {
                return false;
            }
        }
        set
        {
            if (ShowThemeManager == value)
                return;
            preferences.Set(nameof(ShowThemeManager), value);
            OnPropertyChanged();
        }
    }

    public Version? SkipUpdateVersion
    {
        get => preferences.Get(nameof(SkipUpdateVersion), (string?)null) is string versionStr
            && Version.TryParse(versionStr, out var version)
            ? version
            : null;
        set
        {
            if (SkipUpdateVersion == value)
                return;
            preferences.Set(nameof(SkipUpdateVersion), value?.ToString());
            OnPropertyChanged();
        }
    }

    public string? Theme
    {
        get => preferences.Get<string?>(nameof(Theme), null);
        set
        {
            if (Theme == value)
                return;
            preferences.Set(nameof(Theme), value);
            OnPropertyChanged();
        }
    }

    public UserType Type
    {
        get => Get(nameof(Type), UserType.Casual);
        set
        {
            if (Type == value)
                return;
            Set(nameof(Type), value);
            OnPropertyChanged();
        }
    }

    public string UserDataFolderPath
    {
        get => preferences.Get(nameof(UserDataFolderPath), Path.Combine($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}", "Electronic Arts", "The Sims 4"));
        set
        {
            if (UserDataFolderPath == value)
                return;
            preferences.Set(nameof(UserDataFolderPath), value);
            OnPropertyChanged();
        }
    }

    public bool UsePublicPackCatalog
    {
        get => preferences.Get(nameof(UsePublicPackCatalog), false);
        set
        {
            if (UsePublicPackCatalog == value)
                return;
            preferences.Set(nameof(UsePublicPackCatalog), value);
            OnPropertyChanged();
        }
    }

    public Version? VersionAtLastStartup
    {
        get => preferences.Get(nameof(VersionAtLastStartup), (string?)null) is string versionStr
            && Version.TryParse(versionStr, out var version)
            ? version
            : null;
        set
        {
            if (VersionAtLastStartup == value)
                return;
            preferences.Set(nameof(VersionAtLastStartup), value?.ToString());
            OnPropertyChanged();
        }
    }

    public bool WriteScaffoldingToSubdirectory
    {
        get => preferences.Get(nameof(WriteScaffoldingToSubdirectory), true);
        set
        {
            if (WriteScaffoldingToSubdirectory == value)
                return;
            preferences.Set(nameof(WriteScaffoldingToSubdirectory), value);
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Forget()
    {
        var showThemeManager = ShowThemeManager;
        var versionAtLastStartup = VersionAtLastStartup;
        Type = UserType.Casual;
        preferences.Clear();
        ShowThemeManager = showThemeManager;
        VersionAtLastStartup = versionAtLastStartup;
    }

    TEnum Get<TEnum>(string key, TEnum defaultValue)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(preferences.Get(key, defaultValue.ToString()), out var value) ? value : defaultValue;

    bool Get<TParsable>(string key, [NotNullWhen(true)] out TParsable parsable)
        where TParsable : IParsable<TParsable>
    {
        if (preferences.Get(key, (string?)null) is string valueStr
            && TParsable.TryParse(valueStr, null, out var value))
        {
            parsable = value;
            return true;
        }
        parsable = default!;
        return false;
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    void Set<TEnum>(string key, TEnum value)
        where TEnum : struct, Enum =>
        preferences.Set(key, value.ToString());
}
