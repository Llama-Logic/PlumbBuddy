namespace PlumbBuddy.Components.Controls;

partial class ModFileSelector
{
    public static async Task<ModFileManifestModel?> GetADroppedModFileManifestAsync(IUserInterfaceMessaging userInterfaceMessaging, PbDbContext pbDbContext, IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(userInterfaceMessaging);
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(dialogService);
        if ((await userInterfaceMessaging.GetFilesFromDragAndDropAsync())
            .Select(path => new FileInfo(path))
            .FirstOrDefault(file => file.Exists && (file.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))) is { } modFile)
            return await GetSelectedModFileManifestAsync(pbDbContext, dialogService, modFile);
        return default;
    }

    public static async Task<ImmutableArray<byte>> GetADroppedModFileManifestHashAsync(IUserInterfaceMessaging userInterfaceMessaging, PbDbContext pbDbContext, IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(userInterfaceMessaging);
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(dialogService);
        if ((await userInterfaceMessaging.GetFilesFromDragAndDropAsync())
            .Select(path => new FileInfo(path))
            .FirstOrDefault(file => file.Exists && (file.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))) is { } modFile
            && await GetSelectedModFileManifestAsync(pbDbContext, dialogService, modFile) is { } manifest)
            return manifest.Hash;
        return default;
    }

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    static async Task<ModFileManifestModel?> GetSelectedModFileManifestAsync(PbDbContext pbDbContext, IDialogService dialogService, FileInfo modFile)
    {
        IReadOnlyDictionary<ResourceKey, ModFileManifestModel>? manifests = null;
        ImmutableArray<byte> hash;
        try
        {
            hash = await ModFileManifestModel.GetFileSha256HashAsync(modFile.FullName);
        }
        catch
        {
            if (modFile.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))
                await dialogService.ShowErrorDialogAsync
                (
                    AppText.ManifestEditor_Error_InaccessibleScriptArchive_Caption,
                    string.Format(AppText.ManifestEditor_Error_InaccessibleScriptArchive_Text, modFile.FullName)
                ).ConfigureAwait(false);
            else
                await dialogService.ShowErrorDialogAsync
                (
                    AppText.ManifestEditor_Error_InaccessiblePackage_Caption,
                    string.Format(AppText.ManifestEditor_Error_InaccessiblePackage_Text, modFile.FullName)
                ).ConfigureAwait(false);
            return null;
        }
        var hashArray = Unsafe.As<ImmutableArray<byte>, byte[]>(ref hash);
        if (await pbDbContext.ModFileHashes
            .Where(mfh => mfh.Sha256 == hashArray)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.Creators)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.ElectronicArtsPromoCode)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.Exclusivities)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.Features)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.HashResourceKeys)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.IncompatiblePacks)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.InscribedModFileManifestHash)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.RequiredMods!)
                    .ThenInclude(rm => rm.Creators)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.RequiredMods!)
                    .ThenInclude(rm => rm.Hashes)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.RequiredMods!)
                    .ThenInclude(rm => rm.RequiredFeatures)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.RequiredMods!)
                    .ThenInclude(rm => rm.RequirementIdentifier)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.RequiredPacks)
            .Include(mfh => mfh.ModFileManifests!)
                .ThenInclude(mfm => mfm.SubsumedHashes)
            .FirstOrDefaultAsync() is { } modFileHash && modFileHash.ModFileManifests?.Count is > 0)
        {
            var fullInstanceGuard = 0L;
            manifests = modFileHash.ModFileManifests
                .ToDictionary
                (
                    modFileManifest => new ResourceKey(ResourceType.SnippetTuning, 0x80000000, unchecked((ulong)(modFileManifest.TuningFullInstance ?? ++fullInstanceGuard))),
                    modFileManifest => modFileManifest.ToModel()
                );
        }
        else if (modFile.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var dbpf = await DataBasePackedFile.FromPathAsync(modFile.FullName);
                manifests = await ModFileManifestModel.GetModFileManifestsAsync(dbpf);
            }
            catch
            {
            }
        }
        else if (modFile.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var editableManifests = new Dictionary<ResourceKey, ModFileManifestModel>();
                manifests = editableManifests;
                using var zipArchive = ZipFile.OpenRead(modFile.FullName);
                if (await ModFileManifestModel.GetModFileManifestAsync(zipArchive) is { } scriptModManifest)
                    editableManifests.Add(default, scriptModManifest);
            }
            catch
            {
            }
        }
        if (manifests?.Count is 0)
        {
            await dialogService.ShowErrorDialogAsync
            (
                AppText.ModFileSelector_SelectAModFileManifest_Error_NoManifests_Caption,
                string.Format(AppText.ModFileSelector_SelectAModFileManifest_Error_NoManifests_Text, modFile.FullName)
            ).ConfigureAwait(false);
            return default;
        }
        if (manifests?.Count is > 1)
        {
            await dialogService.ShowInfoDialogAsync
            (
                AppText.ModFileSelector_SelectAModFileManifest_Info_MultipleManifests_Caption,
                string.Format(AppText.ModFileSelector_SelectAModFileManifest_Info_MultipleManifests_Text, modFile.FullName)
            ).ConfigureAwait(false);
            if (manifests.TryGetValue(await dialogService.ShowSelectManifestDialogAsync(manifests), out var manifest))
                return manifest;
        }
        return manifests!.Values.First();
    }

    public static async Task<FileInfo?> SelectAModFileAsync()
    {
        if (await FilePicker.PickAsync(new()
        {
            PickerTitle = AppText.ModFileSelector_SelectAModFile_Caption,
            FileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>()
            {
                { DevicePlatform.WinUI, [".package", ".ts4script"] },
                { DevicePlatform.macOS, ["public.data"] },
                { DevicePlatform.MacCatalyst, ["public.data"] }
            })
        }) is { } result)
            return new(result.FullPath);
        return null;
    }

    public static async Task<IReadOnlyList<FileInfo>> SelectModFilesAsync()
    {
        var files = new List<FileInfo>();
        if (await FilePicker.PickMultipleAsync(new()
        {
            PickerTitle = AppText.ModFileSelector_SelectAModFile_Caption,
            FileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>()
            {
                { DevicePlatform.WinUI, [".package", ".ts4script"] },
                { DevicePlatform.macOS, ["public.data"] },
                { DevicePlatform.MacCatalyst, ["public.data"] }
            })
        }) is { } results)
            files.AddRange(results.Select(result => new FileInfo(result.FullPath)));
        return files;
    }

    public static async Task<ImmutableArray<byte>> SelectAModFileManifestHashAsync(PbDbContext pbDbContext, IDialogService dialogService)
    {
        if (await SelectAModFileManifestAsync(pbDbContext, dialogService) is { } manifest)
            return manifest.Hash;
        return default;
    }

    public static async Task<ModFileManifestModel?> SelectAModFileManifestAsync(PbDbContext pbDbContext, IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(dialogService);
        if (await SelectAModFileAsync() is { } modFile)
            return await GetSelectedModFileManifestAsync(pbDbContext, dialogService, modFile);
        return default;
    }

    string icon = MaterialDesignIcons.Normal.File;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public FileInfo? File { get; set; }

    [Parameter]
    public EventCallback<FileInfo> FileChanged { get; set; }

    [Parameter]
    public ModsDirectoryFileType FileType { get; set; }

    [Parameter]
    public EventCallback<ModsDirectoryFileType> FileTypeChanged { get; set; }

    string FilePath
    {
        get => File?.FullName ?? string.Empty;
        set
        {
            var file = !string.IsNullOrWhiteSpace(value) && System.IO.File.Exists(value)
                ? new FileInfo(value)
                : null;
            File = file;
            FileChanged.InvokeAsync(file);
        }
    }

    [Parameter]
    public string Label { get; set; } = "Mod File";

    async Task HandleBrowseOnClickAsync()
    {
        if (await SelectAModFileAsync() is { } modFile)
            FilePath = modFile.FullName;
    }

    async Task HandleDragAndDropOnClickAsync()
    {
        var paths = await UserInterfaceMessaging.GetFilesFromDragAndDropAsync();
        if (paths.Any())
            FilePath = paths[0];
    }

    string? ValidateModFilePath(string? path)
    {
        try
        {
            FileType = ModsDirectoryFileType.Ignored;
            icon = MaterialDesignIcons.Normal.File;
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var file = new FileInfo(path);
            if (!file.Exists)
                return AppText.ModFileSelector_Validate_FileNotFound;
            if (file.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
            {
                FileType = ModsDirectoryFileType.Package;
                icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck;
                return null;
            }
            if (file.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))
            {
                FileType = ModsDirectoryFileType.ScriptArchive;
                icon = MaterialDesignIcons.Normal.SourceBranchCheck;
                return null;
            }
            return AppText.ModFileSelector_Validate_InvalidFormat;
        }
        finally
        {
            FileTypeChanged.InvokeAsync(FileType);
        }
    }
}
