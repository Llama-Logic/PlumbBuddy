using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class ZoneObjectData :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(3, Name = @"objects")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<ObjectList> Objects { get; } = [];

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
