namespace PlumbBuddy.Data;

public class ModHoundReportNotTrackedRecord
{
    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport? ModHoundReport { get; set; }

    public DateTimeOffset FileDate { get; set; }

    public required string FileName { get; set; }

    public ModHoundReportNotTrackedRecordFileType FileType { get; set; }
}
