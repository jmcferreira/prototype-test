/// <summary>
/// Layout definition for a PVP arena: grid size, tower positions, spawn zones.
/// Symmetric layout: P1 on bottom, P2 on top.
/// </summary>
[System.Serializable]
public class PvpArenaDef
{
    public int gridColumns = 11;
    public int gridRows = 11;

    // Base towers (destroy to win)
    public HexCoord p1BaseTowerPos;
    public HexCoord p2BaseTowerPos;
    public int baseTowerHP = 20;

    // Guard towers (2 per player — provide gold and shoot enemies)
    public HexCoord[] p1GuardTowerPositions;
    public HexCoord[] p2GuardTowerPositions;
    public int guardTowerHP = 10;
    public int guardTowerDamage = 2;
    public int guardTowerRange = 2;
    public int guardTowerGoldPerRound = 2;

    // Champion spawn positions
    public HexCoord p1ChampionSpawn;
    public HexCoord p2ChampionSpawn;

    // Creature spawn zones (hexes adjacent to each base tower)
    // Filled at runtime from base tower position

    // Economy
    public int startingGold = 5;

    // Rounds
    public int cyclesPerRound = 3;
}

/// <summary>
/// Provides the default PVP arena layout.
/// </summary>
public static class PvpArenaLayouts
{
    public static PvpArenaDef CreateDefaultArena()
    {
        return new PvpArenaDef
        {
            gridColumns = 11,
            gridRows = 11,

            // Base towers in center of each side
            p1BaseTowerPos = new HexCoord(5, 1),
            p2BaseTowerPos = new HexCoord(5, 9),
            baseTowerHP = 20,

            // Guard towers flanking each base
            p1GuardTowerPositions = new[] { new HexCoord(3, 2), new HexCoord(7, 2) },
            p2GuardTowerPositions = new[] { new HexCoord(3, 8), new HexCoord(7, 8) },
            guardTowerHP = 10,
            guardTowerDamage = 2,
            guardTowerRange = 2,
            guardTowerGoldPerRound = 2,

            // Champions spawn near their base
            p1ChampionSpawn = new HexCoord(5, 0),
            p2ChampionSpawn = new HexCoord(5, 10),

            startingGold = 5,
            cyclesPerRound = 3,
        };
    }
}
