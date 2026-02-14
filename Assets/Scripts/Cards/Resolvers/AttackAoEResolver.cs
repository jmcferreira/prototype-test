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
            if (unit == caster || caster.IsAlly(unit) || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
                return new List<HexCoord> { caster.Coord };
        }
        return new List<HexCoord>();
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        caster.NotifyAttacked();

        // Consume Strength once for the whole AoE
        int strBonus = CombatResolver.ConsumeAttackerBonuses(caster);
        int fateDmg = FateCombatContext.DamageBonus;

        int hitCount = 0;
        foreach (var unit in allUnits)
        {
            if (unit == caster || caster.IsAlly(unit) || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
            {
                var result = CombatResolver.ResolveHit(caster, unit, action.damage, strBonus, fateDmg);
                CombatResolver.LogHit(caster, unit, result);

                if (!result.dodged)
                {
                    CombatResolver.ApplyHitStatus(unit, action);
                    CombatResolver.ApplyFateStatuses(caster, unit);
                }

                hitCount++;
            }
        }
        Debug.Log($"AoE: {caster.DisplayName} hit {hitCount} target(s) total.");
    }
}
