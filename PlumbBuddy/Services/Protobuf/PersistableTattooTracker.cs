using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableTattooTracker :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(1, Name = @"body_type_tattoo_data")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<TattooData> BodyTypeTattooDatas { get; } = [];

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
