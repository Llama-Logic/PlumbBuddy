namespace PlumbBuddy;

sealed class ReadOnlyMemoryOfByteStream :
    Stream
{
    public ReadOnlyMemoryOfByteStream(ReadOnlyMemory<byte> readOnlyMemoryOfBytes) =>
        Memory = readOnlyMemoryOfBytes;

    long position;

    public override bool CanRead =>
        true;

    public override bool CanSeek =>
        true;

    public override bool CanWrite =>
        false;

    public override long Length =>
        Memory.Length;

    public ReadOnlyMemory<byte> Memory { get; }

    public override long Position
    {
        get => position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Memory.Length, nameof(value));
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
        if (position >= Memory.Length)
            return 0;
        var positionAsInt = (int)position;
        var result = Math.Min(Memory.Length - positionAsInt, count);
        Memory.Span.Slice(positionAsInt, result).CopyTo(buffer.AsSpan(offset, result));
        position += result;
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => Memory.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        ArgumentOutOfRangeException.ThrowIfNegative(newPosition, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, Memory.Length, nameof(offset));
        position = newPosition;
        return position;
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
