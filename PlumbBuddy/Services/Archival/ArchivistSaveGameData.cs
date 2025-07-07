using ProtoBuf;

namespace PlumbBuddy.Services.Archival;

[ProtoContract]
public sealed class ArchivistSaveGameData :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(2, Name = "save_slot")]
    public ArchivistSaveSlotData? SaveSlot { get; set; }

    [ProtoMember(3, Name = "account")]
    public ArchivistAccountData? Account { get; set; }

    [ProtoMember(4, Name = "neighborhoods")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<ArchivistNeighborhoodData> Neighborhoods { get; } = [];

    [ProtoMember(5, Name = "households")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<ArchivistHouseholdData> Households { get; } = [];

    [ProtoMember(7, Name = "zones")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<ArchivistZoneData> Zones { get; } = [];
}
