using ProtoBuf;

namespace PlumbBuddy.Services;

[ProtoContract]
sealed class ArchivistHouseholdData :
    IExtensible
{
    IExtension? extensionData;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(2, Name = "household_id", DataFormat = DataFormat.FixedSize, IsRequired = true)]
    public ulong HouseholdId { get; set; }

    [ProtoMember(3, Name = "name")]
    [DefaultValue("")]
    public string Name { get; set; } = "";
}
