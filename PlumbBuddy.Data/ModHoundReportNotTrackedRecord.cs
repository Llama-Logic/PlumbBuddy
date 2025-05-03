namespace PlumbBuddy.Data;

public class ModHoundReportNotTrackedRecord(ModHoundReport modHoundReport)
{
    ModHoundReportNotTrackedRecord() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport ModHoundReport { get; set; } = modHoundReport;

    public DateTimeOffset FileDate { get; set; }

    public string? FileDateString { get; set; }

    public required string FileName { get; set; }

    public ModHoundReportNotTrackedRecordFileType FileType { get; set; }
}
