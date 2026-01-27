using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableGenealogyTracker :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(1, Name = @"family_relations")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<FamilyRelation> FamilyRelations { get; } = [];

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
