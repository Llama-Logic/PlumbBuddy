namespace PlumbBuddy.Data;

public class ModHoundReportIncompatibilityRecord
{
    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport? ModHoundReport { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModHoundReportIncompatibilityRecordPart>? Parts { get; set; }
}
