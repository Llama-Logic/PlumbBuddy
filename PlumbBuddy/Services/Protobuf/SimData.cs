using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class SimData :
    IExtensible
{
    ulong? customTexture;
    IExtension? extensionData;
    string? firstName;
    ulong? householdId;
    string? lastName;

    [ProtoMember(1, Name = @"sim_id", DataFormat = DataFormat.FixedSize, IsRequired = true)]
    public ulong SimId { get; set; }

    [ProtoMember(4, Name = @"household_id", DataFormat = DataFormat.FixedSize)]
    public ulong HouseholdId
    {
        get => householdId.GetValueOrDefault();
        set => householdId = value;
    }

    [ProtoMember(5, Name = @"first_name")]
    [DefaultValue("")]
    public string FirstName
    {
        get => firstName ?? "";
        set => firstName = value;
    }

    [ProtoMember(6, Name = @"last_name")]
    [DefaultValue("")]
    public string LastName
    {
        get => lastName ?? "";
        set => lastName = value;
    }

    [ProtoMember(20, Name = @"inventory")]
    public ObjectList? Inventory { get; set; }

    [ProtoMember(30, Name = @"attributes")]
    public PersistableSimInfoAttributes? Attributes { get; set; }

    [ProtoMember(62, Name = @"custom_texture")]
    public ulong CustomTexture
    {
        get => customTexture.GetValueOrDefault();
        set => customTexture = value;
    }

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    public void ResetCustomTexture() =>
        customTexture = null;

    public void ResetFirstName() =>
        firstName = null;

    public void ResetHouseholdId() =>
        householdId = null;

    public void ResetLastName() =>
        lastName = null;

    public bool ShouldSerializeCustomTexture() =>
        customTexture != null;

    public bool ShouldSerializeFirstName() =>
        firstName != null;

    public bool ShouldSerializeHouseholdId() =>
        householdId != null;

    public bool ShouldSerializeLastName() =>
        lastName != null;
}
