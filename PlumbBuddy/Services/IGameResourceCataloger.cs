namespace PlumbBuddy.Services;

public interface IGameResourceCataloger :
    INotifyPropertyChanged
{
    int PackageExaminationsRemaining { get; }

    Task<ReadOnlyMemory<byte>> GetDirectDrawSurfaceAsPngAsync(ResourceKey key);

    Task<IReadOnlyList<(byte locale, uint locKey, string value)>> GetLocalizedStringsAsync(IEnumerable<uint> locKeys, IEnumerable<byte> locales);

    Task<ReadOnlyMemory<byte>> GetPackIcon128Async(string packCode);

    Task<ReadOnlyMemory<byte>> GetPackIcon32Async(string packCode);

    Task<ReadOnlyMemory<byte>> GetPackIcon64Async(string packCode);

    Task<ReadOnlyMemory<byte>> GetPackIconOwnedAsync(string packCode);

    Task<ReadOnlyMemory<byte>> GetPackIconUnownedAsync(string packCode);

    Task<ReadOnlyMemory<byte>> GetRawResourceAsync(ResourceKey key);

    void ScanSoon();
}
