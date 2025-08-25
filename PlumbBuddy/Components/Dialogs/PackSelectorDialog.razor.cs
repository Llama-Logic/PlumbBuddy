namespace PlumbBuddy.Components.Dialogs;

partial class PackSelectorDialog
{
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
        var commandLineArgumentsLine = await ElectronicArtsApp.GetTS4ConfiguredCommandLineArgumentsAsync();
        if (!string.IsNullOrWhiteSpace(commandLineArgumentsLine)
            && GetDisablePacksCommandLineArgumentPattern().Match(commandLineArgumentsLine) is { Success: true } match)
            commandLineArgumentsLine = commandLineArgumentsLine.Remove(match.Index, match.Length);
        var commandLineArguments = string.IsNullOrWhiteSpace(commandLineArgumentsLine) ? [] : commandLineArgumentsLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var disabledPacks = PackGroups.SelectMany(packGroup => packGroup.Packs.Where(pack => !pack.IsChecked).Select(pack => pack.Code)).ToImmutableArray();
        if (disabledPacks.Length is > 0)
            commandLineArguments.Add($"-disablepacks:{string.Join(",", disabledPacks)}");
        var newCommandLineArgumentsLine = commandLineArguments.Count is 0 ? null : string.Join(" ", commandLineArguments);
        if (newCommandLineArgumentsLine != commandLineArgumentsLine)
            await ElectronicArtsApp.SetTS4ConfiguredCommandLineArgumentsAsync(newCommandLineArgumentsLine);
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

    async Task ReloadPackGroupsAsync()
    {
        PackGroups.Clear();
        isLoading = true;
        StateHasChanged();
        var currentlyDisabledPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var commandLineArguments = await ElectronicArtsApp.GetTS4ConfiguredCommandLineArgumentsAsync();
        if (!string.IsNullOrWhiteSpace(commandLineArguments)
            && GetDisablePacksCommandLineArgumentPattern().Match(commandLineArguments) is { Success: true } match)
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
                            ? "Expansion Packs"
                            : packCatalogEntry.Type is PackDescriptionType.Game
                            ? "Game Packs"
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.SubType is PackDescriptionSubType.Full
                            ? "Stuff Packs"
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.KitType is null
                            ? $"{(string.IsNullOrWhiteSpace(packCatalogEntry.EaPromoCode) ? string.Empty : "Creator ")}Combination Kits"
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.KitType is PackDescriptionKitType.CAS
                            ? $"{(string.IsNullOrWhiteSpace(packCatalogEntry.EaPromoCode) ? string.Empty : "Creator ")}Create A Sim Kits"
                            : packCatalogEntry.Type is PackDescriptionType.Stuff && packCatalogEntry.KitType is PackDescriptionKitType.BuildBuy
                            ? $"{(string.IsNullOrWhiteSpace(packCatalogEntry.EaPromoCode) ? string.Empty : "Creator ")}Build & Buy Kits"
                            : "Free",
                            packs
                        )
                    );
                }
                packs.Add(new()
                {
                    Code = packCode,
                    Icon = $"data:image/png;base64,{Convert.ToBase64String((await GameResourceCataloger.GetPackIcon64Async(packCode)).Span)}",
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
                            ? "Expansion Packs"
                            : packGroupKey.Equals("G", StringComparison.OrdinalIgnoreCase)
                            ? "Game Packs"
                            : packGroupKey.Equals("S", StringComparison.OrdinalIgnoreCase)
                            ? "Stuff Packs"
                            : "Free",
                            packs
                        )
                    );
                }
                packs.Add(new()
                {
                    Code = packCode,
                    Icon = $"data:image/png;base64,{Convert.ToBase64String((await GameResourceCataloger.GetPackIcon64Async(packCode)).Span)}",
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
