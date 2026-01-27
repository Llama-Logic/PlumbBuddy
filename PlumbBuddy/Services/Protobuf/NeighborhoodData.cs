using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class NeighborhoodData :
    IExtensible
{
    IExtension? extensionData;

    [ProtoMember(1, Name = @"neighborhood_id", DataFormat = DataFormat.FixedSize)]
    public ulong NeighborhoodId { get; set; }

    [ProtoMember(3, Name = @"name")]
    [DefaultValue("")]
    public string Name { get; set; } = "";

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);
}
