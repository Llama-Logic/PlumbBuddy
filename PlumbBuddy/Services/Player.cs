namespace PlumbBuddy.Services;

class Player :
    IPlayer
{
    public Player(IPreferences preferences) =>
        this.preferences = preferences;

    readonly IPreferences preferences;

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
        get => preferences.Get(nameof(ScanForMissingMccc), true);
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

    public bool ScanForResourceConflicts
    {
        get => preferences.Get(nameof(ScanForResourceConflicts), true);
        set
        {
            if (ScanForResourceConflicts == value)
                return;
            preferences.Set(nameof(ScanForResourceConflicts), value);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Forget()
    {
        var showThemeManager = ShowThemeManager;
        Type = UserType.Casual;
        preferences.Clear();
        ShowThemeManager = showThemeManager;
    }

    TEnum Get<TEnum>(string key, TEnum defaultValue)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(preferences.Get(key, defaultValue.ToString()), out var value) ? value : defaultValue;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    void Set<TEnum>(string key, TEnum value)
        where TEnum : struct, Enum =>
        preferences.Set(key, value.ToString());
}
