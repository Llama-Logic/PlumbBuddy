namespace PlumbBuddy.Data;

public class ModHoundReportMissingRequirementsRecordDependency
{
    [Key]
    public long Id { get; set; }

    public long ModHoundReportMissingRequirementsRecordId { get; set; }

    [ForeignKey(nameof(ModHoundReportMissingRequirementsRecordId))]
    public ModHoundReportMissingRequirementsRecord? ModHoundReportMissingRequirementsRecord { get; set; }

    [Required]
    public required string Label { get; set; }

    public string? ModLinkOrIndexText { get; set; }

    public Uri? ModLinkOrIndexHref { get; set; }
}
