namespace PlumbBuddy.Services;

public partial class BlazorFramework :
    IBlazorFramework
{
    ILifetimeScope? mainLayoutLifetimeScope;

    public ILifetimeScope? MainLayoutLifetimeScope
    {
        get => mainLayoutLifetimeScope;
        set
        {
            if (mainLayoutLifetimeScope == value)
                return;
            mainLayoutLifetimeScope = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
