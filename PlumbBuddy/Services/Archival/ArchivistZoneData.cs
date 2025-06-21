using ProtoBuf;

namespace PlumbBuddy.Services.Archival;

[ProtoContract]
public sealed class ArchivistZoneData :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(2, Name = "name")]
    [DefaultValue("")]
    public string Name { get; set; } = "";

    [ProtoMember(10, Name = "neighborhood_id", DataFormat = DataFormat.FixedSize)]
    public ulong NeighborhoodId { get; set; }

    [ProtoMember(1, Name = "zone_id", DataFormat = DataFormat.FixedSize)]
    public ulong ZoneId { get; set; }
}
