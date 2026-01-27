using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class ObjectList :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(1, Name = @"objects")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<ObjectData> Objects { get; } = [];
}
