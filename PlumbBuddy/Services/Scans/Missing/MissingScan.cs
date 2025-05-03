namespace PlumbBuddy.Services.Scans.Missing;

public abstract class MissingScan :
    Scan,
    IMissingScan
{
    protected MissingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(settings);
        this.pbDbContextFactory = pbDbContextFactory;
        this.settings = settings;
    }

    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly ISettings settings;

    protected abstract string ModName { get; }

    protected abstract Uri ModUrl { get; }

    protected abstract string ModUtility { get; }

    protected abstract IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; }

    protected abstract IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; }

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string issueDataStr && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "enable")
            {
                if (issueDataStr is "modsGameOptionScanDisabled")
                    settings.ScanForModsDisabled = true;
                else if (issueDataStr is "scriptModsGameOptionScanDisabled")
                    settings.ScanForScriptModsDisabled = true;
                else if (issueDataStr is "packageDepthScanDisabled")
                    settings.ScanForInvalidModSubdirectoryDepth = true;
                else if (issueDataStr is "ts4scriptDepthScanDisabled")
                    settings.ScanForInvalidScriptModSubdirectoryDepth = true;
                return Task.CompletedTask;
            }
            if (resolutionCmd is "stopTellingMe")
            {
                StopScanning(settings);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        if (RequiredPackageKeys?.Count > 0 && !settings.ScanForModsDisabled)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = string.Format(AppText.Scan_Missing_MayNotLoad_Caption, ModName),
                Description = string.Format(AppText.Scan_Missing_MayNotLoad_Description, AppText.Scan_Missing_MayNotLoad_Scan_ModsGameOption, ModName),
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "modsGameOptionScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = string.Format(AppText.Scan_Missing_MayNotLoad_EnableScan_Label, AppText.Scan_Missing_MayNotLoad_Scan_ModsGameOption),
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = string.Format(AppText.Scan_Missing_MayNotLoad_StopTellingMe_CautionText, ModName, ModUtility),
                        Data = "stopTellingMe"
                    }
                ]
            };
        else if (RequiredScriptArchiveFullNames?.Count > 0 && !settings.ScanForScriptModsDisabled)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = string.Format(AppText.Scan_Missing_MayNotLoad_Caption, ModName),
                Description = string.Format(AppText.Scan_Missing_MayNotLoad_Description, AppText.Scan_Missing_MayNotLoad_Scan_ScriptModsGameOption, ModName),
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "scriptModsGameOptionScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = string.Format(AppText.Scan_Missing_MayNotLoad_EnableScan_Label, AppText.Scan_Missing_MayNotLoad_Scan_ScriptModsGameOption),
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = string.Format(AppText.Scan_Missing_MayNotLoad_StopTellingMe_CautionText, ModName, ModUtility),
                        Data = "stopTellingMe"
                    }
                ]
            };
        else if (RequiredPackageKeys?.Count > 0 && !settings.ScanForInvalidModSubdirectoryDepth)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = string.Format(AppText.Scan_Missing_MayNotLoad_Caption, ModName),
                Description = string.Format(AppText.Scan_Missing_MayNotLoad_Description, AppText.Scan_Missing_MayNotLoad_Scan_PackageDepthScan, ModName),
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "packageDepthScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = string.Format(AppText.Scan_Missing_MayNotLoad_EnableScan_Label, AppText.Scan_Missing_MayNotLoad_Scan_PackageDepthScan),
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = string.Format(AppText.Scan_Missing_MayNotLoad_StopTellingMe_CautionText, ModName, ModUtility),
                        Data = "stopTellingMe"
                    }
                ]
            };
        else if (RequiredScriptArchiveFullNames?.Count > 0 && !settings.ScanForInvalidScriptModSubdirectoryDepth)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = string.Format(AppText.Scan_Missing_MayNotLoad_Caption, ModName),
                Description = string.Format(AppText.Scan_Missing_MayNotLoad_Description, AppText.Scan_Missing_MayNotLoad_Scan_Ts4ScriptDepthScan, ModName),
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "ts4scriptDepthScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = string.Format(AppText.Scan_Missing_MayNotLoad_EnableScan_Label, AppText.Scan_Missing_MayNotLoad_Scan_Ts4ScriptDepthScan),
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = string.Format(AppText.Scan_Missing_MayNotLoad_StopTellingMe_CautionText, ModName, ModUtility),
                        Data = "stopTellingMe"
                    }
                ]
            };
        else
        {
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var requiredElementMissing = false;
            if (RequiredPackageKeys is { } keys)
                foreach (var key in keys)
                {
                    var signedType = unchecked((int)(uint)key.Type);
                    var signedGroup = unchecked((int)key.Group);
                    var signedFullInstance = unchecked((long)key.FullInstance);
                    if (!await pbDbContext.ModFileResources.AnyAsync(mfr => mfr.KeyType == signedType && mfr.KeyGroup == signedGroup && mfr.KeyFullInstance == signedFullInstance && mfr.ModFileHash.ModFiles!.Any(mf => mf.Path != null)).ConfigureAwait(false))
                    {
                        requiredElementMissing = true;
                        break;
                    }
                }
            if (!requiredElementMissing && RequiredScriptArchiveFullNames is { } fullNames)
                foreach (var fullName in fullNames)
                {
                    if (!await pbDbContext.ScriptModArchiveEntries.AnyAsync(smae => smae.FullName == fullName && smae.ModFileHash.ModFiles.Any(mf => mf.Path != null)).ConfigureAwait(false))
                    {
                        requiredElementMissing = true;
                        break;
                    }
                }
            if (requiredElementMissing)
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantClosedPlus,
                    Caption = string.Format(AppText.Scan_Missing_Missing_Caption, ModName),
                    Description = string.Format(AppText.Scan_Missing_Missing_Description, ModUtility),
                    Origin = this,
                    Type = ScanIssueType.Uncomfortable,
                    Data = "missing",
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Web,
                            Label = string.Format(AppText.Scan_Missing_Missing_GoGet_Label, ModName),
                            Color = MudBlazor.Color.Primary,
                            Data = "get",
                            Url = ModUrl
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = AppText.Scan_Common_StopTellingMe_Label,
                            CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                            CautionText = string.Format(AppText.Scan_Missing_Missing_StopTellingMe_CautionText, ModName, ModUtility),
                            Data = "stopTellingMe"
                        }
                    ]
                };
            else
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck,
                    Caption = string.Format(AppText.Scan_Missing_Installed_Caption, ModName),
                    Description = string.Format(AppText.Scan_Missing_Installed_Description, ModUtility),
                    Origin = this,
                    Type = ScanIssueType.Healthy
                };
        }
    }

    protected abstract void StopScanning(ISettings settings);
}
