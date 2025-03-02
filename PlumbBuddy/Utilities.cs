namespace PlumbBuddy;

static class Utilities
{
    /// <summary>
    /// Gets the two-letter country code associated with a given language identifier
    /// </summary>
    /// <param name="language">The language identifier</param>
    /// <remarks>Amethyst is cutting it close</remarks>
    public static string GetCountryCodeFromLanguageIdentifier(string language)
    {
        if (language.Equals("cs", StringComparison.OrdinalIgnoreCase))
            return "CZ"; // OMG RegionInfo will actually lie to you about this
        try
        {
            return new RegionInfo(language).TwoLetterISORegionName;
        }
        catch
        {
        }
        language = language.ToUpperInvariant();
        return language switch
        {
            "DA" => "DK", // Danish => Denmark
            "NB" => "NO", // Norwegian => Norway
            _ => language
        };
    }
}
