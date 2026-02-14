/// <summary>
/// Complete definition of a battle scenario: grid size, units, loot, win condition.
/// Fed into GameSetup to build the game from data instead of hardcoded values.
/// </summary>
[System.Serializable]
public class ScenarioDef
{
    public string scenarioName;
    public int gridColumns = 7;
    public int gridRows = 7;

    // Player spawn positions (one per party member)
    public HexCoord[] playerSpawns;

    // Enemies
    public EnemySpawn[] enemySpawns;

    // Loot
    public int initialLootGold = 5;

    // Win condition
    public WinConditionType winCondition = WinConditionType.DefeatAll;
    public int winConditionParam;

    // Difficulty
    public int threatLevel = 1;
}
