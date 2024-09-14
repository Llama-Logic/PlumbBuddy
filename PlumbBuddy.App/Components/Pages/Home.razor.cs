using PlumbBuddy.App.Services;

namespace PlumbBuddy.App.Components.Pages;

partial class Home
{
    /// <inheritdoc />
    public void Dispose() =>
        Player.PropertyChanged -= HandleUserPreferencesChanged;

    void HandleUserPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Player.Type))
            StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandleUserPreferencesChanged;
    }
}
