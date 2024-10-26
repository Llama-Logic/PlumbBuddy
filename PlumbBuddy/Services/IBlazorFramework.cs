namespace PlumbBuddy.Services;

public interface IBlazorFramework :
    INotifyPropertyChanged
{
    ILifetimeScope? MainLayoutLifetimeScope { get; set; }
}
