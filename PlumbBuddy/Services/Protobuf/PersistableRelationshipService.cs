using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableRelationshipService :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(1, Name = @"relationships")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<PersistableServiceRelationship> Relationships { get; } = [];

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
