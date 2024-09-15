namespace PlumbBuddy.Services;

public enum SmartSimCacheStatus
{
    /// <summary>
    /// There are no cache components present on disk
    /// </summary>
    Clear,

    /// <summary>
    /// At least one cache component exists on disk
    /// </summary>
    Normal,

    /// <summary>
    /// At least one cache component existed on disk when a package was replaced or removed
    /// </summary>
    Stale
}
