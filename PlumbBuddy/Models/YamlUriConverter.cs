using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PlumbBuddy.Models;

sealed class YamlUriConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(Uri);

    public object? ReadYaml(YamlDotNet.Core.IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            if (parser.Current is not Scalar scalar)
                return null;
            if (scalar.Value is not string uriString)
                return null;
            if (uriString.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            if ((uriString.StartsWith("'", StringComparison.OrdinalIgnoreCase) && uriString.EndsWith("'", StringComparison.OrdinalIgnoreCase)
                || uriString.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && uriString.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                && Uri.TryCreate(uriString[1..^1], UriKind.Absolute, out var quotedUri))
                return quotedUri;
            return Uri.TryCreate(uriString, UriKind.Absolute, out var uri)
                ? uri
                : null;
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
        if (value is Uri uri)
        {
            emitter.Emit(new Scalar(uri.AbsoluteUri.ToString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
