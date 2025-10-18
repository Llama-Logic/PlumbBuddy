namespace PlumbBuddy.Services.Scans;

public sealed class DependencyScan :
    Scan,
    IDependencyScan
{
    [Flags]
    enum MissingDependencyModResolutionType :
        int
    {
        CompleteMetadata = 0,
        UnnamedDependent = 0x1,
        UnnamedDependency = 0x2,
        IdenticallyNamed = 0x4,
        UnspecifiedDependentSource = 0x8,
        UnspecifiedDependencySource = 0x10,

        Normal = CompleteMetadata,
        FileNeedsDependency = UnnamedDependent,
        ModNeedsDownload = UnnamedDependency,
        FileNeedsDownload = UnnamedDependent | UnnamedDependency,
        ReinstallMod = UnspecifiedDependencySource,
        ReinstallFile = UnspecifiedDependencySource | UnnamedDependent,
        BrokenFile = UnspecifiedDependentSource | UnspecifiedDependencySource
    }

    record ModWithDisabledPacks(string Name, IReadOnlyList<string> Creators, string? ElectronicArtsPromoCode, IReadOnlyList<string> MissingPackCodes, IReadOnlyList<string> FilePaths);
    record ModWithIncompatiblePacks(string Name, IReadOnlyList<string> IncompatiblePackCodes, IReadOnlyList<string> FilePaths);
    record ModWithMissingDependencyMod(long Id, long ModFileManifestId, string? RequirementIdentifier, int CommonRequirementIdentifiers, string? Name, IReadOnlyList<string> Creators, Uri? Url, string? DependencyName, IReadOnlyList<string> DependencyCreators, Uri? DependencyUrl, IReadOnlyList<string> FilePaths, bool WasFeatureRemoved);
    record ModWithMissingPacks(string Name, IReadOnlyList<string> Creators, string? ElectronicArtsPromoCode, IReadOnlyList<string> MissingPackCodes, IReadOnlyList<string> FilePaths);

    public DependencyScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, IBlazorFramework blazorFramework, ISettings settings, ISmartSimObserver smartSimObserver, IPublicCatalogs publicCatalogs)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        ArgumentNullException.ThrowIfNull(publicCatalogs);
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.blazorFramework = blazorFramework;
        this.settings = settings;
        this.smartSimObserver = smartSimObserver;
        this.publicCatalogs = publicCatalogs;
    }

    readonly IBlazorFramework blazorFramework;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly IPublicCatalogs publicCatalogs;
    readonly ISettings settings;
    readonly ISmartSimObserver smartSimObserver;

    public override async Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (resolutionData is string resolutionStr)
        {
            if (issueData is ModWithMissingPacks modWithMissingPacks && resolutionStr.StartsWith("purchase-"))
                await smartSimObserver.HelpWithPackPurchaseAsync(resolutionStr[9..], blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>(), modWithMissingPacks.Creators, modWithMissingPacks.ElectronicArtsPromoCode).ConfigureAwait(false);
            else if (resolutionStr is "selectPacks")
                await blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().ShowPackSelectorDialogAsync();
            else if (resolutionStr.StartsWith("showfile-") && new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", resolutionStr[9..])) is { } modFile && modFile.Exists)
                platformFunctions.ViewFile(modFile);
            else if (resolutionStr is "stopTellingMe")
                settings.ScanForMissingDependency = false;
        }
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        var installedPackCodes = smartSimObserver.InstalledPackCodes;
        var disabledPackCodes = smartSimObserver.DisabledPackCodes;
        var availablePackCodes = installedPackCodes.Except(disabledPackCodes).ToList().AsReadOnly();
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var modWithMissingPacks in pbDbContext.ModFileManifests
            .Where(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null) && mfm.RequiredPacks.Any(pc => !installedPackCodes.Contains(pc.Code.ToUpper())))
            .Select(mfm => new ModWithMissingPacks
            (
                mfm.Name,
                mfm.Creators.Select(c => c.Name).ToList(),
                mfm.ElectronicArtsPromoCode == null ? null : mfm.ElectronicArtsPromoCode.Code,
                mfm.RequiredPacks.Where(pc => !installedPackCodes.Contains(pc.Code.ToUpper())).Select(pc => pc.Code.ToUpper()).ToList(),
                mfm.ModFileHash.ModFiles.Where(mf => mf.FoundAbsent == null).Select(mf => mf.Path).ToList()
            ))
            .AsAsyncEnumerable())
        {
            yield return new ScanIssue
            {
                Caption = string.Format(AppText.Scan_Dependency_RequiredPack_Caption, string.IsNullOrWhiteSpace(modWithMissingPacks.Name) ? AppText.Scan_Dependency_ModNameFallback : modWithMissingPacks.Name, AppText.Scan_Dependency_PackNoun.ToQuantity(modWithMissingPacks.MissingPackCodes.Count)),
                Description = string.Format(AppText.Scan_Dependency_RequiredPack_Description, string.IsNullOrWhiteSpace(modWithMissingPacks.Name) ? AppText.Scan_Dependency_ModNameFallback : modWithMissingPacks.Name, (publicCatalogs.PackCatalog is { } packCatalog ? modWithMissingPacks.MissingPackCodes.Select(packCode => packCatalog.TryGetValue(packCode, out var pack) ? pack.EnglishName : packCode) : modWithMissingPacks.MissingPackCodes).Humanize(), smartSimObserver.IsSteamInstallation ? AppText.Common_Steam : AppText.Common_TheEAApp),
                Icon = MaterialDesignIcons.Normal.BagPersonalOff,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = modWithMissingPacks,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthDependencyScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    ..modWithMissingPacks.MissingPackCodes.Select(missingPackCode => new ScanIssueResolution
                    {
                        Label = string.Format(AppText.Scan_Dependency_RequiredPack_HelpMePurchase_Label, publicCatalogs.PackCatalog is { } packCatalog && packCatalog.TryGetValue(missingPackCode, out var missingPack) ? missingPack.EnglishName : missingPackCode),
                        Icon = MaterialDesignIcons.Normal.Store,
                        Color = MudBlazor.Color.Primary,
                        Data = $"purchase-{missingPackCode}"
                    }),
                    ..modWithMissingPacks.FilePaths.Select(filePath => new ScanIssueResolution
                    {
                        Label = string.Format(AppText.Scan_Common_ShowMeTheFile_Label, string.IsNullOrWhiteSpace(modWithMissingPacks.Name) ? AppText.Scan_Common_ShowMeTheFile_Label_ModNameFallback : modWithMissingPacks.Name),
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_Dependency_RequiredPack_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
        }
        await foreach (var modWithDisabledPacks in pbDbContext.ModFileManifests
            .Where(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null) && mfm.RequiredPacks.Any(pc => disabledPackCodes.Contains(pc.Code.ToUpper())))
            .Select(mfm => new ModWithDisabledPacks
            (
                mfm.Name,
                mfm.Creators.Select(c => c.Name).ToList(),
                mfm.ElectronicArtsPromoCode == null ? null : mfm.ElectronicArtsPromoCode.Code,
                mfm.RequiredPacks.Where(pc => disabledPackCodes.Contains(pc.Code.ToUpper())).Select(pc => pc.Code.ToUpper()).ToList(),
                mfm.ModFileHash.ModFiles.Where(mf => mf.FoundAbsent == null).Select(mf => mf.Path).ToList()
            ))
            .AsAsyncEnumerable())
        {
            yield return new ScanIssue
            {
                Caption = string.Format(AppText.Scan_Dependency_DisabledPack_Caption, string.IsNullOrWhiteSpace(modWithDisabledPacks.Name) ? AppText.Scan_Dependency_ModNameFallback : modWithDisabledPacks.Name, AppText.Scan_Dependency_PackNoun.ToQuantity(modWithDisabledPacks.MissingPackCodes.Count)),
                Description = string.Format(AppText.Scan_Dependency_DisabledPack_Description, string.IsNullOrWhiteSpace(modWithDisabledPacks.Name) ? AppText.Scan_Dependency_ModNameFallback : modWithDisabledPacks.Name, (publicCatalogs.PackCatalog is { } packCatalog ? modWithDisabledPacks.MissingPackCodes.Select(packCode => packCatalog.TryGetValue(packCode, out var pack) ? pack.EnglishName : packCode) : modWithDisabledPacks.MissingPackCodes).Humanize()),
                Icon = MaterialDesignIcons.Normal.BagPersonalOff,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = modWithDisabledPacks,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthDependencyScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    ..modWithDisabledPacks.MissingPackCodes.Select(disabledPackCode => new ScanIssueResolution
                    {
                        Label = string.Format(AppText.Scan_Dependency_DisabledPack_HelpMeEnable_Label, publicCatalogs.PackCatalog is { } packCatalog && packCatalog.TryGetValue(disabledPackCode, out var disabledPack) ? disabledPack.EnglishName : disabledPackCode),
                        Icon = MaterialDesignIcons.Normal.BagPersonalTag,
                        Color = MudBlazor.Color.Primary,
                        Data = "selectPacks"
                    }),
                    ..modWithDisabledPacks.FilePaths.Select(filePath => new ScanIssueResolution
                    {
                        Label = string.Format(AppText.Scan_Common_ShowMeTheFile_Label, string.IsNullOrWhiteSpace(modWithDisabledPacks.Name) ? AppText.Scan_Common_ShowMeTheFile_Label_ModNameFallback : modWithDisabledPacks.Name),
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_Dependency_DisabledPack_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
        }
        await foreach (var modWithIncompatiblePacks in pbDbContext.ModFileManifests
            .Where(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null) && mfm.IncompatiblePacks.Any(pc => installedPackCodes.Contains(pc.Code.ToUpper())))
            .Select(mfm => new ModWithIncompatiblePacks
            (
                mfm.Name,
                mfm.IncompatiblePacks.Where(pc => installedPackCodes.Contains(pc.Code.ToUpper())).Select(pc => pc.Code.ToUpper()).ToList(),
                mfm.ModFileHash.ModFiles.Where(mf => mf.FoundAbsent == null).Select(mf => mf.Path).ToList()
            ))
            .AsAsyncEnumerable())
            yield return new ScanIssue
            {
                Caption = string.Format(AppText.Scan_Dependency_IncompatiblePack_Caption, string.IsNullOrWhiteSpace(modWithIncompatiblePacks.Name) ? AppText.Scan_Dependency_ModNameFallback : modWithIncompatiblePacks.Name, AppText.Scan_Dependency_PackNoun.ToQuantity(modWithIncompatiblePacks.IncompatiblePackCodes.Count)),
                Description = string.Format(AppText.Scan_Dependency_IncompatiblePack_Description, string.IsNullOrWhiteSpace(modWithIncompatiblePacks.Name) ? AppText.Scan_Dependency_ModNameFallback : modWithIncompatiblePacks.Name, (publicCatalogs.PackCatalog is { } packCatalog ? modWithIncompatiblePacks.IncompatiblePackCodes.Select(packCode => packCatalog.TryGetValue(packCode, out var pack) ? pack.EnglishName : packCode) : modWithIncompatiblePacks.IncompatiblePackCodes).Humanize()),
                Icon = MaterialDesignIcons.Normal.BagPersonal,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = modWithIncompatiblePacks,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthDependencyScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    new()
                    {
                        Label = AppText.Scan_Dependency_IncompatiblePack_HelpMeDisable_Label,
                        Icon = MaterialDesignIcons.Normal.BagPersonalTag,
                        Color = MudBlazor.Color.Primary,
                        Data = "selectPacks"
                    },
                    ..modWithIncompatiblePacks.FilePaths.Select(filePath => new ScanIssueResolution
                    {
                        Label = string.Format(AppText.Scan_Common_ShowMeTheFile_Label, string.IsNullOrWhiteSpace(modWithIncompatiblePacks.Name) ? AppText.Scan_Common_ShowMeTheFile_Label_ModNameFallback : modWithIncompatiblePacks.Name),
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_Dependency_IncompatiblePack_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
        var modsWithMissingDependencyMod = new List<ModWithMissingDependencyMod>();
        var commonRequirementIdentifiers = new Dictionary<(long modManifestId, string requirementIdentifier), List<ModWithMissingDependencyMod>>();
        Expression<Func<RequiredMod, bool>> requiredModAbsenceIgnored = rm =>
            rm.IgnoreIfHashAvailable != null // if hash available is set
                && (rm.IgnoreIfHashAvailable.ManifestsByCalculation.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null)) // and hash is available by calculation
                || rm.IgnoreIfHashAvailable.ManifestsBySubsumption.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null))) // or hash is available by subsumption
            || rm.IgnoreIfHashUnavailable != null // or if hash unavailable is set
                && !rm.IgnoreIfHashUnavailable.ManifestsByCalculation.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null)) // and hash is not available by calculation
                && !rm.IgnoreIfHashUnavailable.ManifestsBySubsumption.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null)) // and hash is not available by subsumption
            || rm.IgnoreIfPackAvailable != null // if pack available is set
                && availablePackCodes.Contains(rm.IgnoreIfPackAvailable.Code) // pack is available
            || rm.IgnoreIfPackUnavailable != null // if pack unavailable is set
                && !availablePackCodes.Contains(rm.IgnoreIfPackUnavailable.Code); // pack is unavailable
        Expression<Func<RequiredMod, bool>> requiredModAbsent = rm =>
            rm.Hashes.Any(h => // at least one required hash is
            !h.ManifestsByCalculation.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null)) //... not available via calculation
                && !h.ManifestsBySubsumption.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null))) // and not available via subsumption
            || rm.RequiredFeatures.Any(rf => !rm.Hashes.Any(h => // ... or at least one feature is not
                h.ManifestsByCalculation.Any(mfm => mfm.Features.Any(f => f.Name == rf.Name)) // ... available via calculation
                || h.ManifestsBySubsumption.Any(mfm => mfm.Features.Any(f => f.Name == rf.Name)))); // ... or available via subsumption
        await foreach (var modWithMissingDependencyMod in pbDbContext.RequiredMods
            .AsExpandable()
            .Where
            (
                rm =>
                rm.ModFileManifest.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null) // mod is present in Mods folder
                && !requiredModAbsenceIgnored.Invoke(rm)
                &&
                (
                    rm.RequirementIdentifier == null
                    && requiredModAbsent.Invoke(rm)
                    || rm.RequirementIdentifier != null
                    && rm.ModFileManifest.RequiredMods.Where(rm2 => rm2.RequirementIdentifierId == rm.RequirementIdentifierId).All(rm2 => requiredModAbsent.Invoke(rm2))
                )
            )
            .Select
            (
                rm => new ModWithMissingDependencyMod
                (
                    rm.Id,
                    rm.ModFileManfiestId,
                    rm.RequirementIdentifier == null ? null : rm.RequirementIdentifier.Identifier,
                    rm.ModFileManifest.RequiredMods.Count(rm2 => rm2.RequirementIdentifierId == rm.RequirementIdentifierId),
                    rm.ModFileManifest.Name,
                    rm.ModFileManifest.Creators.Select(c => c.Name).ToList(),
                    rm.ModFileManifest.Url,
                    rm.Name,
                    rm.Creators.Select(c => c.Name).ToList(),
                    rm.Url,
                    rm.ModFileManifest.ModFileHash.ModFiles.Where(mf => mf.FoundAbsent == null).Select(mf => mf.Path).ToList(),
                    rm.Hashes.All(h => // all hashes are
                        h.ManifestsByCalculation.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null)) //... available via calculation
                            || h.ManifestsBySubsumption.Any(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null))) // or available via subsumption
                )
            )
            .AsAsyncEnumerable())
        {
            if (modWithMissingDependencyMod.RequirementIdentifier is null)
            {
                modsWithMissingDependencyMod.Add(modWithMissingDependencyMod);
                continue;
            }
            var key = (modWithMissingDependencyMod.ModFileManifestId, modWithMissingDependencyMod.RequirementIdentifier);
            if (!commonRequirementIdentifiers.TryGetValue(key, out var list))
            {
                list = [];
                commonRequirementIdentifiers.Add(key, list);
            }
            list.Add(modWithMissingDependencyMod);
        }
        static string getByLine(IReadOnlyList<string> creators) =>
            creators.Any() ? string.Format(AppText.Scan_Common_ByLine, creators.Humanize()) : string.Empty;
        static (string caption, string description, ScanIssueResolution? resolution) getResolution(ModWithMissingDependencyMod modWithMissingDependencyMod)
        {
            var resolutionType = MissingDependencyModResolutionType.Normal;
            if (string.IsNullOrWhiteSpace(modWithMissingDependencyMod.Name))
                resolutionType |= MissingDependencyModResolutionType.UnnamedDependent;
            if (modWithMissingDependencyMod.Url is null)
                resolutionType |= MissingDependencyModResolutionType.UnspecifiedDependentSource;
            if (string.IsNullOrWhiteSpace(modWithMissingDependencyMod.DependencyName))
                resolutionType |= MissingDependencyModResolutionType.UnnamedDependency;
            if (modWithMissingDependencyMod.DependencyUrl is null)
                resolutionType |= MissingDependencyModResolutionType.UnspecifiedDependencySource;
            if (modWithMissingDependencyMod.Name == modWithMissingDependencyMod.DependencyName)
                resolutionType |= MissingDependencyModResolutionType.IdenticallyNamed;
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.BrokenFile))
                return
                (
                    AppText.Scan_Dependency_BrokenFile_Caption,
                    string.Format(AppText.Scan_Dependency_BrokenFile_Description, AppText.Scan_Dependency_FileNoun_LowerCase.ToQuantity(modWithMissingDependencyMod.FilePaths.Count), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()),
                    null
                );
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.ReinstallFile))
                return
                (
                    AppText.Scan_Dependency_ReinstallFile_Caption,
                    string.Format(AppText.Scan_Dependency_ReinstallFile_Description, AppText.Scan_Dependency_FileNoun_LowerCase.ToQuantity(modWithMissingDependencyMod.FilePaths.Count), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()),
                    new()
                    {
                        Label = string.Format(AppText.Scan_Dependency_ReinstallFile_Download_Label, modWithMissingDependencyMod.Name),
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadDependent",
                        Url = modWithMissingDependencyMod.Url
                    }
                );
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.ReinstallMod))
                return
                (
                    string.Format(AppText.Scan_Dependency_ReinstallMod_Caption, modWithMissingDependencyMod.Name),
                    string.Format(AppText.Scan_Dependency_ReinstallMod_Description, modWithMissingDependencyMod.Name, getByLine(modWithMissingDependencyMod.Creators), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()),
                    new()
                    {
                        Label = string.Format(AppText.Scan_Dependency_ReinstallMod_Redownload_Label, modWithMissingDependencyMod.Name),
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadDependent",
                        Url = modWithMissingDependencyMod.Url
                    }
                );
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.FileNeedsDownload))
                return
                (
                    AppText.Scan_Dependency_FileNeedsDownload_Caption,
                    string.Format(AppText.Scan_Dependency_FileNeedsDownload_Description, AppText.Scan_Dependency_FileNoun_LowerCase.ToQuantity(modWithMissingDependencyMod.FilePaths.Count), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()),
                    new()
                    {
                        Label = AppText.Scan_Dependency_FileNeedsDownload_Download_Label,
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadDependency",
                        Url = modWithMissingDependencyMod.DependencyUrl
                    }
                );
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.ModNeedsDownload))
                return
                (
                    string.Format(AppText.Scan_Dependency_ModNeedsDownload_Caption, modWithMissingDependencyMod.Name),
                    string.Format(AppText.Scan_Dependency_ModNeedsDownload_Description, modWithMissingDependencyMod.Name, getByLine(modWithMissingDependencyMod.Creators), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()),
                    new()
                    {
                        Label = AppText.Scan_Dependency_ModNeedsDownload_Download_Label,
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadDependency",
                        Url = modWithMissingDependencyMod.DependencyUrl
                    }
                );
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.FileNeedsDependency))
                return
                (
                    string.Format(AppText.Scan_Dependency_FileNeedsDependency_Caption, modWithMissingDependencyMod.DependencyName),
                    string.Format(AppText.Scan_Dependency_FileNeedsDependency_Description, AppText.Scan_Dependency_FileNoun_LowerCase.ToQuantity(modWithMissingDependencyMod.FilePaths.Count), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize(), modWithMissingDependencyMod.DependencyName, getByLine(modWithMissingDependencyMod.DependencyCreators)),
                    new()
                    {
                        Label = string.Format(AppText.Scan_Dependency_FileNeedsDependency_Download_Label, modWithMissingDependencyMod.DependencyName),
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadDependency",
                        Url = modWithMissingDependencyMod.DependencyUrl
                    }
                );
            if (resolutionType.HasFlag(MissingDependencyModResolutionType.IdenticallyNamed))
                return
                (
                    string.Format(AppText.Scan_Dependency_IdenticallyNamed_Caption, modWithMissingDependencyMod.Name),
                    string.Format(AppText.Scan_Dependency_IdenticallyNamed_Description, modWithMissingDependencyMod.Name, getByLine(modWithMissingDependencyMod.Creators), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()),
                    new()
                    {
                        Label = string.Format(AppText.Scan_Dependency_IdenticallyNamed_Download_Label, modWithMissingDependencyMod.Name),
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadComponent",
                        Url = modWithMissingDependencyMod.Url
                    }
                );
            return
                (
                    string.Format(AppText.Scan_Dependency_ModNeedsMod_Caption, modWithMissingDependencyMod.Name, modWithMissingDependencyMod.DependencyName),
                    string.Format(AppText.Scan_Dependency_ModNeedsMod_Description, modWithMissingDependencyMod.Name, getByLine(modWithMissingDependencyMod.Creators), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize(), modWithMissingDependencyMod.DependencyName, getByLine(modWithMissingDependencyMod.DependencyCreators)),
                    new()
                    {
                        Label = string.Format(AppText.Scan_Dependency_ModNeedsMod_Download_Label, modWithMissingDependencyMod.DependencyName),
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Primary,
                        Data = "downloadDependency",
                        Url = modWithMissingDependencyMod.DependencyUrl
                    }
                );
        }
        var commonRequirementIdentifiersValuesBySolitude = commonRequirementIdentifiers.Values
            .ToLookup(list => list.Count is 1);
        foreach (var modWithMissingDependencyMod in modsWithMissingDependencyMod.Concat(commonRequirementIdentifiersValuesBySolitude[true].SelectMany(list => list)))
        {
            var (caption, description, resolution) = getResolution(modWithMissingDependencyMod);
            yield return new()
            {
                Caption = caption,
                Description = description,
                Icon = MaterialDesignIcons.Outline.PuzzleRemove,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = modWithMissingDependencyMod,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthDependencyScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    ..(resolution is null
                        ? Enumerable.Empty<ScanIssueResolution>()
                        : [resolution])
                        .Concat
                        (
                            modWithMissingDependencyMod.FilePaths.Select(filePath => new ScanIssueResolution()
                            {
                                Label = string.Format(AppText.Scan_Common_ShowMeTheFile_Label, string.IsNullOrWhiteSpace(modWithMissingDependencyMod.Name) ? AppText.Scan_Common_ShowMeTheFile_Label_ModNameFallback : modWithMissingDependencyMod.Name),
                                Icon = MaterialDesignIcons.Normal.FileFind,
                                Color = MudBlazor.Color.Secondary,
                                Data = $"showfile-{filePath}"
                            })
                        ),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_Dependency_MissingMod_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
        }
        foreach (var list in commonRequirementIdentifiersValuesBySolitude[false])
        {
            var modWithMissingDependencyMod = list.First();
            var downloadResolutions = list
                .DistinctBy(modWithMissingDependencyMod => modWithMissingDependencyMod.DependencyUrl)
                .Select(modWithMissingDependencyMod => getResolution(modWithMissingDependencyMod).resolution)
                .ToImmutableArray();
            yield return new()
            {
                Caption = string.Format(AppText.Scan_Dependency_UnmetRequirement_Caption, modWithMissingDependencyMod.Name ?? AppText.Scan_Dependency_ModNameFallback),
                Description = string.Format(AppText.Scan_Dependency_UnmetRequirement_Description, modWithMissingDependencyMod.Name, getByLine(modWithMissingDependencyMod.Creators), modWithMissingDependencyMod.FilePaths.Select(filePath => $"`{filePath}`").Humanize(), modWithMissingDependencyMod.RequirementIdentifier, downloadResolutions.Length is 1 ? string.Format(AppText.Scan_Dependency_UnmetRequirement_Description_CauseMissingComponent, modWithMissingDependencyMod.RequirementIdentifier) : AppText.Scan_Dependency_UnmetRequirement_Description_CauseAlternative),
                Icon = MaterialDesignIcons.Outline.PuzzleRemove,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = list,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthDependencyScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    ..downloadResolutions
                        .Concat
                        (
                            modWithMissingDependencyMod.FilePaths.Select(filePath => new ScanIssueResolution()
                            {
                                Label = string.Format(AppText.Scan_Common_ShowMeTheFile_Label, modWithMissingDependencyMod.Name ?? AppText.Scan_Common_ShowMeTheFile_Label_ModNameFallback),
                                Icon = MaterialDesignIcons.Normal.FileFind,
                                Color = MudBlazor.Color.Secondary,
                                Data = $"showfile-{filePath}"
                            })
                        )!,
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_Dependency_UnmetRequirement_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
        }
    }
}
