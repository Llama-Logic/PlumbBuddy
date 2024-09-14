namespace PlumbBuddy.App.Services.Scans;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class ScanAttribute :
    Attribute
{
    public static ScanAttribute? Get(Type type) =>
        type.GetCustomAttribute<ScanAttribute>();

    public bool IsEnabledByDefault { get; init; }
}
