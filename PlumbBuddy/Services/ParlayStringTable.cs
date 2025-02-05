namespace PlumbBuddy.Services;

public record ParlayStringTable(ResourceKey StringTableKey, CultureInfo Locale)
{
    public override string ToString() =>
        $"{Locale.NativeName}{(Locale.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? string.Empty : $" - {Locale.EnglishName}")} - {StringTableKey.GroupHex}:{StringTableKey.FullInstanceHex}";
}
