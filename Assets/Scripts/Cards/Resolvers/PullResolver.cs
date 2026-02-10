using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pull: yank the enemy 1 hex toward the caster.
/// Valid target is the enemy's hex (if in range, not already adjacent,
/// and pull destination is open).
/// </summary>
public class PullResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardData data, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        int dist = caster.Coord.DistanceTo(enemy.Coord);
        if (dist > data.range || dist <= 1)
            return targets;

        HexCoord pullDir = caster.Coord - enemy.Coord;
        HexCoord dest = enemy.Coord + pullDir;

        if (!grid.TryGetTile(dest, out _)) return targets;
        if (IsOccupied(dest, enemy)) return targets;

        targets.Add(enemy.Coord);
        return targets;
    }

    public void Resolve(CardData data, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        HexCoord pullDir = caster.Coord - enemy.Coord;
        HexCoord dest = enemy.Coord + pullDir;

        Debug.Log($"Pull: {enemy.Team} pulled from {enemy.Coord} to {dest}.");
        enemy.ForceMoveTo(dest);
    }

    private static bool IsOccupied(HexCoord coord, Unit exclude)
    {
        foreach (var unit in Object.FindObjectsOfType<Unit>())
        {
            if (unit == exclude) continue;
            if (unit.Coord == coord) return true;
        }
        return false;
    }
}
