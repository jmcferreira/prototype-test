using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pull: yank the enemy N hexes toward the caster (action.pushDistance).
/// Valid target is the enemy's hex if within action.range, not already adjacent,
/// and at least 1 hex of pull space exists. Stops early at occupied tiles.
/// </summary>
public class PullResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        int dist = caster.Coord.DistanceTo(enemy.Coord);
        if (dist > action.range || dist <= 1)
            return targets;

        int pullDir = GetDirectionToward(enemy.Coord, caster.Coord);
        HexCoord dest = WalkDirection(enemy.Coord, pullDir, action.pushDistance, enemy, grid);

        if (dest != enemy.Coord)
            targets.Add(enemy.Coord);

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        int pullDir = GetDirectionToward(enemy.Coord, caster.Coord);
        HexCoord dest = WalkDirection(enemy.Coord, pullDir, action.pushDistance, enemy, grid);

        Debug.Log($"Pull: {enemy.Team} pulled {enemy.Coord.DistanceTo(dest)} hex(es) from {enemy.Coord} to {dest}.");
        enemy.ForceMoveTo(dest);
    }

    /// <summary>
    /// Find the hex direction index (0–5) that points from 'from' toward 'toward'.
    /// </summary>
    private static int GetDirectionToward(HexCoord from, HexCoord toward)
    {
        int bestDir = 0;
        int bestDist = int.MaxValue;

        for (int i = 0; i < 6; i++)
        {
            int dist = from.Neighbor(i).DistanceTo(toward);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir = i;
            }
        }
        return bestDir;
    }

    private static HexCoord WalkDirection(HexCoord origin, int direction, int distance, Unit moving, HexGrid grid)
    {
        HexCoord current = origin;
        for (int i = 0; i < distance; i++)
        {
            HexCoord next = current.Neighbor(direction);
            if (!grid.TryGetTile(next, out _)) break;
            if (IsOccupied(next, moving)) break;
            current = next;
        }
        return current;
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
