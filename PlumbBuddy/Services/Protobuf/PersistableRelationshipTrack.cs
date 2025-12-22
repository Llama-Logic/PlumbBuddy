using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableRelationshipTrack :
    IExtensible
{
    IExtension? extensionData;
    ulong? trackId;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(1, Name = @"track_id")]
    public ulong TrackId
    {
        get => trackId.GetValueOrDefault();
        set => trackId = value;
    }
    public bool ShouldSerializeTrackId() =>
        trackId != null;

    public void ResetTrackId() =>
        trackId = null;
}
