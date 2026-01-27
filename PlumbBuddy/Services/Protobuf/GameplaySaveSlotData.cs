using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class GameplaySaveSlotData :
    IExtensible
{
    IExtension? extensionData;
    ulong? worldGameTime;

    [ProtoMember(3, Name = "camera_data")]
    public GameplayCameraData? CameraData { get; set; }

    [ProtoMember(1, Name = "world_game_time")]
    public ulong WorldGameTime
    {
        get => worldGameTime.GetValueOrDefault();
        set => worldGameTime = value;
    }

    [ProtoMember(13, Name = @"relationship_service")]
    public PersistableRelationshipService? RelationshipService { get; set; }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    public void ResetWorldGameTime() =>
        worldGameTime = null;

    public bool ShouldSerializeWorldGameTime() =>
        worldGameTime is not null;
}
