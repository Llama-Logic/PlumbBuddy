namespace PlumbBuddy.Models;

public class ModHoundIntegrationSettings
{
    public int PackagesBatchHardLimit { get; set; }
    public int PackagesBatchWarningThreshold { get; set; }
    public TimeSpan ReportServiceTimeout { get; set; }
    public string? ReportServiceUnavailableMessage { get; set; }
}
