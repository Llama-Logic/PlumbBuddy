namespace PlumbBuddy.Platforms.MacCatalyst;

public class AppLifecycleManager :
    IAppLifecycleManager
{
    bool isVisible;

    public bool HideMainWindowAtLaunch { get; } = false;

    public bool IsVisible
    {
        get => isVisible;
        internal set
        {
            if (isVisible == value)
                return;
            isVisible = value;
            OnPropertyChanged();
        }
    }

    public bool PreventCasualClosing { get; set; }

    public Task UiReleaseSignal =>
        Task.CompletedTask;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void HideWindow()
    {
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public void ShowWindow()
    {
    }

    public void WindowFirstShown(Window window)
    {
    }
}