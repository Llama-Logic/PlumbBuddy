namespace PlumbBuddy.Services.Archival;

public readonly record struct RelationshipKey
{
    public RelationshipKey(ulong simIdA, ulong simIdB)
    {
        if (simIdA <= simIdB)
        {
            SimIdA = simIdA;
            SimIdB = simIdB;
        }
        else
        {
            SimIdA = simIdB;
            SimIdB = simIdA;
        }
    }

    public readonly ulong SimIdA;
    public readonly ulong SimIdB;
}
