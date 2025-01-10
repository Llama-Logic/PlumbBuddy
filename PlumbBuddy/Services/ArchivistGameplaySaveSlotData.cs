using ProtoBuf;

namespace PlumbBuddy.Services;

[ProtoContract]
sealed class ArchivistGameplaySaveSlotData :
    IExtensible
{
    IExtension? extensionData;

    ProtoBuf.IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(3, Name = "camera_data")]
    public ArchivistGameplayCameraData? CameraData { get; set; }
}
