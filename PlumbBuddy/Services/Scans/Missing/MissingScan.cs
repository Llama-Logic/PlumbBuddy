namespace PlumbBuddy.Services.Scans.Missing;

public abstract class MissingScan :
    Scan,
    IMissingScan
{
    public MissingScan(PbDbContext pbDbContext, IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(player);
        this.pbDbContext = pbDbContext;
        this.player = player;
    }

    readonly PbDbContext pbDbContext;
    readonly IPlayer player;

    protected abstract string ModName { get; }

    protected abstract Uri ModUrl { get; }

    protected abstract string ModUtility { get; }

    protected abstract IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; }

    protected abstract IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; }

    public override Task ResolveIssueAsync(ILifetimeScope interfaceLifetimeScope, object issueData, object resolutionData)
    {
        if (issueData is string issueDataStr && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "enable")
            {
                if (issueDataStr is "modsGameOptionScanDisabled")
                    player.ScanForModsDisabled = true;
                else if (issueDataStr is "scriptModsGameOptionScanDisabled")
                    player.ScanForScriptModsDisabled = true;
                else if (issueDataStr is "packageDepthScanDisabled")
                    player.ScanForInvalidModSubdirectoryDepth = true;
                else if (issueDataStr is "ts4scriptDepthScanDisabled")
                    player.ScanForInvalidScriptModSubdirectoryDepth = true;
                return Task.CompletedTask;
            }
            if (resolutionCmd is "stopTellingMe")
            {
                StopScanning(player);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        if (RequiredPackageKeys?.Count > 0 && !player.ScanForModsDisabled)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = $"{ModName} May Not Load",
                Description = $"Because the <strong>Mods Game Option</strong> scan is disabled, I can't be sure {ModName} would be loaded if *you did* have it installed. Therefore, we need to turn that scan back on.",
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "modsGameOptionScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = "Enable Mods Game Option Scan",
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = $"{ModName} can be quite helpful with {ModUtility}. Turning off this scan will prevent me from getting on your case about not having it, but... you know... you may not have it.",
                        Data = "stopTellingMe"
                    }
                ]
            };
        else if (RequiredScriptArchiveFullNames?.Count > 0 && !player.ScanForScriptModsDisabled)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = $"{ModName} May Not Load",
                Description = $"Because the <strong>Script Mods Game Option</strong> scan is disabled, I can't be sure {ModName} would be loaded if *you did* have it installed. Therefore, we need to turn that scan back on.",
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "scriptModsGameOptionScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = "Enable Script Mods Game Option Scan",
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = $"{ModName} can be quite helpful with {ModUtility}. Turning off this scan will prevent me from getting on your case about not having it, but... you know... you may not have it.",
                        Data = "stopTellingMe"
                    }
                ]
            };
        else if (RequiredPackageKeys?.Count > 0 && !player.ScanForInvalidModSubdirectoryDepth)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = $"{ModName} May Not Load",
                Description = $"Because the <strong>`.package` Depth Scan</strong> scan is disabled, I can't be sure {ModName} would be loaded if *you did* have it installed. Therefore, we need to turn that scan back on.",
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "packageDepthScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = "Enable Package Depth Scan",
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = $"{ModName} can be quite helpful with {ModUtility}. Turning off this scan will prevent me from getting on your case about not having it, but... you know... you may not have it.",
                        Data = "stopTellingMe"
                    }
                ]
            };
        else if (RequiredScriptArchiveFullNames?.Count > 0 && !player.ScanForInvalidScriptModSubdirectoryDepth)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.CubeScan,
                Caption = $"{ModName} May Not Load",
                Description = $"Because the <strong>`.ts4script` Depth Scan</strong> scan is disabled, I can't be sure {ModName} would be loaded if *you did* have it installed. Therefore, we need to turn that scan back on.",
                Origin = this,
                Type = ScanIssueType.Sick,
                Data = "ts4scriptDepthScanDisabled",
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.ToggleSwitch,
                        Label = "Enable TS4Script Depth Scan",
                        Color = MudBlazor.Color.Primary,
                        Data = "enable"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = $"{ModName} can be quite helpful with {ModUtility}. Turning off this scan will prevent me from getting on your case about not having it, but... you know... you may not have it.",
                        Data = "stopTellingMe"
                    }
                ]
            };
        else
        {
            var requiredElementMissing = false;
            if (RequiredPackageKeys is { } keys)
                foreach (var key in keys)
                {
                    var signedType = unchecked((int)(uint)key.Type);
                    var signedGroup = unchecked((int)key.Group);
                    var signedFullInstance = unchecked((long)key.FullInstance);
                    if (!await pbDbContext.ModFileResources.AnyAsync(mfr => mfr.KeyType == signedType && mfr.KeyGroup == signedGroup && mfr.KeyFullInstance == signedFullInstance && mfr.ModFileHash!.ModFiles!.Any(mf => mf.Path != null)).ConfigureAwait(false))
                    {
                        requiredElementMissing = true;
                        break;
                    }
                }
            if (!requiredElementMissing && RequiredScriptArchiveFullNames is { } fullNames)
                foreach (var fullName in fullNames)
                {
                    if (!await pbDbContext.ScriptModArchiveEntries.AnyAsync(smae => smae.FullName == fullName && smae.ModFileHash!.ModFiles!.Any(mf => mf.Path != null)).ConfigureAwait(false))
                    {
                        requiredElementMissing = true;
                        break;
                    }
                }
            if (requiredElementMissing)
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantClosedPlus,
                    Caption = $"{ModName} is Missing",
                    Description = $"You should totally go get it, though, because it does a great deal to improve {ModUtility}.",
                    Origin = this,
                    Type = ScanIssueType.Uncomfortable,
                    Data = "missing",
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Launch,
                            Label = $"Go Get {ModName}",
                            Color = MudBlazor.Color.Primary,
                            Data = "get",
                            Url = ModUrl
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = "Stop telling me",
                            CautionCaption = "Disable this scan?",
                            CautionText = $"Turning off this scan will prevent me from getting on your case about not having {ModName}, but... you know... you will still miss out on the awesome improvements it makes to {ModUtility}.",
                            Data = "stopTellingMe"
                        }
                    ]
                };
            else
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck,
                    Caption = $"{ModName} is Installed",
                    Description = $"Enjoy the awesome improvements to {ModUtility}. 😁",
                    Origin = this,
                    Type = ScanIssueType.Healthy
                };
        }
    }

    protected abstract void StopScanning(IPlayer player);
}
