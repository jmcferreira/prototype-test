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
/// Guardian "Guardian": End of turn, if the unit didn't Attack → gain Block +2.
/// Rewards defensive play and positioning over aggression.
/// </summary>
public class GuardianPassive : PassiveSkill
{
    public GuardianPassive()
        : base("Guardian", "If you didn't Attack this turn, gain Block +2.") { }

    public override bool TryActivate(Unit owner, List<Unit> allUnits, HexGrid grid)
    {
        if (owner.DidAttackThisTurn) return false;

        owner.AddBlock(2);
        Debug.Log($"  Passive [{Name}]: {owner.DisplayName} gains Block +2.");
        BattleLog.AddAction($"Guardian: Block +2");
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
/// Spider "Horde": End of turn, if adjacent to an ally → gain Strength +1.
/// Rewards spiders for sticking together.
/// </summary>
public class HordePassive : PassiveSkill
{
    public HordePassive()
        : base("Horde", "If adjacent to an ally, gain Strength +1.") { }

    public override bool TryActivate(Unit owner, List<Unit> allUnits, HexGrid grid)
    {
        // Check if any alive ally (same team, not self) is adjacent
        bool hasAdjacentAlly = false;
        foreach (var unit in allUnits)
        {
            if (unit == owner || unit.Team != owner.Team || !unit.IsAlive) continue;
            if (owner.Coord.DistanceTo(unit.Coord) == 1)
            {
                hasAdjacentAlly = true;
                break;
            }
        }

        if (!hasAdjacentAlly) return false;

        owner.ApplyStatus(StatusEffectType.Strength, 1);
        Debug.Log($"  Passive [{Name}]: {owner.DisplayName} gains Strength +1.");
        BattleLog.AddAction($"Horde: Strength +1");
        return true;
    }
}
