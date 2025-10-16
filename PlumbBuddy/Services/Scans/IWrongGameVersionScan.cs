namespace PlumbBuddy.Services.Scans;

[Scan(IsEnabledByDefault = true)]
public interface IWrongGameVersionScan :
    IScan
{
}
