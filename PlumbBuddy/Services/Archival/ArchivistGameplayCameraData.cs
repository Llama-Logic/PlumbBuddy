using ProtoBuf;

namespace PlumbBuddy.Services.Archival;

[ProtoContract]
public sealed class ArchivistGameplayCameraData :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(5, Name = "zone_id", DataFormat = DataFormat.FixedSize)]
    public ulong ZoneId { get; set; }
}
