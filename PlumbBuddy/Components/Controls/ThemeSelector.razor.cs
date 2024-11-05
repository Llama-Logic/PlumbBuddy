namespace PlumbBuddy.Components.Controls;

partial class ThemeSelector
{
    string? originalTheme;

    public void Cancel() =>
        Settings.Theme = originalTheme;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        originalTheme = Settings.Theme;
    }

    void SelectTheme(string? theme)
    {
        Settings.Theme = theme;
        StateHasChanged();
    }
}
