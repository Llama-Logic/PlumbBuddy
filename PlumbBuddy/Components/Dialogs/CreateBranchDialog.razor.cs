using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using ResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;

namespace PlumbBuddy.Components.Dialogs;

partial class CreateBranchDialog
{
    [Parameter]
    public string ChronicleName { get; set; } = string.Empty;

    [Parameter]
    public string GameNameOverride { get; set; } = string.Empty;

    [Parameter]
    public string Notes { get; set; } = string.Empty;

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public ImmutableArray<byte> Thumbnail { get; set; } = [];

    async Task BrowseForCustomThumbnailAsync()
    {
        if (await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a Custom Thumbnail",
            FileTypes = FilePickerFileType.Images
        }).ConfigureAwait(false) is { } fileResult)
        {
            try
            {
                using var fileStream = await fileResult.OpenReadAsync().ConfigureAwait(false);
                var thumbnail = await Image.LoadAsync<Rgba32>(fileStream).ConfigureAwait(false);
                if (thumbnail.Width > 2048 || thumbnail.Height > 2048)
                    thumbnail.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new(2048, 2048),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3
                    }));
                using var thumbnailMemoryStream = new MemoryStream();
                await thumbnail.SaveAsPngAsync(thumbnailMemoryStream).ConfigureAwait(false);
                Thumbnail = thumbnailMemoryStream.ToArray().ToImmutableArray();
            }
            catch (Exception ex)
            {
                await DialogService.ShowErrorDialogAsync("Set Thumbnail Failed", $"{ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
            }
        }
    }

    void CancelOnClickHandler() =>
        MudDialog?.Close(DialogResult.Cancel());

    void ClearThumbnail() =>
        Thumbnail = [];

    void OkOnClickHandler() =>
        MudDialog?.Close(DialogResult.Ok(new CreateBranchDialogResult
        {
            ChronicleName = ChronicleName,
            GameNameOverride = string.IsNullOrWhiteSpace(GameNameOverride)
                ? null
                : GameNameOverride,
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? null
                : Notes,
            Thumbnail = Thumbnail
        }));
}
