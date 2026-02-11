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
                // Calculate damage with optional max-range bonus
                int dmg = action.damage;
                if (action.bonusDamageAtMaxRange && caster.Coord.DistanceTo(target) >= action.range)
                    dmg += action.bonusDamage;

                caster.NotifyAttacked();

                // Consume Strength from caster: bonus damage equal to stacks
                int strStacks = caster.ConsumeStatus(StatusEffectType.Strength);
                dmg += strStacks;

                // Consume Burn from target: bonus damage equal to stacks
                int burnStacks = unit.ConsumeStatus(StatusEffectType.Burn);
                dmg += burnStacks;

                unit.TakeHit(dmg);
                string bonusLog = "";
                if (strStacks > 0) bonusLog += $" (+{strStacks} Strength)";
                if (burnStacks > 0) bonusLog += $" (+{burnStacks} Burn)";
                Debug.Log($"Attack: {caster.DisplayName} hit {unit.DisplayName} for {dmg} damage{bonusLog}.");

                // Apply status-on-hit if configured
                if (action.statusStacks > 0)
                {
                    unit.ApplyStatus(action.statusEffect, action.statusStacks);
                    var def = StatusEffectDefs.Get(action.statusEffect);
                    Debug.Log($"Attack: applied {action.statusStacks} {def.Name} to {unit.DisplayName}.");
                }
                break;
            }
        }
    }
}
