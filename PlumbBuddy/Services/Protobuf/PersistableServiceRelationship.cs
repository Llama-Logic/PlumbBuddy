using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableServiceRelationship :
    IExtensible
{
    IExtension? extensionData;
    ulong? simIdA;
    ulong? simIdB;

    [ProtoMember(1, Name = @"sim_id_a")]
    public ulong SimIdA
    {
        get => simIdA.GetValueOrDefault();
        set => simIdA = value;
    }

    [ProtoMember(2, Name = @"sim_id_b")]
    public ulong SimIdB
    {
        get => simIdB.GetValueOrDefault();
        set => simIdB = value;
    }

    [ProtoMember(3, Name = @"bidirectional_relationship_data")]
    public PersistableBidirectionalRelationshipData? BidirectionalRelationshipData { get; set; }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    public void ResetSimIdA() =>
        simIdA = null;

    public bool ShouldSerializeSimIdA() =>
        simIdA != null;

    public void ResetSimIdB() =>
        simIdB = null;

    public bool ShouldSerializeSimIdB() =>
        simIdB != null;
}
