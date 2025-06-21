using ProtoBuf;

namespace PlumbBuddy.Services.Archival;

[ProtoContract]
public sealed class ArchivistGameplaySaveSlotData :
    IExtensible
{
    IExtension? extensionData;
    ulong? worldGameTime;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(3, Name = "camera_data")]
    public ArchivistGameplayCameraData? CameraData { get; set; }

    [ProtoMember(1, Name = "world_game_time")]
    public ulong WorldGameTime
    {
        get => worldGameTime.GetValueOrDefault();
        set => worldGameTime = value;
    }

    public bool ShouldSerializeWorldGameTime() =>
        worldGameTime is not null;

    public void ResetWorldGameTime() =>
        worldGameTime = null;
}
