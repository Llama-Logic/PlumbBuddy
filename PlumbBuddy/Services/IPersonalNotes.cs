namespace PlumbBuddy.Services;

public interface IPersonalNotes :
    INotifyPropertyChanged
{
    string? EditNotes { get; set; }
    DateTime? EditPersonalDate { get; set; }
    DateTime? PersonalDateLowerBound { get; set; }
    DateTime? PersonalDateUpperBound { get; set; }
    string? SearchText { get; set; }

    event EventHandler? DataAltered;

    void HandleRecordOnPreviewEditClick(object? item);
    void HandleRecordRowEditCommit(object? item);
    Task<TableData<PersonalNotesRecord>> LoadRecordsAsync(TableState state, CancellationToken token);
}
