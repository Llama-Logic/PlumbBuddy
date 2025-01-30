namespace PlumbBuddy.Components.Dialogs;

partial class SelectSupportDiscordDialog
{
    string description = string.Empty;
    IReadOnlyList<(string name, string creatorName, SupportDiscord discord)>? relevantSupportDiscords;
    bool showAppSupport;

    [Parameter]
    public FileInfo? ErrorFile { get; set; }

    [Parameter]
    public IReadOnlyList<string>? ForCreators { get; set; }

    [Parameter]
    public string? ForManifestHashHex { get; set; }

    [Parameter]
    public bool IsPatchDay { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public IReadOnlyDictionary<string, SupportDiscord>? SupportDiscords { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void GetHelpWithMeOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok((discord: "PlumbBuddy", creator: string.Empty)));

    [SuppressMessage("Security", "CA5394: Do not use insecure randomness", Justification = "I'm sure cryptographic strength is really important for shuffling the Discords. 🤦")]
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (SupportDiscords is not { } supportDiscords)
            return;
        var discordsToUse = supportDiscords
#if MACCATALYST
            .Where(kv => kv.Key is not "PlumbBuddy" && !kv.Value.NoMacSupport);
#elif WINDOWS
            .Where(kv => kv.Key is not "PlumbBuddy" && !kv.Value.NoWindowsSupport);
#else
            .Where(kv => true);
#endif
        if (ForCreators is { } forCreators && ForManifestHashHex is { } forManifestHashHex)
        {
            var forCreatorsHashSet = forCreators.ToImmutableHashSet();
            var forManifestHash = forManifestHashHex.ToByteSequence().ToImmutableArray();
            description = AppText.SelectSupportDiscordDialog_Description_ModSpecific;
            var specificDiscordsToUseList = discordsToUse
                .Where(kv => kv.Value.SpecificCreators.Any(sc => forCreatorsHashSet.Contains(sc.Key) && !sc.Value.ExceptForHashes.Any(efh => efh.SequenceEqual(forManifestHash))))
                .Select(kv => (name: kv.Key, creator: kv.Value.SpecificCreators.First(sc => forCreatorsHashSet.Contains(sc.Key) && !sc.Value.ExceptForHashes.Any(efh => efh.SequenceEqual(forManifestHash))).Key, discord: kv.Value))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(specificDiscordsToUseList));
            var discordsToUseList = discordsToUse
                .Where(kv => kv.Value.AskForHelpSteps.Count is > 0)
                .Select(kv => (name: kv.Key, creator: string.Empty, discord: kv.Value))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(discordsToUseList));
            relevantSupportDiscords = specificDiscordsToUseList
                .Concat(discordsToUseList)
                .DistinctBy(discord => discord.name)
                .ToImmutableArray();
        }
        else if (ErrorFile is { } errorFile)
        {
            description = string.Format(AppText.SelectSupportDiscordDialog_Description_ErrorLog, errorFile.Name);
            var discordsToUseWeightedList = discordsToUse
                .Where(kv => kv.Value.TextFileSubmissionSteps.Count is > 0)
                .Select(kv => (name: kv.Key, discord: kv.Value, recommendationWeight: kv.Value.SupportedTextFilePatterns.Sum(textFilePattern => Regex.IsMatch(errorFile.Name, textFilePattern.Pattern, textFilePattern.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) ? 1D + textFilePattern.AdditionalRecommendationWeight : 0D)))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(discordsToUseWeightedList)); // shuffle before ordering to ensure that Discords with identical weight get an equal chance to be listed first
            relevantSupportDiscords = discordsToUseWeightedList
                .Where(t => t.recommendationWeight is > 0D)
                .OrderByDescending(t => t.recommendationWeight)
                .Select(t => (t.name, string.Empty, t.discord))
                .ToImmutableArray();
        }
        else if (IsPatchDay)
        {
            description = AppText.SelectSupportDiscordDialog_Description_ModUpdates;
            var discordsToUseList = discordsToUse
                .Where(kv => kv.Value.PatchDayHelpSteps.Count is > 0)
                .Select(kv => (name: kv.Key, string.Empty, discord: kv.Value))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(discordsToUseList));
            relevantSupportDiscords = discordsToUseList.ToImmutableArray();
        }
        else
        {
            showAppSupport = true;
            description = AppText.SelectSupportDiscordDialog_Description_General;
            var discordsToUseList = discordsToUse
                .Where(kv => kv.Value.AskForHelpSteps.Count is > 0)
                .Select(kv => (name: kv.Key, string.Empty, discord: kv.Value))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(discordsToUseList));
            relevantSupportDiscords = discordsToUseList.ToImmutableArray();
        }
        StateHasChanged();
    }

    void SelectSupportDiscord(string supportDiscordName, string creatorName) =>
        MudDialog?.Close(DialogResult.Ok((discord: supportDiscordName, creator: creatorName)));
}
