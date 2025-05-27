using Microsoft.Maui.Handlers;

namespace PlumbBuddy;

partial class UiBridgeWebViewHandler :
    WebViewHandler
{
    const string uriPrefix = "plumbbuddy://bridged-ui/";

    UiBridgeWebView UiBridgeWebView =>
        (UiBridgeWebView)VirtualView;

    protected async ValueTask<(ReadOnlyMemory<byte> content, string contentType)> GetContentAsync(Uri uri)
    {
        var entryName = uri.AbsoluteUri.ToString();
        if (!entryName.StartsWith(uriPrefix, StringComparison.Ordinal))
            return (ReadOnlyMemory<byte>.Empty, string.Empty);
        entryName = entryName[uriPrefix.Length..];
        if (!string.IsNullOrEmpty(uri.Fragment))
            entryName = entryName[..^uri.Fragment.Length];
        if (!string.IsNullOrEmpty(uri.Query))
            entryName = entryName[..^uri.Query.Length];

        if (UiBridgeWebView.ScriptModFile.GetEntry(Path.Combine(UiBridgeWebView.BridgedUiRootPath, entryName).Replace("\\", "/", StringComparison.Ordinal)) is not { } entry)
            return (ReadOnlyMemory<byte>.Empty, string.Empty);
        var writer = new ArrayBufferWriter<byte>();
        using (var entryStream = entry.Open())
        {
            var rentedArray = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                Memory<byte> buffer = rentedArray;
                var bytesRead = await entryStream.ReadAsync(buffer).ConfigureAwait(false);
                while (bytesRead > 0)
                {
                    writer.Write(buffer.Span[..bytesRead]);
                    bytesRead = await entryStream.ReadAsync(buffer).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }
        return
        (
            writer.WrittenMemory,
            Path.GetExtension(entryName).ToUpperInvariant() switch
            {
                ".CSS" => "text/css",
                ".GIF" => "image/gif",
                ".HTM" or ".HTML" => "text/html",
                ".ICO" => "image/x-icon",
                ".JPG" or ".JPEG" => "image/jpeg",
                ".JS" or ".MJS" => "application/javascript",
                ".JSON" or ".MAP" => "application/json",
                ".MP3" => "audio/mp3",
                ".MP4" => "video/mp4",
                ".OGG" => "audio/ogg",
                ".OTF" => "font/otf",
                ".PNG" => "image/png",
                ".SVG" => "image/svg+xml",
                ".TTF" => "font/ttf",
                ".TXT" => "text/plain",
                ".WASM" => "application/wasm",
                ".WAV" => "audio/wav",
                ".WEBMANIFEST" => "application/manifest+json",
                ".WEBP" => "image/webp",
                ".WOFF" => "font/woff",
                ".WOFF2" => "font/woff2",
                ".XML" => "text/xml",
                _ => "application/octet-stream"
            }
        );
    }
}