/// <summary>
/// A recruitable creature within a faction, with its cost tier.
/// </summary>
[System.Serializable]
public class CreatureEntry
{
    public UnitDef unitDef;
    public int goldCost;

    public CreatureEntry(UnitDef def, int cost)
    {
        unitDef = def;
        goldCost = cost;
    }
}

/// <summary>
/// Definition of a PVP faction: champion stats/deck and available creatures.
/// </summary>
[System.Serializable]
public class FactionDef
{
    public string factionId;
    public string factionName;
    public UnitDef championDef;
    public CreatureEntry[] creatures;
}
