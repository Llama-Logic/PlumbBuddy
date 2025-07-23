namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public partial class GameResourceCataloger :
    IGameResourceCataloger
{
    record EffectiveLocalizedStringRecord(int SignedKey, byte Locale, string PackagePath, int KeyType, int KeyGroup, long KeyFullInstance);
    record EffectiveResourceLocationRecord(string PackagePath, int KeyType, int KeyGroup, long KeyFullInstance);

    [GeneratedRegex(@"^[EFGS]P\d{2}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetPackDirectoryNamePattern();

    static readonly ImmutableDictionary<ResourceType, ResourceFileType?> resourceFileTypeByResourceType = Enum
        .GetValues<ResourceType>()
        .ToImmutableDictionary(resourceType => resourceType, resourceType => typeof(ResourceType).GetMember(resourceType.ToString()).FirstOrDefault()?.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType);
    static readonly ImmutableHashSet<ResourceType> typesCataloged = [ResourceType.DSTImage, ResourceType.StringTable];

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

    public async Task<ReadOnlyMemory<byte>> GetDirectDrawSurfaceAsPngAsync(ResourceKey key)
    {
        if (resourceFileTypeByResourceType.TryGetValue(key.Type, out var resourceFileType)
            && resourceFileType is ResourceFileType.DirectDrawSurfaceAsPortableNetworkGraphic)
            key = new(ResourceType.DSTImage, key.Group, key.FullInstance);
        return await DirectDrawSurface.GetPngDataFromDiffuseSurfaceTextureDataAsync(await GetRawResourceAsync(key).ConfigureAwait(false)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(byte locale, uint locKey, string value)>> GetLocalizedStringsAsync(IEnumerable<uint> locKeys, IEnumerable<byte> locales)
    {
        var result = new List<(byte locale, uint locKey, string value)>();
        if (locKeys.Any())
        {
            var queryParts = new List<string>()
            {
                $"""
                WITH AllStringTableEntries AS (
                	SELECT
                		2 Classification,
                		mf.Path PackagePath,
                		(KeyFullInstance & 0xff00000000000000) >> 56 Locale,
                		mfr.KeyType,
                		mfr.KeyGroup,
                		mfr.KeyFullInstance,
                		mfste.SignedKey
                	FROM
                		ModFileStringTableEntries mfste
                		JOIN ModFileResources mfr ON mfr.Id = mfste.ModFileResourceId
                		JOIN ModFileHashes mfh ON mfh.Id = mfr.ModFileHashId
                		JOIN ModFiles mf ON mf.ModFileHashId = mfh.Id
                	WHERE
                		mf.Path IS NOT NULL
                		AND mf.FileType = 1
                	UNION
                	SELECT
                		grp.IsDelta Classification,
                		grp.Path PackagePath,
                		(KeyFullInstance & 0xff00000000000000) >> 56 Locale,
                		grpr.KeyType,
                		grpr.KeyGroup,
                		grpr.KeyFullInstance,
                		gste.SignedKey
                	FROM
                		GameResourcePackages grp
                		JOIN GameResourcePackageResources grpr ON grpr.GameResourcePackageId = grp.Id
                		JOIN GameStringTableEntries gste ON gste.GameResourcePackageResourceId = grpr.Id
                	ORDER BY
                		1 DESC,
                		2 COLLATE {platformFunctions.FileSystemSQliteCollation}
                )
                SELECT DISTINCT
                	SignedKey,
                	Locale,
                	FIRST_VALUE(PackagePath) OVER (PARTITION BY Locale, SignedKey) PackagePath,
                	FIRST_VALUE(KeyType) OVER (PARTITION BY Locale, SignedKey) KeyType,
                	FIRST_VALUE(KeyGroup) OVER (PARTITION BY Locale, SignedKey) KeyGroup,
                	FIRST_VALUE(KeyFullInstance) OVER (PARTITION BY Locale, SignedKey) KeyFullInstance
                FROM
                	AllStringTableEntries
                WHERE
                    SignedKey IN ({string.Join(", ", locKeys.Select(locKey => unchecked((int)locKey)))})
                """
            };
            if (locales.Any())
                queryParts.Add
                (
                    $"""
                        AND Locale IN ({string.Join(", ", locales)})
                    """
                );
            var lastPackagePath = string.Empty;
            DataBasePackedFile? dbpf = null;
            ResourceKey lastStblKey = default;
            StringTableModel? stbl = null;
            try
            {
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await foreach (var (signedKey, locale, packagePath, keyType, keyGroup, keyFullInsance) in pbDbContext.Database.SqlQueryRaw<EffectiveLocalizedStringRecord>(string.Join("\n", queryParts)).AsAsyncEnumerable().ConfigureAwait(false))
                {
                    if (lastPackagePath != packagePath)
                    {
                        if (dbpf is not null)
                        {
                            stbl = null;
                            await dbpf.DisposeAsync().ConfigureAwait(false);
                            dbpf = null;
                        }
                        var packageFile = new FileInfo(File.Exists(packagePath) ? packagePath : Path.Combine(settings.UserDataFolderPath, "Mods", packagePath));
                        if (packageFile.Exists)
                            dbpf = await DataBasePackedFile.FromPathAsync(Path.Combine(settings.UserDataFolderPath, "Mods", packagePath)).ConfigureAwait(false);
                        lastPackagePath = packagePath;
                    }
                    var stblKey = new ResourceKey(unchecked((ResourceType)(uint)keyType), unchecked((uint)keyGroup), unchecked((ulong)keyFullInsance));
                    if (dbpf is not null && (stbl is null || stblKey != lastStblKey))
                    {
                        stbl = await dbpf!.GetStringTableAsync(stblKey).ConfigureAwait(false);
                        lastStblKey = stblKey;
                    }
                    var locKey = unchecked((uint)signedKey);
                    if (stbl is not null && stbl[locKey] is { } value && !string.IsNullOrWhiteSpace(value))
                        result.Add((locale, locKey, value));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unhandled exception");
            }
            finally
            {
                if (dbpf is not null)
                    await dbpf.DisposeAsync().ConfigureAwait(false);
            }
        }
        return result.AsReadOnly();
    }

    public async Task<ReadOnlyMemory<byte>> GetRawResourceAsync(ResourceKey key)
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
        await foreach (var (packagePath, _, _, _) in pbDbContext.Database.SqlQueryRaw<EffectiveResourceLocationRecord>
        (
            $"""
            WITH AllResources AS (
            	SELECT
            		2 Classification,
            		mf.Path PackagePath,
            		mfr.KeyType,
            		mfr.KeyGroup,
            		mfr.KeyFullInstance
            	FROM
            		ModFileResources mfr
            		JOIN ModFileHashes mfh ON mfh.Id = mfr.ModFileHashId
            		JOIN ModFiles mf ON mf.ModFileHashId = mfh.Id
            	WHERE
            		mf.Path IS NOT NULL
            		AND mf.FileType = 1
            	UNION
            	SELECT
            		grp.IsDelta Classification,
            		grp.Path PackagePath,
            		grpr.KeyType,
            		grpr.KeyGroup,
            		grpr.KeyFullInstance
            	FROM
            		GameResourcePackages grp
            		JOIN GameResourcePackageResources grpr ON grpr.GameResourcePackageId = grp.Id
            	ORDER BY
            		1 DESC,
            		2 COLLATE NOCASE
            )
            SELECT DISTINCT
            	FIRST_VALUE(PackagePath) OVER (PARTITION BY KeyType, KeyGroup, KeyFullInstance) PackagePath,
            	FIRST_VALUE(KeyType) OVER (PARTITION BY KeyType, KeyGroup, KeyFullInstance) KeyType,
            	FIRST_VALUE(KeyGroup) OVER (PARTITION BY KeyType, KeyGroup, KeyFullInstance) KeyGroup,
            	FIRST_VALUE(KeyFullInstance) OVER (PARTITION BY KeyType, KeyGroup, KeyFullInstance) KeyFullInstance
            FROM
            	AllResources
            WHERE
                KeyType = {unchecked((int)(uint)key.Type)}
                AND KeyGroup = {unchecked((int)key.Group)}
                AND KeyFullInstance = {unchecked((long)key.FullInstance)}
            """
        ).AsAsyncEnumerable().ConfigureAwait(false))
        {
            try
            {
                var packageFile = new FileInfo(File.Exists(packagePath) ? packagePath : Path.Combine(settings.UserDataFolderPath, "Mods", packagePath));
                if (!packageFile.Exists)
                    continue;
                var dbpf = await DataBasePackedFile.FromPathAsync(packageFile.FullName).ConfigureAwait(false);
                return await dbpf.GetAsync(key).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unhandled exception while considering {PackagePath} :: {ResourceKey}", packagePath, key);
                continue;
            }
        }
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection
        return ReadOnlyMemory<byte>.Empty;
    }

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
                try
                {
                    var packageId = 0L;
                    var packageCreated = DateTimeOffset.MinValue;
                    var packageLastWrite = DateTimeOffset.MinValue;
                    var packageSize = 0L;
                    using (var getGameResourcePackageCommand = connection.CreateCommand())
                    {
                        getGameResourcePackageCommand.CommandText = "SELECT Id, Creation, LastWrite, Size FROM GameResourcePackages WHERE Path = @path";
                        getGameResourcePackageCommand.Parameters.AddWithValue("@path", file.FullName);
                        using var reader = await getGameResourcePackageCommand.ExecuteReaderAsync().ConfigureAwait(false);
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            packageId = reader.GetInt64(0);
                            packageCreated = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(1));
                            packageLastWrite = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2));
                            packageSize = reader.GetInt64(3);
                        }
                    }
                    if (packageId is not 0)
                    {
                        if (packageCreated == file.CreationTimeUtc.TrimSeconds()
                            && packageLastWrite == file.LastWriteTimeUtc.TrimSeconds()
                            && packageSize == file.Length)
                            return;
                        using var updatePackageCommand = connection.CreateCommand();
                        updatePackageCommand.CommandText = "UPDATE GameResourcePackages SET Creation = @creation, LastWrite = @lastWrite, Size = @size WHERE Id = @id";
                        updatePackageCommand.Parameters.AddWithValue("@creation", ((DateTimeOffset)file.CreationTimeUtc).ToUnixTimeSeconds());
                        updatePackageCommand.Parameters.AddWithValue("@lastWrite", ((DateTimeOffset)file.LastWriteTimeUtc).ToUnixTimeSeconds());
                        updatePackageCommand.Parameters.AddWithValue("@size", file.Length);
                        updatePackageCommand.Parameters.AddWithValue("@id", packageId);
                        await updatePackageCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        using var insertCommand = connection.CreateCommand();
                        insertCommand.CommandText = "INSERT INTO GameResourcePackages (Creation, IsDelta, LastWrite, Path, Size) VALUES (@creation, @isDelta, @lastWrite, @path, @size); SELECT last_insert_rowid();";
                        insertCommand.Parameters.AddWithValue("@creation", ((DateTimeOffset)file.CreationTimeUtc).ToUnixTimeSeconds());
                        insertCommand.Parameters.AddWithValue("@isDelta", isDelta);
                        insertCommand.Parameters.AddWithValue("@lastWrite", ((DateTimeOffset)file.LastWriteTimeUtc).ToUnixTimeSeconds());
                        insertCommand.Parameters.AddWithValue("@path", file.FullName);
                        insertCommand.Parameters.AddWithValue("@size", file.Length);
                        if (await insertCommand.ExecuteScalarAsync().ConfigureAwait(false) is long nonNullLastInsertId)
                            packageId = nonNullLastInsertId;
                        else
                            throw new InvalidOperationException($"failed to retrieve insertion ID for package at path: {file.FullName}");
                    }
                    var catalogedResources = new Dictionary<ResourceKey, long>();
                    using (var getExistingResourceCommand = connection.CreateCommand())
                    {
                        getExistingResourceCommand.CommandText = "SELECT KeyType, KeyGroup, KeyFullInstance, Id FROM GameResourcePackageResources WHERE GameResourcePackageId = @gameResourcePackageId";
                        getExistingResourceCommand.Parameters.AddWithValue("@gameResourcePackageId", packageId);
                        using var reader = await getExistingResourceCommand.ExecuteReaderAsync().ConfigureAwait(false);
                        while (await reader.ReadAsync().ConfigureAwait(false))
                            catalogedResources.Add(new((ResourceType)unchecked((uint)reader.GetInt32(0)), unchecked((uint)reader.GetInt32(1)), unchecked((ulong)reader.GetInt64(2))), reader.GetInt64(3));
                    }
                    using var dbpf = await DataBasePackedFile.FromPathAsync(file.FullName).ConfigureAwait(false);
                    var resourceKeys = (await dbpf.GetKeysAsync().ConfigureAwait(false)).Where(key => typesCataloged.Contains(key.Type)).ToImmutableHashSet();
                    var noLongerExistingKeys = catalogedResources.Keys.Except(resourceKeys);
                    if (!noLongerExistingKeys.Any())
                        foreach (var noLongerExistingKeysChunk in noLongerExistingKeys.Chunk(800))
                        {
                            using var deleteResourcesCommand = connection.CreateCommand();
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
                    foreach (var resourceKeysChunk in resourceKeys.Chunk(200))
                    {
                        using var replaceIntoCommand = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                        replaceIntoCommand.CommandText = $"INSERT INTO GameResourcePackageResources (GameResourcePackageId, KeyType, KeyGroup, KeyFullInstance) VALUES {string.Join(", ", resourceKeysChunk.Select((key, index) =>
                        {
                            replaceIntoCommand.Parameters.AddWithValue($"@p{index}", packageId);
                            replaceIntoCommand.Parameters.AddWithValue($"@t{index}", unchecked((int)(uint)key.Type));
                            replaceIntoCommand.Parameters.AddWithValue($"@g{index}", unchecked((int)key.Group));
                            replaceIntoCommand.Parameters.AddWithValue($"@i{index}", unchecked((long)key.FullInstance));
                            return $"(@p{index}, @t{index}, @g{index}, @i{index})";
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
                            getStblResourceIdCommand.CommandText = "SELECT Id FROM GameResourcePackageResources WHERE GameResourcePackageId = @gameResourcePackageId AND KeyType = @keyType AND KeyGroup = @keyGroup AND KeyFullInstance = @keyFullInstance";
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
                            using var insertIntoCommand = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                            insertIntoCommand.CommandText = $"INSERT INTO GameStringTableEntries (GameResourcePackageResourceId, SignedKey) VALUES {string.Join(", ", keyHashesChunk.Select((key, index) =>
                            {
                                insertIntoCommand.Parameters.AddWithValue($"@r{index}", stlbResourceId);
                                insertIntoCommand.Parameters.AddWithValue($"@k{index}", unchecked((int)key));
                                return $"(@r{index}, @k{index})";
                            }))} ON CONFLICT DO NOTHING";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                            await insertIntoCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
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
