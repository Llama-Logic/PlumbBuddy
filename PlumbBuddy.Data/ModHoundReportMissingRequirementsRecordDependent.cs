namespace PlumbBuddy.Data;

public class ModHoundReportMissingRequirementsRecordDependent
{
    [Key]
    public long Id { get; set; }

    public long ModHoundReportMissingRequirementsRecordId { get; set; }

    [ForeignKey(nameof(ModHoundReportMissingRequirementsRecordId))]
    public ModHoundReportMissingRequirementsRecord? ModHoundReportMissingRequirementsRecord { get; set; }

    [Required]
    public required string Label { get; set; }
}
