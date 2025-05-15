namespace PlumbBuddy.Models;

public class ModHoundIntegrationSettings
{
    public int PackagesBatchHardLimit { get; set; } = 26000;
    public int PackagesBatchWarningThreshold { get; set; } = 5000;
    public TimeSpan ReportServiceTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public string? ReportServiceUnavailableMessage { get; set; }
}
