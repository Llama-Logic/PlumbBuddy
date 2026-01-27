using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class FamilyRelation :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(1, Name = @"relation_type", IsRequired = true)]
    public RelationshipIndex RelationType { get; set; }

    [ProtoMember(2, Name = @"sim_id", IsRequired = true)]
    public ulong SimId { get; set; }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
