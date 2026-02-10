using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies a status effect to the enemy at target hex.
/// Range works like Attack — valid if enemy is within action.range.
/// </summary>
public class StatusResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();
        if (caster.Coord.DistanceTo(enemy.Coord) <= action.range)
            targets.Add(enemy.Coord);
        return targets;
    }

    public void Resolve(CardAction action, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        enemy.ApplyStatus(action.statusEffect, action.statusStacks);
        var def = StatusEffectDefs.Get(action.statusEffect);
        Debug.Log($"Status: {caster.Team} applied {action.statusStacks} {def.Name} to {enemy.Team}.");
    }
}
