namespace PlumbBuddy;

sealed class ReadOnlyMemoryOfByteStream :
    Stream
{
    public ReadOnlyMemoryOfByteStream(ReadOnlyMemory<byte> readOnlyMemoryOfBytes) =>
        this.readOnlyMemoryOfBytes = readOnlyMemoryOfBytes;

    long position;
    readonly ReadOnlyMemory<byte> readOnlyMemoryOfBytes;

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        true;

    public override bool CanWrite =>
        false;

    public override long Length =>
        readOnlyMemoryOfBytes.Length;

    public override long Position
    {
        get => position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, readOnlyMemoryOfBytes.Length, nameof(value));
            position = value;
        }
    }

    public override void Flush()
    {
        // no op
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        if (buffer.Length - offset < count)
            throw new ArgumentException("invalid offset and length");
        if (position >= readOnlyMemoryOfBytes.Length)
            return 0;
        var positionAsInt = (int)position;
        var result = Math.Min(readOnlyMemoryOfBytes.Length - positionAsInt, count);
        readOnlyMemoryOfBytes.Span.Slice(positionAsInt, result).CopyTo(buffer.AsSpan(offset, result));
        position += result;
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => readOnlyMemoryOfBytes.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        ArgumentOutOfRangeException.ThrowIfNegative(newPosition, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, readOnlyMemoryOfBytes.Length, nameof(offset));
        position = newPosition;
        return position;
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
