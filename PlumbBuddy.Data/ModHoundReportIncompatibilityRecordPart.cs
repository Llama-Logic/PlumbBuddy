namespace PlumbBuddy.Data;

public class ModHoundReportIncompatibilityRecordPart
{
    [Key]
    public long Id { get; set; }

    public long ModHoundReportIncompatibilityRecordId { get; set; }

    [ForeignKey(nameof(ModHoundReportIncompatibilityRecordId))]
    public ModHoundReportIncompatibilityRecord? ModHoundReportIncompatibilityRecord { get; set; }

    [Required]
    public required string Label { get; set; }
}
