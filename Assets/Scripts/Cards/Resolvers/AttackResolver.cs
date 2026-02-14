using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack: deal card.damage to a single enemy unit within card.range hex distance.
/// Supports optional status-on-hit and max-range bonus damage.
/// Multi-target is handled by the unit flow calling Resolve multiple times.
/// </summary>
public class AttackResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();
        // Blind prevents targeting enemies
        if (caster.HasStatus(StatusEffectType.Blind))
            return targets;

        foreach (var unit in allUnits)
        {
            if (unit == caster || unit.Team == caster.Team || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
                targets.Add(unit.Coord);
        }
        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        foreach (var unit in allUnits)
        {
            if (unit.Coord == target && unit != caster && unit.IsAlive)
            {
                int baseDmg = action.damage;
                if (action.bonusDamageAtMaxRange && caster.Coord.DistanceTo(target) >= action.range)
                    baseDmg += action.bonusDamage;

                caster.NotifyAttacked();
                int strBonus = CombatResolver.ConsumeAttackerBonuses(caster);
                int fateDmg = FateCombatContext.DamageBonus;

                var result = CombatResolver.ResolveHit(caster, unit, baseDmg, strBonus, fateDmg);
                CombatResolver.LogHit(caster, unit, result);

                if (!result.dodged)
                {
                    CombatResolver.ApplyHitStatus(unit, action);
                    CombatResolver.ApplyFateStatuses(caster, unit);
                }

                break;
            }
        }
    }
}
