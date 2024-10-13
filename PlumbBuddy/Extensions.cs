namespace PlumbBuddy;

static class Extensions
{
    public static IEnumerable<byte> ToByteSequence(this string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even number of characters");
        return Enumerable
            .Range(0, hex.Length / 2)
            .Select(byteIndex => hex.Substring(byteIndex * 2, 2))
            .Select(byteHex => byte.Parse(byteHex, NumberStyles.HexNumber));
    }

    public static string ToHexString(this IEnumerable<byte> bytes) =>
        string.Join(string.Empty, bytes.Select(b => b.ToString("x2")));
}
