using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public sealed class SimData :
    IExtensible
{
    IExtension? extensionData;
    string? firstName;
    ulong? householdId;
    string? lastName;

    IExtension IExtensible.GetExtensionObject(bool createIfMissing) =>
        Extensible.GetExtensionObject(ref extensionData, createIfMissing);

    [ProtoMember(1, Name = @"sim_id", DataFormat = DataFormat.FixedSize, IsRequired = true)]
    public ulong SimId { get; set; }

    [ProtoMember(4, Name = @"household_id", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
    public ulong HouseholdId
    {
        get => householdId.GetValueOrDefault();
        set => householdId = value;
    }

    public bool ShouldSerializeHouseholdId() =>
        householdId != null;

    public void ResetHouseholdId() =>
        householdId = null;

    [ProtoMember(5, Name = @"first_name")]
    [DefaultValue("")]
    public string FirstName
    {
        get => firstName ?? "";
        set => firstName = value;
    }

    public bool ShouldSerializeFirstName() =>
        firstName != null;

    public void ResetFirstName() =>
        firstName = null;

    [ProtoMember(6, Name = @"last_name")]
    [DefaultValue("")]
    public string LastName
    {
        get => lastName ?? "";
        set => lastName = value;
    }

    public bool ShouldSerializeLastName() =>
        lastName != null;

    public void ResetLastName() =>
        lastName = null;

    [ProtoMember(30, Name = @"attributes")]
    public PersistableSimInfoAttributes? Attributes { get; set; }
}
