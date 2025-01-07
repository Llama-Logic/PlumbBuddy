namespace PlumbBuddy.Services.Scans;

public sealed class CacheStalenessScan :
    Scan,
    ICacheStalenessScan
{
    public CacheStalenessScan(ISettings settings, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        this.settings = settings;
        this.smartSimObserver = smartSimObserver;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
    }

    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly ISettings settings;
    readonly ISmartSimObserver smartSimObserver;

    public override async Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string issueDataStr && issueDataStr is "stale" && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "clear")
            {
                await smartSimObserver.ClearCacheAsync();
                return;
            }
            if (resolutionCmd is "stopTellingMe")
            {
                settings.ScanForCacheStaleness = false;
                return;
            }
        }
    }

    public override IAsyncEnumerable<ScanIssue> ScanAsync() =>
        AsyncEnumerable.Empty<ScanIssue>().Append
        (
            modsDirectoryCataloger.State is ModsDirectoryCatalogerState.AnalyzingTopology
            ?
            new()
            {
                Icon = MaterialDesignIcons.Normal.SelectCompare,
                Caption = AppText.Scan_CacheStaleness_AwaitingAnalysis_Caption,
                Description = AppText.Scan_CacheStaleness_AwaitingAnalysis_Description,
                Origin = this
            }
            :
            settings.CacheStatus switch
            {
                SmartSimCacheStatus.Stale => new()
                {
                    Icon = MaterialDesignIcons.Normal.FridgeAlert,
                    Caption = AppText.Scan_CacheStaleness_Stale_Caption,
                    Description = settings.Type is UserType.Casual
                        ? AppText.Scan_CacheStaleness_Stale_Description_Casual
                        : AppText.Scan_CacheStaleness_Stale_Description_NonCasual,
                    Origin = this,
                    Data = "stale",
                    Type = ScanIssueType.Sick,
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Eraser,
                            Label = AppText.Scan_CacheStaleness_Stale_Clear_Label,
                            Color = MudBlazor.Color.Primary,
                            Data = "clear"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = AppText.Scan_Common_StopTellingMe_Label,
                            CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                            CautionText = AppText.Scan_CacheStaleness_Stale_StopTellingMe_CautionText,
                            Data = "stopTellingMe"
                        }
                    ]
                },
                SmartSimCacheStatus.Normal => new()
                {
                    Icon = MaterialDesignIcons.Normal.Fridge,
                    Caption = AppText.Scan_CacheStaleness_Fine_Caption,
                    Description = settings.Type is UserType.Casual
                        ? AppText.Scan_CacheStaleness_Fine_Description_Casual
                        : AppText.Scan_CacheStaleness_Fine_Description_NonCasual,
                    Origin = this
                },
                _ => new()
                {
                    Icon = MaterialDesignIcons.Outline.Fridge,
                    Caption = AppText.Scan_CacheStaleness_Clear_Caption,
                    Description = settings.Type is UserType.Casual
                        ? AppText.Scan_CacheStaleness_Clear_Description_Casual
                        : AppText.Scan_CacheStaleness_Clear_Description_NonCasual,
                    Origin = this
                }
            }
        );
}
