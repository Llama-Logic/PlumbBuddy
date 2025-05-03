namespace PlumbBuddy.Data;

public class ModHoundReportRecord(ModHoundReport modHoundReport)
{
    ModHoundReportRecord() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport ModHoundReport { get; set; } = modHoundReport;

    public required string FileName { get; set; }

    public required string FilePath { get; set; }

    public required string ModName { get; set; }

    public required string CreatorName { get; set; }

    public DateTimeOffset LastUpdateDate { get; set; }

    public string? LastUpdateDateString { get; set; }

    public DateTimeOffset DateOfInstalledFile { get; set; }

    public string? DateOfInstalledFileString { get; set; }

    public ModHoundReportRecordStatus Status { get; set; }

    public string? ModLinkOrIndexText { get; set; }

    public Uri? ModLinkOrIndexHref { get; set; }

    public string? UpdateNotes { get; set; }
}
