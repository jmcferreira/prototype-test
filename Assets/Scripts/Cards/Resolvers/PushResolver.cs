using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Push: shove the enemy N hexes away from the caster (action.pushDistance).
/// Valid target is the enemy's hex if within action.range and at least 1 hex
/// of push space exists. Stops early at grid edge or occupied tiles.
/// </summary>
public class PushResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, Unit enemy, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        if (caster.Coord.DistanceTo(enemy.Coord) > action.range)
            return targets;

        int pushDir = GetDirectionAwayFrom(caster.Coord, enemy.Coord);
        HexCoord dest = WalkDirection(enemy.Coord, pushDir, action.pushDistance, enemy, grid);

        // Only valid if the enemy actually moves at least 1 hex
        if (dest != enemy.Coord)
            targets.Add(enemy.Coord);

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, Unit enemy, HexGrid grid, HexCoord target)
    {
        int pushDir = GetDirectionAwayFrom(caster.Coord, enemy.Coord);
        HexCoord dest = WalkDirection(enemy.Coord, pushDir, action.pushDistance, enemy, grid);

        Debug.Log($"Push: {enemy.Team} pushed {enemy.Coord.DistanceTo(dest)} hex(es) from {enemy.Coord} to {dest}.");
        enemy.ForceMoveTo(dest);
    }

    /// <summary>
    /// Find the hex direction index (0–5) that points from 'from' away toward 'to'.
    /// </summary>
    private static int GetDirectionAwayFrom(HexCoord from, HexCoord at)
    {
        // The push direction is from the caster toward the target (away from caster)
        int bestDir = 0;
        int bestDist = int.MaxValue;
        HexCoord desired = at + (at - from);

        for (int i = 0; i < 6; i++)
        {
            int dist = at.Neighbor(i).DistanceTo(desired);
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
