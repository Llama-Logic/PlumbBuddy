namespace PlumbBuddy.Services;

public interface IModHoundClient :
    INotifyPropertyChanged
{
    int? ProcessingCurrent { get; }

    int? ProcessingTotal { get; }

    void RequestReport();
}
