namespace PlumbBuddy.Services.Scans.Corrupt;

[Scan(IsEnabledByDefault = true)]
public interface IPackageCorruptScan :
    ICorruptScan
{
}
