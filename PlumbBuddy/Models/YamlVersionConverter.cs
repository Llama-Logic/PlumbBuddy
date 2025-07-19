using YamlDotNet.Core.Events;

namespace PlumbBuddy.Models;

sealed class YamlVersionConverter :
    IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(Version);

    public object? ReadYaml(YamlDotNet.Core.IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        try
        {
            if (parser.Current is not Scalar scalar)
                return null;
            if (scalar.Value is not string versionString)
                return null;
            if (versionString.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            if ((versionString.StartsWith("'", StringComparison.OrdinalIgnoreCase) && versionString.EndsWith("'", StringComparison.OrdinalIgnoreCase)
                || versionString.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && versionString.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                && Version.TryParse(versionString[1..^1], out var quotedVersion))
                return quotedVersion;
            return Version.TryParse(versionString, out var version)
                ? version
                : null;
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
        if (value is Version version)
        {
            emitter.Emit(new Scalar(version.ToString()));
            return;
        }
        throw new NotSupportedException($"{value} ({value.GetType().FullName}) is not supported");
    }
}
