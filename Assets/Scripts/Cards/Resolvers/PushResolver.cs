using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Push: shove an enemy N hexes away from the caster (action.pushDistance).
/// Valid target is any enemy's hex within action.range where at least 1 hex
/// of push space exists. Stops early at grid edge or occupied tiles.
/// </summary>
public class PushResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        foreach (var unit in allUnits)
        {
            if (unit == caster || unit.Team == caster.Team || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) > action.range) continue;

            int pushDir = GetDirectionAwayFrom(caster.Coord, unit.Coord);
            HexCoord dest = WalkDirection(unit.Coord, pushDir, action.pushDistance, unit, allUnits, grid);

            if (dest != unit.Coord)
                targets.Add(unit.Coord);
        }

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        Unit enemy = FindUnitAt(target, caster, allUnits);
        if (enemy == null) return;

        int pushDir = GetDirectionAwayFrom(caster.Coord, enemy.Coord);
        HexCoord dest = WalkDirection(enemy.Coord, pushDir, action.pushDistance, enemy, allUnits, grid);

        int pushDist = enemy.Coord.DistanceTo(dest);
        Debug.Log($"Push: {enemy.DisplayName} pushed {pushDist} hex(es) from {enemy.Coord} to {dest}.");
        BattleLog.AddAction($"Push {enemy.DisplayName} {pushDist} hex(es)");
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

    private static int GetDirectionAwayFrom(HexCoord from, HexCoord at)
    {
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
