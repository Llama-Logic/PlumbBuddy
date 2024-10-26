namespace PlumbBuddy.Services.Scans;

public sealed class CacheStalenessScan :
    Scan,
    ICacheStalenessScan
{
    public CacheStalenessScan(IPlayer player, ISmartSimObserver smartSimObserver)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        this.player = player;
        this.smartSimObserver = smartSimObserver;
    }

    readonly IPlayer player;
    readonly ISmartSimObserver smartSimObserver;

    public override Task ResolveIssueAsync(ILifetimeScope interfaceLifetimeScope, object issueData, object resolutionData)
    {
        if (issueData is string issueDataStr && issueDataStr is "stale" && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "clear")
            {
                smartSimObserver.ClearCache();
                return Task.CompletedTask;
            }
            if (resolutionCmd is "stopTellingMe")
            {
                player.ScanForCacheStaleness = false;
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public override IAsyncEnumerable<ScanIssue> ScanAsync() =>
        AsyncEnumerable.Empty<ScanIssue>().Append(player.CacheStatus switch
        {
            SmartSimCacheStatus.Stale => new()
            {
                Icon = MaterialDesignIcons.Normal.FridgeAlert,
                Caption = "The Cache is Stale",
                Description = player.Type is UserType.Casual
                    ? "Uhh, these cache files might be like the kids from that Offspring song — as in \"not alright\". We need to see to this and soon."
                    : "The cache may contain resources which are no longer being loaded because they are no longer the victors of override conflicts or their mods have been replaced or removed.",
                Origin = this,
                Data = "stale",
                Type = ScanIssueType.Sick,
                Resolutions =
                [
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Eraser,
                        Label = "Clear the cache",
                        Color = MudBlazor.Color.Primary,
                        Data = "clear"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = "Umm, not clearing the cache files when they may have old junk hanging around in them can cause weird problems. Disabling this scan will make this warning go away, but it won't protect you from those problems.",
                        Data = "stopTellingMe"
                    }
                ]
            },
            SmartSimCacheStatus.Normal => new()
            {
                Icon = MaterialDesignIcons.Normal.Fridge,
                Caption = "The Cache is Fine",
                Description = player.Type is UserType.Casual
                    ? "There are cache files at the moment. They look fine to me, as cache files go. A little ordinary, but that's what you want in cache files!"
                    : "The cache files exist on disk in a normal state.",
                Origin = this
            },
            _ => new()
            {
                Icon = MaterialDesignIcons.Outline.Fridge,
                Caption = "The Cache is Clear",
                Description = player.Type is UserType.Casual
                    ? "There are no cache files at the moment. I mean... that's fine. The game's gonna load a little slower next time, but it's okay."
                    : "There are currently no cache files on disk.",
                Origin = this
            }
        });
}
