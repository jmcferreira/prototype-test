using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for passive skills. Each unit can have one passive skill
/// that activates automatically when trigger conditions are met.
/// Subclass and override TryActivate to implement specific passives.
/// </summary>
public abstract class PassiveSkill
{
    public string Name { get; }
    public string Description { get; }

    protected PassiveSkill(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Called at end of the unit's turn. Returns true if the passive activated.
    /// </summary>
    public abstract bool TryActivate(Unit owner, List<Unit> allUnits, HexGrid grid);
}

/// <summary>
/// Player "Setup": End of turn, if the unit didn't Move → gain Strength +1.
/// </summary>
public class SetupPassive : PassiveSkill
{
    public SetupPassive()
        : base("Setup", "If you didn't Move this turn, gain Strength +1.") { }

    public override bool TryActivate(Unit owner, List<Unit> allUnits, HexGrid grid)
    {
        if (owner.DidMoveThisTurn) return false;

        owner.ApplyStatus(StatusEffectType.Strength, 1);
        Debug.Log($"  Passive [{Name}]: {owner.DisplayName} gains Strength +1.");
        BattleLog.AddAction($"Setup: Strength +1");
        return true;
    }
}

/// <summary>
/// Cultist "Dark Pact": End of turn, if the unit was hit this turn → gain Strength 1.
/// Punishes the player for focusing the Cultist.
/// </summary>
public class DarkPactPassive : PassiveSkill
{
    public DarkPactPassive()
        : base("Dark Pact", "If hit this turn, gain Strength +1.") { }

    public override bool TryActivate(Unit owner, List<Unit> allUnits, HexGrid grid)
    {
        if (!owner.WasHitThisTurn) return false;

        owner.ApplyStatus(StatusEffectType.Strength, 1);
        Debug.Log($"  Passive [{Name}]: {owner.DisplayName} gains Strength +1.");
        BattleLog.AddAction($"Dark Pact: Strength +1");
        return true;
    }
}

/// <summary>
/// Spider "Nesting": End of turn, if the unit didn't Attack → place a web token
/// on a random adjacent unoccupied hex.
/// </summary>
public class NestingPassive : PassiveSkill
{
    public NestingPassive()
        : base("Nesting", "If you didn't Attack this turn, place a Web on an adjacent hex.") { }

    public override bool TryActivate(Unit owner, List<Unit> allUnits, HexGrid grid)
    {
        if (owner.DidAttackThisTurn) return false;

        // Find adjacent hexes that are on-grid, unoccupied, and don't already have a web
        var candidates = new List<HexCoord>();
        for (int dir = 0; dir < 6; dir++)
        {
            HexCoord adj = owner.Coord.Neighbor(dir);
            if (!grid.TryGetTile(adj, out _)) continue;

            // Skip occupied hexes
            bool occupied = false;
            foreach (var u in allUnits)
            {
                if (u.IsAlive && u.Coord == adj) { occupied = true; break; }
            }
            if (occupied) continue;

            // Skip hexes that already have a token
            if (TokenManager.Instance != null && TokenManager.Instance.HasToken(adj)) continue;

            candidates.Add(adj);
        }

        if (candidates.Count == 0) return false;

        HexCoord chosen = candidates[Random.Range(0, candidates.Count)];
        TokenManager.Instance?.PlaceToken(new WebToken(owner), chosen, grid);
        Debug.Log($"  Passive [{Name}]: {owner.DisplayName} placed a Web at {chosen}.");
        BattleLog.AddAction($"Nesting: placed Web at {chosen}");
        return true;
    }
}
