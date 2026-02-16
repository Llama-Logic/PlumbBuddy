namespace PlumbBuddy.Services.Archival;

static class Extensions
{
    public static byte[] ToByteArray(this ResourceKey key)
    {
        var keyBytes = new byte[16];
        Span<byte> keyBytesSpan = keyBytes;
        var type = key.Type;
        MemoryMarshal.Write(keyBytesSpan[0..4], in type);
        var group = key.Group;
        MemoryMarshal.Write(keyBytesSpan[4..8], in group);
        var fullInstance = key.FullInstance;
        MemoryMarshal.Write(keyBytesSpan[8..16], in fullInstance);
        return keyBytes;
    }

    public static ImmutableArray<byte> ToImmutableArray(this ReadOnlyMemory<byte> readOnlyMemory)
    {
        if (MemoryMarshal.TryGetArray(readOnlyMemory, out var segment))
        {
            if (segment.Offset is 0 && segment.Count == readOnlyMemory.Length)
                return ImmutableArray.Create(segment.Array);
            return [..segment];
        }
        return ImmutableArray.Create(readOnlyMemory.ToArray());
    }

    public static ResourceKey ToResourceKey(this byte[] keyBytes)
    {
        ArgumentNullException.ThrowIfNull(keyBytes);
        if (keyBytes.Length != 16)
            throw new ArgumentException("key must be precisely sixteen bytes");
        ReadOnlySpan<byte> keyBytesSpan = keyBytes;
        return new
        (
            MemoryMarshal.Read<ResourceType>(keyBytesSpan[0..4]),
            MemoryMarshal.Read<uint>(keyBytesSpan[4..8]),
            MemoryMarshal.Read<ulong>(keyBytesSpan[8..16])
        );
    }
}
