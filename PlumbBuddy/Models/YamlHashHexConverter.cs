using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PlumbBuddy.Models;

sealed class YamlHashHexConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(ImmutableArray<byte>);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            if (parser.Current is not Scalar scalar)
                return null;
            if (scalar.Value is not string hashHexString)
                return null;
            if (hashHexString.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            if (hashHexString.StartsWith("'", StringComparison.OrdinalIgnoreCase)
                && hashHexString.EndsWith("'", StringComparison.OrdinalIgnoreCase)
                || hashHexString.StartsWith("\"", StringComparison.OrdinalIgnoreCase)
                && hashHexString.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                hashHexString = hashHexString[1..^1];
            return hashHexString.Length > 0
                ? hashHexString.ToByteSequence().ToImmutableArray()
                : [];
        }
        finally
        {
            parser.MoveNext();
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }
        if (value is IEnumerable<byte> byteSequence)
        {
            emitter.Emit(new Scalar(byteSequence.ToHexString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
