namespace PlumbBuddy.Data;

public class ModHoundReportMissingRequirementsRecord
{
    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport? ModHoundReport { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModHoundReportMissingRequirementsRecordDependency>? Dependencies { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModHoundReportMissingRequirementsRecordDependent>? Dependents { get; set; }
}
