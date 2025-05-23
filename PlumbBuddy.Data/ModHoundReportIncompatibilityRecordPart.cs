namespace PlumbBuddy.Data;

public class ModHoundReportIncompatibilityRecordPart(ModHoundReportIncompatibilityRecord modHoundReportIncompatibilityRecord)
{
    ModHoundReportIncompatibilityRecordPart() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModHoundReportIncompatibilityRecordId { get; set; }

    [ForeignKey(nameof(ModHoundReportIncompatibilityRecordId))]
    public ModHoundReportIncompatibilityRecord ModHoundReportIncompatibilityRecord { get; set; } = modHoundReportIncompatibilityRecord;

    public required string Label { get; set; }

    public required string FilePath { get; set; }
}
