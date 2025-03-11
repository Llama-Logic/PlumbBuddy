namespace PlumbBuddy.Data;

public enum ModHoundReportRecordStatus :
    int
{
    OutdatedMatches = 0,
    DuplicateMatches = 1,
    BrokenObsoleteMatches = 2,
    UnknownStatusMatches = 3,
    UpToDateOkayMatches = 4
}
