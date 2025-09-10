namespace PlumbBuddy.Services;

abstract partial class ElectronicArtsApp :
    IElectronicArtsApp
{
    [GeneratedRegex(@"^.*offerId=\[(?<offerId>.*)\] slug=\[the-sims-4\].*$", RegexOptions.Multiline)]
    private static partial Regex GetLogEntryContainingTS4OfferIdPattern();

    [GeneratedRegex(@"^user\.gamecommandline\.(?<offerId>.*)$")]
    private static partial Regex GetUserIniFileCommandLineArgumentsKeyPattern();

    protected ElectronicArtsApp(ILogger<IElectronicArtsApp> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    ImmutableHashSet<string>? cachedOfferIds;
    protected readonly ILogger<IElectronicArtsApp> logger;

    /// <inheritdoc/>
    public abstract Task<DirectoryInfo?> GetElectronicArtsUserDataDirectoryAsync();

    /// <inheritdoc/>
    public abstract Task<bool> GetIsElectronicArtsAppInstalledAsync();

    /// <inheritdoc/>
    public async Task<string?> GetTS4ConfiguredCommandLineArgumentsAsync()
    {
        if (await GetTS4ElectronicArtsOfferIdsAsync().ConfigureAwait(false) is not { } offerIds
            || await GetElectronicArtsUserDataDirectoryAsync().ConfigureAwait(false) is not { } userDataDirectory)
            return null;
        var commandLineArgumentsValues = new List<string>();
        foreach (var userIniFile in userDataDirectory.GetFiles("user_*.ini", SearchOption.TopDirectoryOnly))
        {
            var parser = new IniDataParser();
            var data = parser.Parse(await File.ReadAllTextAsync(userIniFile.FullName).ConfigureAwait(false));
            foreach (var keyData in data.Global)
            {
                if (GetUserIniFileCommandLineArgumentsKeyPattern().Match(keyData.KeyName) is not { Success: true } match
                    || !offerIds.Contains(match.Groups["offerId"].Value))
                    continue;
                commandLineArgumentsValues.Add(keyData.Value);
            }
        }
        if (commandLineArgumentsValues.Count is 0)
            return null;
        return commandLineArgumentsValues[0];
    }

    /// <inheritdoc/>
    public async Task<ImmutableHashSet<string>?> GetTS4ElectronicArtsOfferIdsAsync()
    {
        if (cachedOfferIds is not null)
            return cachedOfferIds;
        if (await GetElectronicArtsUserDataDirectoryAsync().ConfigureAwait(false) is not { } userDataDirectory)
            return null;
        var offerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var logsDirectory = new DirectoryInfo(Path.Combine(userDataDirectory.FullName, "Logs"));
        foreach (var logFileOfInterest in logsDirectory.GetFiles("EADesktopVerbose.*", SearchOption.TopDirectoryOnly))
        {
            var ioGrace = 10;
            try
            {
                offerIds.UnionWith
                (
                    GetLogEntryContainingTS4OfferIdPattern()
                    .Matches(await File.ReadAllTextAsync(logFileOfInterest.FullName).ConfigureAwait(false))
                    .Select(match => match.Groups["offerId"].Value)
                );
            }
            catch (IOException) when (--ioGrace > 0)
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "while trying to process {FileName}", logFileOfInterest.FullName);
            }
        }
        cachedOfferIds = offerIds.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        return cachedOfferIds;
    }

    /// <inheritdoc/>
    public abstract Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync();

    public abstract Task LaunchElectronicArtsAppAsync();

    /// <inheritdoc/>
    public abstract Task<bool> QuitElectronicArtsAppAsync();

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "Sorry, the EA App disagrees with you, CA.")]
    public async Task SetTS4ConfiguredCommandLineArgumentsAsync(string? commandLineArguments)
    {
        if (await GetTS4ElectronicArtsOfferIdsAsync().ConfigureAwait(false) is not { } offerIds
            || await GetElectronicArtsUserDataDirectoryAsync().ConfigureAwait(false) is not { } userDataDirectory)
            return;
        var quitEaApp = await QuitElectronicArtsAppAsync().ConfigureAwait(false);
        var isDeleting = commandLineArguments is null;
        foreach (var userIniFile in userDataDirectory.GetFiles("user_*.ini", SearchOption.TopDirectoryOnly))
        {
            var offerIdsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parser = new IniDataParser();
            var data = parser.Parse(await File.ReadAllTextAsync(userIniFile.FullName).ConfigureAwait(false));
            var global = data.Global;
            foreach (var keyData in global.ToImmutableArray())
            {
                if (GetUserIniFileCommandLineArgumentsKeyPattern().Match(keyData.KeyName) is not { Success: true } match)
                    continue;
                var offerId = match.Groups["offerId"].Value;
                if (!offerIds.Contains(offerId))
                    continue;
                offerIdsSet.Add(offerId);
                if (isDeleting)
                {
                    global.RemoveKey(keyData.KeyName);
                    continue;
                }
                keyData.Value = commandLineArguments;
                global.SetKeyData(keyData);
            }
            if (!isDeleting)
                foreach (var offerId in offerIds.Except(offerIdsSet))
                    global[$"user.gamecommandline.{offerId.ToLowerInvariant()}"] = commandLineArguments;
            await File.WriteAllTextAsync(userIniFile.FullName, data.ToString()).ConfigureAwait(false);
        }
        if (quitEaApp)
            await LaunchElectronicArtsAppAsync().ConfigureAwait(false);
    }
}
