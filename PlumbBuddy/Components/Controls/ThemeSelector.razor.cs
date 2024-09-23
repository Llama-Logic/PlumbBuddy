namespace PlumbBuddy.Components.Controls;

partial class ThemeSelector
{
    const string amethystLilac = "Amethyst Lilac";

    string? originalTheme;

    public void Cancel() =>
        Player.Theme = originalTheme;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        originalTheme = Player.Theme;
    }

    void SelectTheme(string? theme) =>
        Player.Theme = theme;
}
