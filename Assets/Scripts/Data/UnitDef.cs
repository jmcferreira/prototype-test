/// <summary>
/// Data definition for a unit: stats, deck, passive, rewards.
/// Used by ScenarioDef to describe what to spawn.
/// </summary>
[System.Serializable]
public class UnitDef
{
    public string displayName;
    public int maxHP;
    public string iconLetter;
    public string deckId;
    public PassiveType passiveType;
    public int xpReward;
    public int goldReward;
}
