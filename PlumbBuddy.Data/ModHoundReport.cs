namespace PlumbBuddy.Data;

[Index(nameof(RequestSha256), nameof(Retrieved))]
public class ModHoundReport
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] RequestSha256 { get; set; }

    public Guid TaskId { get; set; }

    public required string ResultId { get; set; }

    public required string ReportHtml { get; set; }

    public DateTimeOffset Retrieved { get; set; }

    public DateTimeOffset LastEditedAtAny { get; set; }

    public ICollection<ModHoundReportRecord> Records { get; } = [];

    public ICollection<ModHoundReportIncompatibilityRecord> IncompatibilityRecords { get; } = [];

    public ICollection<ModHoundReportMissingRequirementsRecord> MissingRequirementsRecords { get; } = [];

    public ICollection<ModHoundReportNotTrackedRecord> NotTrackedRecords { get; } = [];
}
