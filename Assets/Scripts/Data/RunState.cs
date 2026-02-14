using System.Collections.Generic;

/// <summary>
/// Persistent state that carries across scenarios within a single run.
/// Created once at run start, survives scenario transitions.
/// </summary>
public class RunState
{
    public static RunState Current { get; set; }

    // Player state
    public UnitDef PlayerDef;
    public int PlayerCurrentHP;
    public FateCardData[] PlayerFateDeck;

    // Run progress
    public int ScenariosCompleted;
    public int TotalGold;
    public int TotalXP;

    // Healing between scenarios (base 10%, can be modified by relics later)
    public float HealPercent = 0.10f;

    /// <summary>
    /// Create a new run from a starting player definition.
    /// </summary>
    public static RunState NewRun(UnitDef playerDef)
    {
        var run = new RunState
        {
            PlayerDef = playerDef,
            PlayerCurrentHP = playerDef.maxHP,
            PlayerFateDeck = FateCardLibrary.GetStarterDeck(),
            ScenariosCompleted = 0,
            TotalGold = 0,
            TotalXP = 0,
        };
        Current = run;
        return run;
    }

    /// <summary>
    /// Called after winning a scenario. Heals the player and increments progress.
    /// </summary>
    public void OnScenarioWon(int playerHPAfterBattle, int goldEarned, int xpEarned)
    {
        ScenariosCompleted++;
        TotalGold += goldEarned;
        TotalXP += xpEarned;

        // Heal 10% (rounded up, at least 1 HP)
        int healAmount = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.CeilToInt(PlayerDef.maxHP * HealPercent));
        PlayerCurrentHP = UnityEngine.Mathf.Min(playerHPAfterBattle + healAmount, PlayerDef.maxHP);
    }

    /// <summary>
    /// Get the next scenario for this run. Returns null if the run is complete.
    /// </summary>
    public ScenarioDef GetNextScenario()
    {
        return ScenarioPool.GetScenario(ScenariosCompleted + 1);
    }
}
