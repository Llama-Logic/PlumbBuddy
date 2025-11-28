using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class HouseholdData :
    IExtensible
{
    IExtension? extensionData;
    ulong? homeZone;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(2, Name = "household_id", DataFormat = DataFormat.FixedSize, IsRequired = true)]
    public ulong HouseholdId { get; set; }

    [ProtoMember(3, Name = "name")]
    [DefaultValue("")]
    public string Name { get; set; } = "";

    [ProtoMember(4, Name = @"home_zone", DataFormat = DataFormat.FixedSize)]
    public ulong HomeZone
    {
        get => homeZone.GetValueOrDefault();
        set => homeZone = value;
    }

    public bool ShouldSerializeHomeZone() =>
        homeZone != null;

    public void ResetHomeZone() =>
        homeZone = null;
}
