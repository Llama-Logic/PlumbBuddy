using EA.Sims4.Persistence;
using ProtoBuf;

namespace PlumbBuddy.Services;

[ProtoContract]
sealed class ArchivistAccountData :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(4, Name = "created")]
    public ulong Created { get; set; }

    [ProtoMember(1, Name = "nucleus_id", DataFormat = DataFormat.FixedSize, IsRequired = true)]
    public ulong NucleusId { get; set; }

    [ProtoMember(2, Name = "persona_name", IsRequired = true)]
    public string PersonaName { get; set; } = "";
}
