using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class SaveGameData :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(2, Name = "save_slot")]
    public SaveSlotData? SaveSlot { get; set; }

    [ProtoMember(3, Name = "account")]
    public AccountData? Account { get; set; }

    [ProtoMember(4, Name = "neighborhoods")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<NeighborhoodData> Neighborhoods { get; } = [];

    [ProtoMember(5, Name = "households")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<HouseholdData> Households { get; } = [];

    [ProtoMember(6, Name = @"sims")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<SimData> Sims { get; } = [];

    [ProtoMember(7, Name = "zones")]
    [SuppressMessage("Design", "CA1002: Do not expose generic lists", Justification = "Take it up with protobuf.net")]
    public List<ZoneData> Zones { get; } = [];

    [ProtoMember(30, Name = @"attributes")]
    public PersistableSimInfoAttributes? Attributes { get; set; }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
