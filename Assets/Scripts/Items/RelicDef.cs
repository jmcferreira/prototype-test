/// <summary>
/// Definition of a relic — a persistent passive bonus for the run.
/// </summary>
[System.Serializable]
public class RelicDef
{
    public string id;
    public string relicName;
    public string description;
    public ItemTier tier;
    public RelicEffect effect;
    public int value;
}
