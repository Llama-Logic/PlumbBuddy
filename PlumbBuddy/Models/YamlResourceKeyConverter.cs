using YamlDotNet.Core.Events;

namespace PlumbBuddy.Models;

sealed class YamlResourceKeyConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(ResourceKey);

    public object? ReadYaml(YamlDotNet.Core.IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            if (parser.Current is not Scalar scalar)
                return null;
            if (scalar.Value is not string resourceKeyString)
                return null;
            if (resourceKeyString.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            if ((resourceKeyString.StartsWith("'", StringComparison.OrdinalIgnoreCase) && resourceKeyString.EndsWith("'", StringComparison.OrdinalIgnoreCase)
                || resourceKeyString.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && resourceKeyString.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                && ResourceKey.TryParse(resourceKeyString[1..^1], out var quotedResourceKey))
                return quotedResourceKey;
            return ResourceKey.TryParse(resourceKeyString, out var resourceKey)
                ? resourceKey
                : (object?)null;
        }
        finally
        {
            parser.MoveNext();
        }
    }

    public void WriteYaml(YamlDotNet.Core.IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }
        if (value is ResourceKey resourceKey)
        {
            emitter.Emit(new Scalar(resourceKey.ToString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
