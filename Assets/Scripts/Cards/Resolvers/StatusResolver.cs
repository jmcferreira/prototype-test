using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies a status effect to the target. If targetSelf is true, the caster
/// is the target (for self-buffs). Otherwise targets enemy units within range.
/// </summary>
public class StatusResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        if (action.targetSelf)
        {
            targets.Add(caster.Coord);
            return targets;
        }

        foreach (var unit in allUnits)
        {
            if (unit == caster || caster.IsAlly(unit) || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
                targets.Add(unit.Coord);
        }
        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        Unit targetUnit = null;

        if (action.targetSelf)
        {
            targetUnit = caster;
        }
        else
        {
            foreach (var unit in allUnits)
            {
                if (unit.Coord == target && unit != caster && unit.IsAlive)
                {
                    targetUnit = unit;
                    break;
                }
            }
        }

        if (targetUnit == null) return;

        targetUnit.ApplyStatus(action.statusEffect, action.statusStacks);
        var def = StatusEffectDefs.Get(action.statusEffect);
        Debug.Log($"Status: {caster.DisplayName} applied {action.statusStacks} {def.Name} to {targetUnit.DisplayName}.");
        string targetName = action.targetSelf ? "self" : targetUnit.DisplayName;
        BattleLog.AddAction($"{def.Name} {action.statusStacks} on {targetName}");
    }
}
