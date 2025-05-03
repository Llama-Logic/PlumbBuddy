namespace PlumbBuddy.Data;

public class ModHoundReportMissingRequirementsRecord(ModHoundReport modHoundReport)
{
    ModHoundReportMissingRequirementsRecord() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModHoundReportId { get; set; }

    [ForeignKey(nameof(ModHoundReportId))]
    public ModHoundReport ModHoundReport { get; set; } = modHoundReport;

    public ICollection<ModHoundReportMissingRequirementsRecordDependency> Dependencies { get; } = [];

    public ICollection<ModHoundReportMissingRequirementsRecordDependent> Dependents { get; } = [];
}
