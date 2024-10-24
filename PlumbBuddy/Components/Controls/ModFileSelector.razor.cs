using LlamaLogic.Packages.Cryptography;
using System.Security.Cryptography;

namespace PlumbBuddy.Components.Controls;

partial class ModFileSelector
{
    public static async Task<FileInfo?> SelectAModFileAsync()
    {
        if (await FilePicker.PickAsync(new()
        {
            PickerTitle = "Select a Mod File",
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
            PickerTitle = "Select a Mod File",
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
        {
            IReadOnlyDictionary<ResourceKey, ModFileManifestModel>? manifests = null;
            var hash = await ModFileManifestModel.GetFileSha256HashAsync(modFile.FullName);
            var hashArray = Unsafe.As<ImmutableArray<byte>, byte[]>(ref hash);
            if (await pbDbContext.ModFileHashes
                .AsSplitQuery()
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
            //Memory<byte> hash1 = new byte[32];
            //Memory<byte> hash2 = new byte[32];
            //Memory<byte> hash3 = new byte[32];
            //using (var rng = RandomNumberGenerator.Create())
            //{
            //    rng.GetBytes(hash1.Span);
            //    rng.GetBytes(hash2.Span);
            //    rng.GetBytes(hash3.Span);
            //}
            //manifests = new Dictionary<ResourceKey, ModFileManifestModel>
            //{
            //    { $"{(uint)ResourceType.SnippetTuning:x8}:80000000:{Fnv64.GetHash("creator:manifest_ThisModA"):x16}", new() { Name = "This Mod A", TuningName = "creator:manifest_ThisModA", Hash = [..hash1.Span] } },
            //    { $"{(uint)ResourceType.SnippetTuning:x8}:80000000:{Fnv64.GetHash("creator:manifest_SomeModB"):x16}", new() { Name = "Some Mod B", TuningName = "creator:manifest_SomeModB", Hash = [..hash2.Span] } },
            //    { $"{(uint)ResourceType.SnippetTuning:x8}:80000000:{Fnv64.GetHash("creator:manifest_SomeOtherModC"):x16}", new() { Name = "Some Other Mod C", TuningName = "creator:manifest_SomeOtherModC", Hash = [..hash3.Span] } },
            //};
            //Memory<byte> hash = new byte[32];
            //using (var rng = RandomNumberGenerator.Create())
            //    rng.GetBytes(hash.Span);
            //ResourceKey resourceKey = $"{(uint)ResourceType.SnippetTuning:x8}:80000000:{Fnv64.GetHash("lot51:manifest_CoreLibrary"):x16}";
            //manifests = new Dictionary<ResourceKey, ModFileManifestModel>
            //{
            //    { resourceKey, new()
            //    {
            //        Name = "Core Library",
            //        Hash = [..hash.Span],
            //        TuningName = "lot51:manifest_CoreLibrary",
            //        Url = new("https://lot51.cc/core", UriKind.Absolute),
            //        Version = "1.25.1"
            //    } }
            //};
            //manifests[resourceKey].Creators.Add("Lot 51");
            //manifests[resourceKey].Features.Add("Tuning Injector Tunable Snippet");
            //manifests[resourceKey].Features.Add("Purchase Picker Tunable Snippet");
            //manifests[resourceKey].Features.Add("Lot51 Core Loot Actions Tunable Snippet");
            //manifests[resourceKey].Features.Add("Affordance Lock-Out Snippet");
            //manifests[resourceKey].Features.Add("lot51_core.open_url Command");
            //manifests[resourceKey].Features.Add("purchase_picker.refresh Command");
            if (manifests?.Count is 0)
            {
                await dialogService.ShowErrorDialogAsync("Mod file contains no manifests",
                    $"""
                    I'm sorry, but the mod file you selected doesn't contain any manifests. For technical reasons, it simply isn't safe to try to reference it in this manner. All you can do for now is *politely* ask the original creator to publish it with a manifest in their next release.
                    `{modFile.FullName}`<br /><br />
                    <iframe src="https://giphy.com/embed/3oEjI8D0T5KXgPZrTW" width="480" height="269" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/summerbreak-summer-break-sb3-3oEjI8D0T5KXgPZrTW">via GIPHY</a></p>
                    """).ConfigureAwait(false);
                return default;
            }
            if (manifests?.Count is > 1)
            {
                await dialogService.ShowInfoDialogAsync("Mod file contains multiple manifests",
                    $"""
                    This is embarassing, but I'm going to have to ask you to select precisely which of the manifests this mod file contains that you mean because some bozo merged their files and didn't update the manifests. 🤦
                    `{modFile.FullName}`<br /><br />
                    <iframe src="https://giphy.com/embed/8Fla28qk2RGlYa2nXr" width="480" height="259" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/8Fla28qk2RGlYa2nXr">via GIPHY</a></p>
                    """).ConfigureAwait(false);
                if (manifests.TryGetValue(await dialogService.ShowSelectManifestDialogAsync(manifests), out var manifest))
                    return manifest;
            }
            return manifests!.Values.First();
        }
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
                return "There is no file at this location.";
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
            return "This is not a Maxis DBPF package or a TS4 script archive.";
        }
        finally
        {
            FileTypeChanged.InvokeAsync(FileType);
        }
    }
}
