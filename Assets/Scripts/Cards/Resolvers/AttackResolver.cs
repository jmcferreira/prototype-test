using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack: deal card.damage to the enemy if within card.range hex distance.
/// Valid target is the enemy's hex.
/// </summary>
public class AttackResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardData data, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();
        if (caster.Coord.DistanceTo(enemy.Coord) <= data.range)
            targets.Add(enemy.Coord);
        return targets;
    }

    public void Resolve(CardData data, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        enemy.TakeHit(data.damage);
        Debug.Log($"Attack: {caster.Team} hit {enemy.Team} for {data.damage} damage.");
    }
}
