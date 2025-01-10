using ProtoBuf;

namespace PlumbBuddy.Services;

[ProtoContract]
sealed class ArchivistSaveGameData :
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
    public List<ArchivistNeighborhoodData> Neighborhoods { get; } = [];

    [ProtoMember(5, Name = "households")]
    public List<ArchivistHouseholdData> Households { get; } = [];

    [ProtoMember(7, Name = "zones")]
    public List<ArchivistZoneData> Zones { get; } = [];
}
