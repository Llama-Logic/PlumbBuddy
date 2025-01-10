using ProtoBuf;

namespace PlumbBuddy.Services;

[ProtoContract]
sealed class ArchivistSaveSlotData :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(11, Name = @"active_household_id")]
    public ulong ActiveHouseholdId { get; set; }

    [ProtoMember(8, Name = "gameplay_data")]
    public ArchivistGameplaySaveSlotData? GameplayData { get; set; }

    [ProtoMember(1, Name = "slot_id", DataFormat = DataFormat.FixedSize)]
    public ulong SlotId { get; set; }

    [ProtoMember(9, Name = "slot_name")]
    [DefaultValue("")]
    public string SlotName { get; set; } = "";
}
