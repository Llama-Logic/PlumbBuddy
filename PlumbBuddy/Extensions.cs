#if MACCATALYST
using PlumbBuddy.Platforms.MacCatalyst;
#elif WINDOWS
using PlumbBuddy.Platforms.Windows;
#endif

namespace PlumbBuddy;

static partial class Extensions
{
    public static void AddFileDragAndDropHandler(this IElement element, IMauiContext mauiContext, Func<IReadOnlyList<string>, Task> dropHandler)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(mauiContext);
        FileDragAndDrop.AddHandler(element.ToPlatform(mauiContext), dropHandler);
    }

    [GeneratedRegex(@"[^\da-f]", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GetNonHexStringCharacterPattern();

    public static TValue GetLanguageOptimalValue<TValue>(this IReadOnlyDictionary<string, TValue> dictionary, Func<TValue> createEmptyValue)
    {
        var userLocaleName = CultureInfo.CurrentUICulture.Name;
        if (dictionary.TryGetValue(userLocaleName, out var regionOrCountryMatch))
            return regionOrCountryMatch;
        var slashIndex = userLocaleName.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex is >= 0 && dictionary.TryGetValue(userLocaleName[0..slashIndex], out var regionedCountryMatch))
            return regionedCountryMatch;
        if (dictionary.TryGetValue(userLocaleName[0..userLocaleName.IndexOf('-', StringComparison.Ordinal)], out var languageMatch))
            return languageMatch;
        if (dictionary.TryGetValue(string.Empty, out var invariant))
            return invariant;
        return createEmptyValue();
    }

    public static void RemoveFileDragAndDropHandler(this IElement element, IMauiContext mauiContext, Func<IReadOnlyList<string>, Task> dropHandler)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(mauiContext);
        FileDragAndDrop.RemoveHandler(element.ToPlatform(mauiContext), dropHandler);
    }

    public static IEnumerable<byte> ToByteSequence(this string hex)
    {
        if (!TryToByteSequence(hex, out var sequence))
            throw new ArgumentException("not a valid hex string", nameof(hex));
        return sequence;
    }

    public static bool TryToByteSequence(this string hex, [NotNullWhen(true)] out IEnumerable<byte>? sequence)
    {
        if (hex is null
            || hex.Length % 2 != 0
            || GetNonHexStringCharacterPattern().IsMatch(hex))
        {
            sequence = default;
            return false;
        }
        sequence = Enumerable
            .Range(0, hex.Length / 2)
            .Select(byteIndex => hex.Substring(byteIndex * 2, 2))
            .Select(byteHex => byte.Parse(byteHex, NumberStyles.HexNumber));
        return true;
    }

    public static string ToHexString(this IEnumerable<byte> bytes) =>
        string.Join(string.Empty, bytes.Select(b => b.ToString("x2")));
}
