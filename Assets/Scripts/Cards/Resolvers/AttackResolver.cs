using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack: deal 1 damage to the enemy if they're within range.
/// Valid target is the enemy's hex (if in range).
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
        enemy.TakeHit(1);
        Debug.Log($"Attack: {caster.Team} hit {enemy.Team} for 1 damage.");
    }
}
