using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class TattooData :
    IExtensible
{
    ulong? bodyPartCustomTexture;
    IExtension? extensionData;

    [ProtoMember(7, Name = @"body_part_custom_texture")]
    public ulong BodyPartCustomTexture
    {
        get => bodyPartCustomTexture.GetValueOrDefault();
        set => bodyPartCustomTexture = value;
    }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    public void ResetBodyPartCustomTexture() =>
        bodyPartCustomTexture = null;

    public bool ShouldSerializeBodyPartCustomTexture() =>
        bodyPartCustomTexture != null;
}
