using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableServiceRelationship :
    IExtensible
{
    IExtension? extensionData;
    ulong? simIdA;
    ulong? simIdB;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(1, Name = @"sim_id_a")]
    public ulong SimIdA
    {
        get => simIdA.GetValueOrDefault();
        set => simIdA = value;
    }

    public bool ShouldSerializeSimIdA() =>
        simIdA != null;

    public void ResetSimIdA() =>
        simIdA = null;

    [ProtoMember(2, Name = @"sim_id_b")]
    public ulong SimIdB
    {
        get => simIdB.GetValueOrDefault();
        set => simIdB = value;
    }

    public bool ShouldSerializeSimIdB() =>
        simIdB != null;

    public void ResetSimIdB() =>
        simIdB = null;

    [ProtoMember(3, Name = @"bidirectional_relationship_data")]
    public PersistableBidirectionalRelationshipData? BidirectionalRelationshipData { get; set; }
}
