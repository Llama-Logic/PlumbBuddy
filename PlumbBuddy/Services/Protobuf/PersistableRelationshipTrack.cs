using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class PersistableRelationshipTrack :
    IExtensible
{
    IExtension? extensionData;
    ulong? trackId;
    float? value;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(1, Name = @"track_id")]
    public ulong TrackId
    {
        get => trackId.GetValueOrDefault();
        set => trackId = value;
    }

    [ProtoMember(2, Name = @"value")]
    public float Value
    {
        get => value.GetValueOrDefault();
        set => this.value = value;
    }

    public void ResetTrackId() =>
        trackId = null;

    public void ResetValue() =>
        value = null;

    public bool ShouldSerializeTrackId() =>
        trackId != null;

    public bool ShouldSerializeValue() =>
        value != null;
}
