namespace PlumbBuddy.Data;

public class ModHoundReportMissingRequirementsRecordDependent(ModHoundReportMissingRequirementsRecord modHoundReportMissingRequirementsRecord)
{
    ModHoundReportMissingRequirementsRecordDependent() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModHoundReportMissingRequirementsRecordId { get; set; }

    [ForeignKey(nameof(ModHoundReportMissingRequirementsRecordId))]
    public ModHoundReportMissingRequirementsRecord ModHoundReportMissingRequirementsRecord { get; set; } = modHoundReportMissingRequirementsRecord;

    [Required]
    public required string Label { get; set; }
}
