namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayStringTable
{
    [GeneratedRegex(@"\\.")]
    private static partial Regex GetEscapeSequencePattern();

    [GeneratedRegex(@"&lt;.*?&gt;")]
    private static partial Regex GetMarkupFormattingPattern();

    [GeneratedRegex(@"\{[A-Z]*?\d+\..*?\}")]
    private static partial Regex GetTokenPattern();

    public static MarkupString HighlightForDisplay(string text)
    {
        text = WebUtility.HtmlEncode(text);
        text = GetMarkupFormattingPattern().Replace(text, $"<span class=\"markup-formatting\">$0</span>");
        text = GetTokenPattern().Replace(text, $"<span class=\"token\">$0</span>");
        text = GetEscapeSequencePattern().Replace(text, $"<span class=\"escape-sequence\">$0</span>");
        return new(text);
    }

    public static bool IncludeEntry(IParlay parlay, ParlayStringTableEntry entry)
    {
        if (parlay is null || entry is null)
            return false;
        var entrySearchText = parlay.EntrySearchText;
        if (string.IsNullOrWhiteSpace(entrySearchText))
            return true;
        if (entry.Hash.ToString("x8").Contains(entrySearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (entry.Original.Contains(entrySearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (entry.Translation.Contains(entrySearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
