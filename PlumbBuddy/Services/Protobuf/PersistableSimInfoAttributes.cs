using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableSimInfoAttributes :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(14, Name = @"genealogy_tracker")]
    public PersistableGenealogyTracker? GenealogyTracker { get; set; }

    [ProtoMember(37, Name = @"tattoo_tracker")]
    public PersistableTattooTracker? TattooTracker { get; set; }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
