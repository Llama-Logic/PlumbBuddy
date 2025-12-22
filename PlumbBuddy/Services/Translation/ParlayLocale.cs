namespace PlumbBuddy.Services.Translation;

public record ParlayLocale(CultureInfo Locale)
{
    public override string ToString() =>
        $"{Locale.NativeName}{(Locale.TwoLetterISOLanguageName == "en" ? string.Empty : $" - {Locale.EnglishName}")}";
}
