namespace PlumbBuddy.Services;

public record ModHoundReportSelection(long Id, DateTimeOffset Retrieved)
{
    public override string ToString() =>
        $"{Retrieved.ToLocalTime():g}";
}
