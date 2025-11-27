namespace PlumbBuddy.Components.Dialogs;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
partial class PackSelectorDialog
{
    static readonly Dictionary<string, string> cachedBase64PackIcons = [];

    [GeneratedRegex(@"\-disablepacks:(?<packCodes>[^\s]*)")]
    private static partial Regex GetDisablePacksCommandLineArgumentPattern();

    bool isApplying = false;
    bool isLoading = true;

    bool? AllPacks
    {
        get
        {
            var isCheckedValues = PackGroups.SelectMany(packGroup => packGroup.Packs.Select(pack => pack.IsChecked)).Distinct().ToImmutableArray();
            if (isCheckedValues.Length is > 1)
                return null;
            return isCheckedValues[0];
        }
    }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    ObservableRangeCollection<PackGroup> PackGroups { get; } = [];

    public bool UsePublicPackCatalog
    {
        get => Settings.UsePublicPackCatalog;
        set => Settings.UsePublicPackCatalog = value;
    }

    void AllGroupsValueChangedHandler(bool? isChecked)
    {
        foreach (var pack in PackGroups.SelectMany(packGroup => packGroup.Packs))
            pack.IsChecked = isChecked is null or true;
        StateHasChanged();
    }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            PublicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
            SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
        }
    }

    async Task<string> GetPackIconAsync(string packCode)
    {
        if (!cachedBase64PackIcons.TryGetValue(packCode, out var base64Icon))
        {
            base64Icon = Convert.ToBase64String((await GameResourceCataloger.GetPackIcon64Async(packCode)).Span);
            try
            {
                cachedBase64PackIcons.Add(packCode, base64Icon);
            }
            catch (ArgumentException)
            {
            }
        }
        return base64Icon;
    }

    void GroupValueChangedHandler(PackGroup packGroup, bool? isChecked)
    {
        foreach (var pack in packGroup.Packs)
            pack.IsChecked = isChecked is null or true;
        StateHasChanged();
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandlePublicCatalogsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPublicCatalogs.PackCatalog))
            ReloadPackGroups();
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.InstalledPackCodes))
            ReloadPackGroups();
    }

    async Task OkOnClickHandlerAsync()
    {
        isApplying = true;
        StateHasChanged();
        if (await SmartSimObserver.CheckIfGameIsRunningAsync())
        {
            isApplying = false;
            StateHasChanged();
            return;
        }
        var commandLineArgumentsLine = SmartSimObserver.IsSteamInstallation
            ? await Steam.GetTS4ConfiguredCommandLineArgumentsAsync()
            : await ElectronicArtsApp.GetTS4ConfiguredCommandLineArgumentsAsync();
        if (!string.IsNullOrWhiteSpace(commandLineArgumentsLine)
            && GetDisablePacksCommandLineArgumentPattern().Match(commandLineArgumentsLine) is { Success: true } match)
            commandLineArgumentsLine = commandLineArgumentsLine.Remove(match.Index, match.Length);
        var commandLineArguments = string.IsNullOrWhiteSpace(commandLineArgumentsLine) ? [] : commandLineArgumentsLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var disabledPacks = PackGroups.SelectMany(packGroup => packGroup.Packs.Where(pack => !pack.IsChecked).Select(pack => pack.Code)).ToImmutableArray();
        if (disabledPacks.Length is > 0)
            commandLineArguments.Add($"-disablepacks:{string.Join(",", disabledPacks)}");
        var newCommandLineArgumentsLine = commandLineArguments.Count is 0 ? null : string.Join(" ", commandLineArguments);
        if (newCommandLineArgumentsLine != commandLineArgumentsLine)
        {
            var inGamePackSelectorActive = false;
            var userSettingIniFile = new FileInfo(Path.Combine(Settings.UserDataFolderPath, "UserSetting.ini"));
            if (userSettingIniFile.Exists)
            {
                try
                {
                    var parser = new IniDataParser();
                    var data = parser.Parse(await File.ReadAllTextAsync(userSettingIniFile.FullName));
                    var usersettingData = data["usersetting"];
                    inGamePackSelectorActive = !string.IsNullOrWhiteSpace(usersettingData["packstoskipmount"]);
                }
                catch (ParsingException ex)
                {
                    // eww, a bad INI file?
                    Logger.LogWarning(ex, "attempting to parse the game user setting INI file at {path} failed", userSettingIniFile.FullName);
                }
            }
            if (inGamePackSelectorActive)
            {
                var nullableResetInGamePackSelector = await DialogService.ShowQuestionDialogAsync(AppText.PackSelectorDialog_Question_ResetInGame_Caption, AppText.PackSelectorDialog_Question_ResetInGame_Text, userCanCancel: true);
                if (nullableResetInGamePackSelector is not { } resetInGamePackSelector)
                {
                    isApplying = false;
                    StateHasChanged();
                    return;
                }
                if (resetInGamePackSelector)
                {
                    try
                    {
                        var parser = new IniDataParser();
                        var data = parser.Parse(await File.ReadAllTextAsync(userSettingIniFile.FullName));
                        var usersettingData = data["usersetting"];
                        usersettingData["packstoskipmount"] = string.Empty;
                        await File.WriteAllTextAsync(userSettingIniFile.FullName, data.ToString());
                    }
                    catch (ParsingException ex)
                    {
                        // eww, a bad INI file?
                        Logger.LogWarning(ex, "attempting to parse the game user setting INI file at {path} failed", userSettingIniFile.FullName);
                    }
                }
            }
            if (SmartSimObserver.IsSteamInstallation)
                await Steam.SetTS4ConfiguredCommandLineArgumentsAsync(newCommandLineArgumentsLine);
            else
                await ElectronicArtsApp.SetTS4ConfiguredCommandLineArgumentsAsync(newCommandLineArgumentsLine);
        }
        MudDialog?.Close(DialogResult.Cancel());
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await ReloadPackGroupsAsync();
    }

    protected override void OnInitialized()
    {
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
    }

    void ReloadPackGroups() =>
        _ = StaticDispatcher.DispatchAsync(ReloadPackGroupsAsync);

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task ReloadPackGroupsAsync()
    {
        PackGroups.Clear();
        isLoading = true;
        StateHasChanged();
        var currentlyDisabledPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var commandLineArgumentsLine = SmartSimObserver.IsSteamInstallation
            ? await Steam.GetTS4ConfiguredCommandLineArgumentsAsync()
            : await ElectronicArtsApp.GetTS4ConfiguredCommandLineArgumentsAsync();
        if (!string.IsNullOrWhiteSpace(commandLineArgumentsLine)
            && GetDisablePacksCommandLineArgumentPattern().Match(commandLineArgumentsLine) is { Success: true } match)
            foreach (var packCode in match.Groups["packCodes"].Value.Split(","))
                currentlyDisabledPacks.Add(packCode);
        var packGroups = new Dictionary<string, (string name, IList<Pack> packs)>();
        if (PublicCatalogs.PackCatalog is { } packCatalog)
        {
            foreach (var packCode in SmartSimObserver.InstalledPackCodes.Order())
            {
                if (!packCatalog.TryGetValue(packCode, out var packCatalogEntry))
                    continue;
                IList<Pack> packs;
                var packGroupKey = $"{packCatalogEntry.Type}{packCatalogEntry.SubType}{packCatalogEntry.KitType}{(string.IsNullOrWhiteSpace(packCatalogEntry.EaPromoCode) ? string.Empty : "Creator")}";
                if (packGroups.TryGetValue(packGroupKey, out var packGroup))
                    packs = packGroup.packs;
                else
                {
                    packs = [];
                    packGroups.Add
                    (
                        packGroupKey,
                        (
                            packCatalogEntry.Type is PackDescriptionType.Expansion
                            ? AppText.PackSelectorDialog_PackType_Expansion
                            : packCatalogEntry.Type is PackDescriptionType.Game
                            ? AppText.PackSelectorDialog_PackType_Game
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.SubType is PackDescriptionSubType.Full
                            ? AppText.PackSelectorDialog_PackType_Stuff
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.KitType is null
                            ? (packCatalogEntry.IsCreatorContent ? AppText.PackSelectorDialog_PackType_CreatorCombinationKit : AppText.PackSelectorDialog_PackType_CombinationKit)
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.KitType is PackDescriptionKitType.CAS
                            ? (packCatalogEntry.IsCreatorContent ? AppText.PackSelectorDialog_PackType_CreatorCreateASimKit : AppText.PackSelectorDialog_PackType_CreateASimKit)
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.KitType is PackDescriptionKitType.BuildBuy
                            ? (packCatalogEntry.IsCreatorContent ? AppText.PackSelectorDialog_PackType_CreatorBuildAndBuyKit : AppText.PackSelectorDialog_PackType_BuildAndBuyKit)
                            : AppText.PackSelectorDialog_PackType_Free,
                            packs
                        )
                    );
                }
                packs.Add(new()
                {
                    Code = packCode,
                    Icon = $"data:image/png;base64,{await GetPackIconAsync(packCode)}",
                    IsChecked = !currentlyDisabledPacks.Contains(packCode),
                    Name = packCatalogEntry.EnglishName
                });
            }
        }
        else
        {
            foreach (var packCode in SmartSimObserver.InstalledPackCodes.Order())
            {
                IList<Pack> packs;
                var packGroupKey = packCode[..1];
                if (packGroups.TryGetValue(packGroupKey, out var packGroup))
                    packs = packGroup.packs;
                else
                {
                    packs = [];
                    packGroups.Add
                    (
                        packGroupKey,
                        (
                            packGroupKey.Equals("E", StringComparison.OrdinalIgnoreCase)
                            ? AppText.PackSelectorDialog_PackType_Expansion
                            : packGroupKey.Equals("G", StringComparison.OrdinalIgnoreCase)
                            ? AppText.PackSelectorDialog_PackType_Game
                            : packGroupKey.Equals("S", StringComparison.OrdinalIgnoreCase)
                            ? AppText.PackSelectorDialog_PackType_Stuff
                            : AppText.PackSelectorDialog_PackType_Free,
                            packs
                        )
                    );
                }
                packs.Add(new()
                {
                    Code = packCode,
                    Icon = $"data:image/png;base64,{await GetPackIconAsync(packCode)}",
                    IsChecked = !currentlyDisabledPacks.Contains(packCode),
                    Name = packCode
                });
            }
        }
        PackGroups.AddRange(packGroups.OrderBy(kv => kv.Key).Select(kv => new PackGroup() { Name = kv.Value.name, Packs = kv.Value.packs.AsReadOnly() }));
        isLoading = false;
        StateHasChanged();
    }

    void ValueChangedHandler(string packCode, bool isChecked)
    {
        foreach (var packGroup in PackGroups)
            foreach (var pack in packGroup.Packs)
                if (pack.Code.Equals(packCode, StringComparison.OrdinalIgnoreCase))
                    pack.IsChecked = isChecked;
        StateHasChanged();
    }

    class PackGroup
    {
        public bool? IsChecked
        {
            get
            {
                var isCheckedValues = Packs.Select(pack => pack.IsChecked).Distinct().ToImmutableArray();
                if (isCheckedValues.Length is > 1)
                    return null;
                return isCheckedValues[0];
            }
        }

        public required string Name { get; init; }
        public required IReadOnlyList<Pack> Packs { get; init; }
    }

    class Pack
    {
        public required string Code { get; init; }
        public required string Icon { get; init; }
        public bool IsChecked { get; set; }
        public required string Name { get; init; }
    }
}
