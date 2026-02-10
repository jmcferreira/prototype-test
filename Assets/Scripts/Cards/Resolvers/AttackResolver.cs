using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack: deal card.damage to the enemy if within card.range hex distance.
/// Valid target is the enemy's hex.
/// </summary>
public class AttackResolver : ICardResolver
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
        enemy.TakeHit(action.damage);
        Debug.Log($"Attack: {caster.Team} hit {enemy.Team} for {action.damage} damage.");
    }
}
