namespace PlumbBuddy.Services;

public interface IGameResourceCataloger :
    INotifyPropertyChanged
{
    int PackageExaminationsRemaining { get; }

    Task<ReadOnlyMemory<byte>> GetDirectDrawSurfaceAsPngAsync(ResourceKey key);

    Task<IReadOnlyList<(byte locale, uint locKey, string value)>> GetLocalizedStringsAsync(IEnumerable<uint> locKeys, IEnumerable<byte> locales);

    Task<ReadOnlyMemory<byte>> GetRawResourceAsync(ResourceKey key);

    void ScanSoon();
}
