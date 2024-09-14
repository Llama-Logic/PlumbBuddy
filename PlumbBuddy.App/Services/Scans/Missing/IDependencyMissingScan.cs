namespace PlumbBuddy.App.Services.Scans.Missing;

[Scan(IsEnabledByDefault = true)]
public interface IDependencyMissingScan :
    IMissingScan
{
}
