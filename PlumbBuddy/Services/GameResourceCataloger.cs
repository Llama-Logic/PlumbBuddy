namespace PlumbBuddy.Services;

public partial class GameResourceCataloger :
    IGameResourceCataloger
{
    [GeneratedRegex(@"^[EFGS]P\d{2}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetPackDirectoryNamePattern();

    public GameResourceCataloger(ILogger<GameResourceCataloger> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        scanDebouncer = new(ScanAsync, TimeSpan.FromSeconds(5));
    }

    int packageExaminationsRemaining;
    readonly ILogger<GameResourceCataloger> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly AsyncDebouncer scanDebouncer;
    readonly ISettings settings;

    public int PackageExaminationsRemaining
    {
        get => packageExaminationsRemaining;
        private set
        {
            if (packageExaminationsRemaining == value)
                return;
            packageExaminationsRemaining = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task ScanAsync()
    {
        try
        {
            await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
            PackageExaminationsRemaining = -1;
            var installationDirectory = new DirectoryInfo(settings.InstallationFolderPath);
            var resourcePackagesOnDisk = new Dictionary<string, (FileInfo file, bool isDelta)>(platformFunctions.FileSystemStringComparer);
            foreach (var clientPackageFile in platformFunctions.GetClientDirectory(installationDirectory).GetFiles("*.package", SearchOption.TopDirectoryOnly))
                resourcePackagesOnDisk.Add(clientPackageFile.FullName, (clientPackageFile, clientPackageFile.Name.Contains("Delta", StringComparison.OrdinalIgnoreCase)));
            foreach (var deltaPackageFile in platformFunctions.GetDeltaDirectory(installationDirectory).GetFiles("*.package", SearchOption.AllDirectories))
                resourcePackagesOnDisk.Add(deltaPackageFile.FullName, (deltaPackageFile, true));
            foreach (var packDirectory in platformFunctions.GetPacksDirectory(installationDirectory).GetDirectories("*.*", SearchOption.TopDirectoryOnly))
                if (GetPackDirectoryNamePattern().IsMatch(packDirectory.Name))
                    foreach (var packPackageFile in packDirectory.GetFiles("*.package", SearchOption.TopDirectoryOnly))
                        resourcePackagesOnDisk.Add(packPackageFile.FullName, (packPackageFile, false));
            PackageExaminationsRemaining = resourcePackagesOnDisk.Count;
            await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
            var pbDbConnectionString = string.Empty;
            using (var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false))
            {
                pbDbConnectionString = pbDbContext.Database.GetConnectionString();
                await pbDbContext.GameResourcePackages.Where(grp => !Enumerable.Contains(resourcePackagesOnDisk.Keys, grp.Path)).ExecuteDeleteAsync().ConfigureAwait(false);
            }
            using var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 4));
            await Task.WhenAll(resourcePackagesOnDisk.Values.Select(async tuple =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                var (file, isDelta) = tuple;
                await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
                var connection = new SqliteConnection(pbDbConnectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                using var tx = (SqliteTransaction)await connection.BeginTransactionAsync().ConfigureAwait(false);
                try
                {
                    var packageId = 0L;
                    var packageCreated = DateTimeOffset.MinValue;
                    var packageLastWrite = DateTimeOffset.MinValue;
                    var packageSize = 0L;
                    byte[]? packageSha256 = null;
                    using (var getGameResourcePackageCommand = connection.CreateCommand())
                    {
                        getGameResourcePackageCommand.Transaction = tx;
                        getGameResourcePackageCommand.CommandText = "SELECT Id, Creation, LastWrite, Size, Sha256 FROM GameResourcePackages WHERE Path = @path";
                        getGameResourcePackageCommand.Parameters.AddWithValue("@path", file.FullName);
                        using var reader = await getGameResourcePackageCommand.ExecuteReaderAsync().ConfigureAwait(false);
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            packageId = reader.GetInt64(0);
                            packageCreated = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(1));
                            packageLastWrite = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2));
                            packageSize = reader.GetInt64(3);
                            packageSha256 = (byte[])reader.GetValue(4);
                        }
                    }
                    AsyncLazy<ImmutableArray<byte>> sha256 = new(() => ModFileManifestModel.GetFileSha256HashAsync(file.FullName));
                    if (packageId is not 0)
                    {
                        if (packageCreated == file.CreationTimeUtc.TrimSeconds()
                            && packageLastWrite == file.LastWriteTimeUtc.TrimSeconds()
                            && packageSize == file.Length)
                            return;
                        var wasFileContentsUnchanged = packageSha256 is not null && (await sha256.Task).SequenceEqual(packageSha256);
                        using var updatePackageCommand = connection.CreateCommand();
                        updatePackageCommand.Transaction = tx;
                        if (wasFileContentsUnchanged)
                            updatePackageCommand.CommandText = "UPDATE GameResourcePackages SET Creation = @creation, LastWrite = @lastWrite, Size = @size WHERE Id = @id";
                        else
                        {
                            updatePackageCommand.CommandText = "UPDATE GameResourcePackages SET Creation = @creation, LastWrite = @lastWrite, Size = @size, Sha256 = @sha256 WHERE Id = @id";
                            var sha256ImmutableArray = await sha256.Task.ConfigureAwait(false);
                            updatePackageCommand.Parameters.AddWithValue("@sha256", Unsafe.As<ImmutableArray<byte>, byte[]>(ref sha256ImmutableArray));
                        }
                        updatePackageCommand.Parameters.AddWithValue("@creation", ((DateTimeOffset)file.CreationTimeUtc).ToUnixTimeSeconds());
                        updatePackageCommand.Parameters.AddWithValue("@lastWrite", ((DateTimeOffset)file.LastWriteTimeUtc).ToUnixTimeSeconds());
                        updatePackageCommand.Parameters.AddWithValue("@size", file.Length);
                        updatePackageCommand.Parameters.AddWithValue("@id", packageId);
                        await updatePackageCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        if (wasFileContentsUnchanged)
                            return;
                    }
                    else
                    {
                        var sha256ImmutableArray = await sha256.Task.ConfigureAwait(false);
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = tx;
                        insertCommand.CommandText = "INSERT INTO GameResourcePackages (Creation, IsDelta, LastWrite, Path, Sha256, Size) VALUES (@creation, @isDelta, @lastWrite, @path, @sha256, @size); SELECT last_insert_rowid();";
                        insertCommand.Parameters.AddWithValue("@creation", ((DateTimeOffset)file.CreationTimeUtc).ToUnixTimeSeconds());
                        insertCommand.Parameters.AddWithValue("@isDelta", isDelta);
                        insertCommand.Parameters.AddWithValue("@lastWrite", ((DateTimeOffset)file.LastWriteTimeUtc).ToUnixTimeSeconds());
                        insertCommand.Parameters.AddWithValue("@path", file.FullName);
                        insertCommand.Parameters.AddWithValue("@sha256", Unsafe.As<ImmutableArray<byte>, byte[]>(ref sha256ImmutableArray));
                        insertCommand.Parameters.AddWithValue("@size", file.Length);
                        if (await insertCommand.ExecuteScalarAsync().ConfigureAwait(false) is long nonNullLastInsertId)
                            packageId = nonNullLastInsertId;
                        else
                            throw new InvalidOperationException($"failed to retrieve insertion ID for package at path: {file.FullName}");
                    }
                    var catalogedResources = new Dictionary<ResourceKey, long>();
                    using (var getExistingResourceCommand = connection.CreateCommand())
                    {
                        getExistingResourceCommand.Transaction = tx;
                        getExistingResourceCommand.CommandText = "SELECT KeyType, KeyGroup, KeyFullInstance, Id FROM GameResourcePackageResources WHERE GameResourcePackageId = @gameResourcePackageId";
                        getExistingResourceCommand.Parameters.AddWithValue("@gameResourcePackageId", packageId);
                        using var reader = await getExistingResourceCommand.ExecuteReaderAsync().ConfigureAwait(false);
                        while (await reader.ReadAsync().ConfigureAwait(false))
                            catalogedResources.Add(new((ResourceType)unchecked((uint)reader.GetInt32(0)), unchecked((uint)reader.GetInt32(1)), unchecked((ulong)reader.GetInt64(2))), reader.GetInt64(3));
                    }
                    using var dbpf = await DataBasePackedFile.FromPathAsync(file.FullName).ConfigureAwait(false);
                    var resourceKeys = (await dbpf.GetKeysAsync().ConfigureAwait(false)).ToImmutableHashSet();
                    var noLongerExistingKeys = catalogedResources.Keys.Except(resourceKeys);
                    if (!noLongerExistingKeys.Any())
                        foreach (var noLongerExistingKeysChunk in noLongerExistingKeys.Chunk(800))
                        {
                            using var deleteResourcesCommand = connection.CreateCommand();
                            deleteResourcesCommand.Transaction = tx;
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                            deleteResourcesCommand.CommandText = $"DELETE FROM GameResourcePackageResources WHERE Id IN ({string.Join(", ", noLongerExistingKeysChunk.Select((key, index) =>
                            {
                                var paramName = $"k{index}";
                                deleteResourcesCommand.Parameters.AddWithValue(paramName, catalogedResources[key]);
                                return paramName;
                            }))}";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                            await deleteResourcesCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    foreach (var resourceKeysChunk in resourceKeys.Chunk(160))
                    {
                        using var replaceIntoCommand = connection.CreateCommand();
                        replaceIntoCommand.Transaction = tx;
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                        replaceIntoCommand.CommandText = $"INSERT INTO GameResourcePackageResources (GameResourcePackageId, KeyType, KeyGroup, KeyFullInstance, StringTableLocalePrefix) VALUES {string.Join(", ", resourceKeysChunk.Select((key, index) =>
                        {
                            replaceIntoCommand.Parameters.AddWithValue($"@p{index}", packageId);
                            replaceIntoCommand.Parameters.AddWithValue($"@t{index}", unchecked((int)(uint)key.Type));
                            replaceIntoCommand.Parameters.AddWithValue($"@g{index}", unchecked((int)key.Group));
                            replaceIntoCommand.Parameters.AddWithValue($"@i{index}", unchecked((long)key.FullInstance));
                            replaceIntoCommand.Parameters.AddWithValue($"@s{index}", key.Type is ResourceType.StringTable ? (byte)((key.FullInstance & 0xff00000000000000) >> 56) : (byte)0);
                            return $"(@p{index}, @t{index}, @g{index}, @i{index}, @s{index})";
                        }))} ON CONFLICT DO NOTHING";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                        await replaceIntoCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                    var stblResourceKeys = (await dbpf.GetKeysAsync().ConfigureAwait(false)).Where(key => key.Type is ResourceType.StringTable).ToImmutableHashSet();
                    foreach (var stblResourceKey in stblResourceKeys)
                    {
                        var stlbResourceId = 0L;
                        using (var getStblResourceIdCommand = connection.CreateCommand())
                        {
                            getStblResourceIdCommand.CommandText = "SELECT Id From GameResourcePackageResources WHERE GameResourcePackageId = @gameResourcePackageId AND KeyType = @keyType AND KeyGroup = @keyGroup AND KeyFullInstance = @keyFullInstance";
                            getStblResourceIdCommand.Transaction = tx;
                            getStblResourceIdCommand.Parameters.AddWithValue("@gameResourcePackageId", packageId);
                            getStblResourceIdCommand.Parameters.AddWithValue("@keyType", unchecked((int)(uint)stblResourceKey.Type));
                            getStblResourceIdCommand.Parameters.AddWithValue("@keyGroup", unchecked((int)stblResourceKey.Group));
                            getStblResourceIdCommand.Parameters.AddWithValue("@keyFullInstance", unchecked((long)stblResourceKey.FullInstance));
                            if (await getStblResourceIdCommand.ExecuteScalarAsync().ConfigureAwait(false) is not long nonNullStblResourceId)
                                continue;
                            stlbResourceId = nonNullStblResourceId;
                        }
                        var stbl = await dbpf.GetStringTableAsync(stblResourceKey).ConfigureAwait(false);
                        var keys = stbl.KeyHashes.ToImmutableHashSet();
                        foreach (var keyHashesChunk in stbl.KeyHashes.Chunk(400))
                        {
                            using var replaceIntoCommand = connection.CreateCommand();
                            replaceIntoCommand.Transaction = tx;
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                            replaceIntoCommand.CommandText = $"INSERT INTO GameStringTableEntries (GameResourcePackageResourceId, SignedKey) VALUES {string.Join(", ", keyHashesChunk.Select((key, index) =>
                            {
                                replaceIntoCommand.Parameters.AddWithValue($"@r{index}", stlbResourceId);
                                replaceIntoCommand.Parameters.AddWithValue($"@k{index}", unchecked((int)key));
                                return $"(@r{index}, @k{index})";
                            }))} ON CONFLICT DO NOTHING";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                            await replaceIntoCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    await tx.CommitAsync().ConfigureAwait(false);
                    Interlocked.Decrement(ref packageExaminationsRemaining);
                    OnPropertyChanged(nameof(PackageExaminationsRemaining));
                    semaphore.Release();
                }
            })).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unhandled exception");
        }
        finally
        {
            PackageExaminationsRemaining = 0;
        }
    }

    public void ScanSoon() =>
        scanDebouncer.Execute();
}
