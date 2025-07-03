namespace PlumbBuddy.Services;

public interface IGameResourceCataloger :
    INotifyPropertyChanged
{
    bool IsBusy { get; }

    void ScanSoon();
}
