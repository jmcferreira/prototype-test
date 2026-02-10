using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Push: shove the enemy 1 hex away from the caster.
/// Valid target is the enemy's hex (if in range and push destination is open).
/// </summary>
public class PushResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardData data, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        if (caster.Coord.DistanceTo(enemy.Coord) > data.range)
            return targets;

        HexCoord pushDir = enemy.Coord - caster.Coord;
        HexCoord dest = enemy.Coord + pushDir;

        if (!grid.TryGetTile(dest, out _)) return targets;
        if (IsOccupied(dest, enemy)) return targets;

        targets.Add(enemy.Coord);
        return targets;
    }

    public void Resolve(CardData data, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        HexCoord pushDir = enemy.Coord - caster.Coord;
        HexCoord dest = enemy.Coord + pushDir;

        Debug.Log($"Push: {enemy.Team} pushed from {enemy.Coord} to {dest}.");
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
