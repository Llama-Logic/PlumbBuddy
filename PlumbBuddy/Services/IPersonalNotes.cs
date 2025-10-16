namespace PlumbBuddy.Services;

public interface IPersonalNotes :
    INotifyPropertyChanged
{
    string? EditNotes { get; set; }
    DateTime? EditPersonalDate { get; set; }
    DateTime? FileDateLowerBound { get; set; }
    DateTime? FileDateUpperBound { get; set; }
    DateTime? ModFilesDateLowerBound { get; }
    DateTime? ModFilesDateUpperBound { get; }
    DateTime? PersonalDateLowerBound { get; set; }
    DateTime? PersonalDateUpperBound { get; set; }
    DateTime? PlayerDataDateLowerBound { get; }
    DateTime? PlayerDataDateUpperBound { get; }
    string? SearchText { get; set; }

    event EventHandler? DataAltered;

    void HandleRecordOnPreviewEditClick(object? item);
    void HandleRecordRowEditCommit(object? item);
    Task<TableData<PersonalNotesRecord>> LoadRecordsAsync(TableState state, CancellationToken token);
}
