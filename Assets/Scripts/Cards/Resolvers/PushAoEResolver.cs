using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AoE Push: pushes ALL non-caster units within range away from the caster.
/// Valid targets = all pushable unit hexes in range. Clicking any one triggers
/// the push on all of them.
/// </summary>
public class PushAoEResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        foreach (var unit in allUnits)
        {
            if (unit == caster || !unit.IsAlive) continue;
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
        // Push ALL units in range, not just the clicked one
        var toPush = new List<(Unit unit, HexCoord dest)>();

        foreach (var unit in allUnits)
        {
            if (unit == caster || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) > action.range) continue;

            int pushDir = GetDirectionAwayFrom(caster.Coord, unit.Coord);
            HexCoord dest = WalkDirection(unit.Coord, pushDir, action.pushDistance, unit, allUnits, grid);
            if (dest != unit.Coord)
                toPush.Add((unit, dest));
        }

        foreach (var (unit, dest) in toPush)
        {
            int pushDist = unit.Coord.DistanceTo(dest);
            Debug.Log($"PushAoE: {unit.DisplayName} pushed from {unit.Coord} to {dest}.");
            BattleLog.AddAction($"Push {unit.DisplayName} {pushDist} hex(es)");
            unit.ForceMoveTo(dest);
            TokenManager.Instance?.OnUnitEnterHex(unit, dest, allUnits);
        }
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
