namespace PlumbBuddy;

sealed class ArrayBufferWriterOfByteStream :
    Stream
{
    readonly ArrayBufferWriter<byte> writer = new();
    long? setLength;

    public override bool CanRead =>
        false;

    public override bool CanSeek =>
        false;

    public override bool CanWrite =>
        true;

    public override long Length =>
        setLength is { } nonNullSetLength ? nonNullSetLength : writer.WrittenCount;

    public override long Position
    {
        get => setLength is { } nonNullSetLength && nonNullSetLength < writer.WrittenCount ? nonNullSetLength : writer.WrittenCount;
        set => throw new NotSupportedException();
    }

    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            if (setLength is not { } nonNullSetLength)
                return writer.WrittenMemory;
            if (nonNullSetLength < writer.WrittenCount)
                return writer.WrittenMemory[..(int)nonNullSetLength];
            Memory<byte> result = new byte[nonNullSetLength];
            writer.WrittenMemory.CopyTo(result);
            return result;
        }
    }

    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            if (setLength is not { } nonNullSetLength)
                return writer.WrittenSpan;
            if (nonNullSetLength < writer.WrittenCount)
                return writer.WrittenSpan[..(int)nonNullSetLength];
            Span<byte> result = new byte[nonNullSetLength];
            writer.WrittenSpan.CopyTo(result);
            return result;
        }
    }

    public override void Flush()
    {
        // no op
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        setLength = value;

    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        if (buffer.Length - offset < count)
            throw new ArgumentException("invalid offset and length");
        writer.Write(buffer.AsSpan(offset, count));
    }
}
