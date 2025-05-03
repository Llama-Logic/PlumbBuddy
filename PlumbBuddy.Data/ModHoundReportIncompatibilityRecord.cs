namespace PlumbBuddy.Data;

public class ModHoundReportIncompatibilityRecord(ModHoundReport modHoundReport)
{
    ModHoundReportIncompatibilityRecord() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport ModHoundReport { get; set; } = modHoundReport;

    public ICollection<ModHoundReportIncompatibilityRecordPart> Parts { get; } = [];
}
