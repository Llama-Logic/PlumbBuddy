using ProtoBuf;

namespace PlumbBuddy.Services.Protobuf;

[ProtoContract]
public enum RelationshipIndex
{
    [ProtoEnum(Name = @"RELATIONSHIP_MOTHER")]
    RelationshipMother = 0,
    [ProtoEnum(Name = @"RELATIONSHIP_FATHER")]
    RelationshipFather = 1,
    [ProtoEnum(Name = @"RELATIONSHIP_MOTHERS_MOM")]
    RelationshipMothersMom = 2,
    [ProtoEnum(Name = @"RELATIONSHIP_MOTHERS_FATHER")]
    RelationshipMothersFather = 3,
    [ProtoEnum(Name = @"RELATIONSHIP_FATHERS_MOM")]
    RelationshipFathersMom = 4,
    [ProtoEnum(Name = @"RELATIONSHIP_FATHERS_FATHER")]
    RelationshipFathersFather = 5,
    [ProtoEnum(Name = @"RELATIONSHIP_NONE")]
    RelationshipNone = 6,
    [ProtoEnum(Name = @"RELATIONSHIP_PARENT")]
    RelationshipParent = 7,
    [ProtoEnum(Name = @"RELATIONSHIP_SIBLING")]
    RelationshipSibling = 8,
    [ProtoEnum(Name = @"RELATIONSHIP_SPOUSE")]
    RelationshipSpouse = 9,
    [ProtoEnum(Name = @"RELATIONSHIP_FIANCE")]
    RelationshipFiance = 10,
    [ProtoEnum(Name = @"RELATIONSHIP_STEADY")]
    RelationshipSteady = 11,
    [ProtoEnum(Name = @"RELATIONSHIP_DESCENDANT")]
    RelationshipDescendant = 12,
    [ProtoEnum(Name = @"RELATIONSHIP_GRANDPARENT")]
    RelationshipGrandparent = 13,
    [ProtoEnum(Name = @"RELATIONSHIP_GRANDCHILD")]
    RelationshipGrandchild = 14,
    [ProtoEnum(Name = @"RELATIONSHIP_SIBLINGS_CHILDREN")]
    RelationshipSiblingsChildren = 15,
    [ProtoEnum(Name = @"RELATIONSHIP_PARENTS_SIBLING")]
    RelationshipParentsSibling = 16,
    [ProtoEnum(Name = @"RELATIONSHIP_COUSIN")]
    RelationshipCousin = 17,
}
