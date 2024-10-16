namespace PlumbBuddy.Services;

public interface ICustomThemes
{
    IReadOnlyDictionary<string, CustomTheme> Themes { get; }
}
