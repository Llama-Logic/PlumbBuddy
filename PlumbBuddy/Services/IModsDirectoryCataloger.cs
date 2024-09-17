namespace PlumbBuddy.Services;

public interface IModsDirectoryCataloger :
    INotifyPropertyChanged
{
    ModDirectoryCatalogerState State { get; }

    int PackageCount { get; }

    int ResourceCount { get; }

    int ScriptArchiveCount { get; }

    void Catalog(string path);

    void GoToSleep();

    void WakeUp();
}
