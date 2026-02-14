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

    // Chosen scenario for the next stage (set by ScenarioSelectionUI, cleared after loading)
    public ScenarioDef ChosenNextScenario;

    // Healing between scenarios (base 10%, modified by HealBonus relics)
    public float HealPercent = 0.10f;

    // Relics acquired during the run
    public List<RelicDef> Relics = new();

    /// <summary>
    /// Add a relic. Immediate effects (HealBonus) are applied on acquisition.
    /// </summary>
    public void AddRelic(RelicDef relic)
    {
        Relics.Add(relic);

        // HealBonus modifies the run-level heal percent immediately
        if (relic.effect == RelicEffect.HealBonus)
            HealPercent += relic.value / 100f;

        // MaxHPBonus increases the player def's max HP permanently for the run
        if (relic.effect == RelicEffect.MaxHPBonus)
        {
            PlayerDef.maxHP += relic.value;
            PlayerCurrentHP += relic.value; // also increase current HP
        }

        UnityEngine.Debug.Log($"Relic acquired: {relic.relicName} ({relic.tier})");
    }

    /// <summary>
    /// Sum of all relic values for a given effect type.
    /// </summary>
    public int GetRelicTotal(RelicEffect effect)
    {
        int total = 0;
        foreach (var r in Relics)
            if (r.effect == effect) total += r.value;
        return total;
    }

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
    /// Get the next scenario for this run. Uses the chosen scenario if set,
    /// otherwise falls back to the default (first) choice for the stage.
    /// Returns null if the run is complete.
    /// </summary>
    public ScenarioDef GetNextScenario()
    {
        if (ChosenNextScenario != null)
            return ChosenNextScenario;
        return ScenarioPool.GetScenario(ScenariosCompleted + 1);
    }
}
