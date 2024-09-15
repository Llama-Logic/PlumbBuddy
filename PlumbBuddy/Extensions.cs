namespace PlumbBuddy;

static class Extensions
{
    public static string ToHexString(this byte[] bytes) =>
        string.Join(string.Empty, bytes.Select(b => b.ToString("x2")));
}
