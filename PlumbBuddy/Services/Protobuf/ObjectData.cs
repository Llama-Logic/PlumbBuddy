using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class ObjectData :
    IExtensible
{
    IExtension? extensionData;
    ulong? textureId;

    [ProtoMember(20, Name = @"texture_id", DataFormat = DataFormat.FixedSize)]
    public ulong TextureId
    {
        get => textureId.GetValueOrDefault();
        set => textureId = value;
    }

    [ProtoMember(31, Name = @"unique_inventory")]
    public ObjectList? UniqueInventory { get; set; }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    public void ResetTextureId() =>
        textureId = null;

    public bool ShouldSerializeTextureId() =>
        textureId != null;
}
