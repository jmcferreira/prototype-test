using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack: deal card.damage to a single enemy unit within card.range hex distance.
/// Checks all enemy-team units for valid targets.
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
                unit.TakeHit(action.damage);
                Debug.Log($"Attack: {caster.DisplayName} hit {unit.DisplayName} for {action.damage} damage.");
                break;
            }
        }
    }
}
