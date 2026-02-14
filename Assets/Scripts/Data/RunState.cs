using System.Collections.Generic;

/// <summary>
/// Persistent state that carries across scenarios within a single run.
/// Supports multiple player characters (party). Created once at run start.
/// </summary>
public class RunState
{
    public static RunState Current { get; set; }

    // Party state (one entry per player character)
    public UnitDef[] PlayerDefs;
    public int[] PlayerCurrentHPs;
    public FateCardData[][] PlayerFateDecks;

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

    /// <summary>Number of player characters in the party.</summary>
    public int PartySize => PlayerDefs?.Length ?? 0;

    /// <summary>
    /// Add a relic. Immediate effects (HealBonus, MaxHPBonus) are applied on acquisition.
    /// MaxHPBonus applies to ALL party members.
    /// </summary>
    public void AddRelic(RelicDef relic)
    {
        Relics.Add(relic);

        if (relic.effect == RelicEffect.HealBonus)
            HealPercent += relic.value / 100f;

        if (relic.effect == RelicEffect.MaxHPBonus)
        {
            for (int i = 0; i < PartySize; i++)
            {
                PlayerDefs[i].maxHP += relic.value;
                PlayerCurrentHPs[i] += relic.value;
            }
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
    /// Create a new run with a party of player characters.
    /// </summary>
    public static RunState NewRun(UnitDef[] playerDefs)
    {
        var hps = new int[playerDefs.Length];
        var fateDecks = new FateCardData[playerDefs.Length][];
        for (int i = 0; i < playerDefs.Length; i++)
        {
            hps[i] = playerDefs[i].maxHP;
            fateDecks[i] = FateCardLibrary.GetStarterDeck();
        }

        var run = new RunState
        {
            PlayerDefs = playerDefs,
            PlayerCurrentHPs = hps,
            PlayerFateDecks = fateDecks,
            ScenariosCompleted = 0,
            TotalGold = 0,
            TotalXP = 0,
        };
        Current = run;
        return run;
    }

    /// <summary>
    /// Called after winning a scenario. Heals all party members and increments progress.
    /// </summary>
    public void OnScenarioWon(int[] playerHPsAfterBattle, int goldEarned, int xpEarned)
    {
        ScenariosCompleted++;
        TotalGold += goldEarned;
        TotalXP += xpEarned;

        for (int i = 0; i < PartySize; i++)
        {
            int healAmount = UnityEngine.Mathf.Max(1,
                UnityEngine.Mathf.CeilToInt(PlayerDefs[i].maxHP * HealPercent));
            PlayerCurrentHPs[i] = UnityEngine.Mathf.Min(
                playerHPsAfterBattle[i] + healAmount, PlayerDefs[i].maxHP);
        }
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
