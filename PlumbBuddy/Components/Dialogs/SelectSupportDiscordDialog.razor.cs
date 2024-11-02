namespace PlumbBuddy.Components.Dialogs;

partial class SelectSupportDiscordDialog
{
    string description = string.Empty;
    IReadOnlyList<(string name, SupportDiscord discord)>? relevantSupportDiscords;

    [Parameter]
    public FileInfo? ErrorFile { get; set; }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public IReadOnlyDictionary<string, SupportDiscord>? SupportDiscords { get; set; }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    [SuppressMessage("Security", "CA5394: Do not use insecure randomness", Justification = "I'm sure cryptographic strength is really important for shuffling the Discords. 🤦")]
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (SupportDiscords is not { } supportDiscords)
            return;
        var discordsToUse = supportDiscords
#if MACCATALYST
            .Where(kv => !kv.Value.NoMacSupport);
#elif WINDOWS
            .Where(kv => !kv.Value.NoWindowsSupport);
#else
            .Where(kv => true);
#endif
        if (ErrorFile is { } errorFile)
        {
            description = $"Here are the Community Support Discord servers I could find to help us with: `{errorFile.Name}`";
            var discordsToUseWeightedList = discordsToUse
                .Where(kv => kv.Value.TextFileSubmissionSteps.Count is > 0)
                .Select(kv => (name: kv.Key, discord: kv.Value, recommendationWeight: kv.Value.SupportedTextFilePatterns.Sum(textFilePattern => Regex.IsMatch(errorFile.Name, textFilePattern.Pattern, textFilePattern.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) ? 1D + textFilePattern.AdditionalRecommendationWeight : 0D)))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(discordsToUseWeightedList)); // shuffle before ordering to ensure that Discords with identical weight get an equal chance to be listed first
            relevantSupportDiscords = discordsToUseWeightedList
                .Where(t => t.recommendationWeight is > 0D)
                .OrderByDescending(t => t.recommendationWeight)
                .Select(t => (t.name, t.discord))
                .ToImmutableArray();
        }
        else
        {
            description = "Here are the Community Support Discord servers prepared to offer general support. If you're looking for help with *me* instead of with the game or with a mod, [click here](https://plumbbuddy.app/redirect?to=PlumbBuddyDiscord).";
            var discordsToUseList = discordsToUse
                .Where(kv => kv.Value.AskForHelpSteps.Count is > 0)
                .Select(kv => (name: kv.Key, discord: kv.Value))
                .ToList();
            Random.Shared.Shuffle(CollectionsMarshal.AsSpan(discordsToUseList));
            relevantSupportDiscords = discordsToUseList.ToImmutableArray();
        }
        StateHasChanged();
    }

    void SelectSupportDiscord(string supportDiscordName) =>
        MudDialog?.Close(DialogResult.Ok(supportDiscordName));
}
