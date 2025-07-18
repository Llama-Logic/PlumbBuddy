namespace PlumbBuddy.Services;

public interface IGameResourceCataloger :
    INotifyPropertyChanged
{
    int PackageExaminationsRemaining { get; }

    void ScanSoon();
}
