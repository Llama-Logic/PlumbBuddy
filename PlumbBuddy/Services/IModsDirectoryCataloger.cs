namespace PlumbBuddy.Services;

public interface IModsDirectoryCataloger :
    INotifyPropertyChanged
{
    TimeSpan? EstimatedStateTimeRemaining { get; }

    int PackageCount { get; }

    int PythonByteCodeFileCount { get; }

    int PythonScriptCount { get; }

    int ResourceCount { get; }

    int ScriptArchiveCount { get; }

    ModsDirectoryCatalogerState State { get; }

    void Catalog(string path);

    void GoToSleep();

    void WakeUp();
}
