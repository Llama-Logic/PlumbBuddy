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
