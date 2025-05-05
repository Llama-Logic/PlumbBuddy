namespace PlumbBuddy.Services;

public interface IModHoundClient :
    INotifyPropertyChanged
{
    const int PackagesBatchHardLimit = 26_000;
    const int PackagesBatchWarningThreshold = 5_000;

    const string SectionBrokenObsolete = "BrokenObsolete";
    const string SectionDuplicates = "Duplicates";
    const string SectionIncompatible = "Incompatible";
    const string SectionMissingRequirements = "MissingRequirements";
    const string SectionNotTracked = "NotTracked";
    const string SectionOutdated = "Outdated";
    const string SectionUnknownStatus = "UnknownStatus";
    const string SectionUpToDate = "UpToDate";

    ReadOnlyObservableCollection<ModHoundReportSelection> AvailableReports { get; }

    int? BrokenObsoleteCount { get; }

    int? DuplicatesCount { get; }

    int? IncompatibleCount { get; }

    int? MissingRequirementsCount { get; }

    int? NotTrackedCount { get; }

    int? OutdatedCount { get; }

    int? ProgressMax { get; }

    int? ProgressValue { get; }

    int? RequestPhase { get; }

    string SearchText { get; set; }

    ModHoundReportSelection? SelectedReport { get; set; }

    ReadOnlyObservableCollection<ModHoundReportIncompatibilityRecord> SelectedReportIncompatibilityRecords { get; }

    ReadOnlyObservableCollection<ModHoundReportMissingRequirementsRecord> SelectedReportMissingRequirementsRecords { get; }

    string? SelectedReportSection { get; set; }

    string? Status { get; }

    int? UnknownStatusCount { get; }

    int? UpToDateCount { get; }

    void RequestReport();
}
