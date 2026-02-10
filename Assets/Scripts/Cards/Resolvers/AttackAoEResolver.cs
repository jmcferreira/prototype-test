using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AttackAoE: damage all enemy units within range of the caster.
/// Valid target is the caster's own hex (AoE centered on self).
/// Only available if at least one enemy is within range.
/// </summary>
public class AttackAoEResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        // Check if any enemy is in range
        foreach (var unit in allUnits)
        {
            if (unit == caster || unit.Team == caster.Team || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
                return new List<HexCoord> { caster.Coord };
        }
        return new List<HexCoord>();
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        int hitCount = 0;
        foreach (var unit in allUnits)
        {
            if (unit == caster || unit.Team == caster.Team || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
            {
                // Consume Burn: bonus damage equal to stacks, then remove all
                int burnStacks = unit.ConsumeStatus(StatusEffectType.Burn);
                int dmg = action.damage + burnStacks;

                unit.TakeHit(dmg);
                hitCount++;
                Debug.Log($"AoE: {caster.DisplayName} hit {unit.DisplayName} for {dmg} damage" +
                          (burnStacks > 0 ? $" (+{burnStacks} Burn)." : "."));
            }
        }
        Debug.Log($"AoE: {caster.DisplayName} hit {hitCount} target(s) total.");
    }
}
