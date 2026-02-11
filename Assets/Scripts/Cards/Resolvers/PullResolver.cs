using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pull: yank an enemy N hexes toward the caster (action.pushDistance).
/// Valid target is any enemy's hex within action.range, not already adjacent,
/// and where at least 1 hex of pull space exists. Stops early at occupied tiles.
/// </summary>
public class PullResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        foreach (var unit in allUnits)
        {
            if (unit == caster || unit.Team == caster.Team || !unit.IsAlive) continue;

            int dist = caster.Coord.DistanceTo(unit.Coord);
            if (dist > action.range || dist <= 1) continue;

            int pullDir = GetDirectionToward(unit.Coord, caster.Coord);
            HexCoord dest = WalkDirection(unit.Coord, pullDir, action.pushDistance, unit, allUnits, grid);

            if (dest != unit.Coord)
                targets.Add(unit.Coord);
        }

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        Unit enemy = FindUnitAt(target, caster, allUnits);
        if (enemy == null) return;

        int pullDir = GetDirectionToward(enemy.Coord, caster.Coord);
        HexCoord dest = WalkDirection(enemy.Coord, pullDir, action.pushDistance, enemy, allUnits, grid);

        int pullDist = enemy.Coord.DistanceTo(dest);
        Debug.Log($"Pull: {enemy.DisplayName} pulled {pullDist} hex(es) from {enemy.Coord} to {dest}.");
        BattleLog.AddAction($"Pull {enemy.DisplayName} {pullDist} hex(es)");
        enemy.ForceMoveTo(dest);
        TokenManager.Instance?.OnUnitEnterHex(enemy, dest, allUnits);
    }

    private static Unit FindUnitAt(HexCoord coord, Unit exclude, List<Unit> allUnits)
    {
        foreach (var unit in allUnits)
        {
            if (unit != exclude && unit.IsAlive && unit.Coord == coord)
                return unit;
        }
        return null;
    }

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

    private static HexCoord WalkDirection(HexCoord origin, int direction, int distance, Unit moving, List<Unit> allUnits, HexGrid grid)
    {
        HexCoord current = origin;
        for (int i = 0; i < distance; i++)
        {
            HexCoord next = current.Neighbor(direction);
            if (!grid.TryGetTile(next, out _)) break;
            if (IsOccupied(next, moving, allUnits)) break;
            current = next;
        }
        return current;
    }

    private static bool IsOccupied(HexCoord coord, Unit exclude, List<Unit> allUnits)
    {
        foreach (var unit in allUnits)
        {
            if (unit == exclude) continue;
            if (unit.IsAlive && unit.Coord == coord) return true;
        }
        return false;
    }
}
