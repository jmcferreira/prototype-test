using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reduces the cooldown of a specific card in the caster's hand.
/// Auto-targets the caster (like self-buffing status effects).
/// Uses action.targetCardIndex to identify which card to affect.
/// </summary>
public class ReduceCooldownResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        // Auto-target: always valid, targets the caster's own coord
        return new List<HexCoord> { caster.Coord };
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        caster.ReduceCardCooldown(action.targetCardIndex, 1);
        BattleLog.AddAction("Haste -1 CD");
    }
}
