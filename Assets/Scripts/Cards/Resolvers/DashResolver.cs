using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dash: move in a straight line up to range hexes in one of 6 directions.
/// Returns all reachable hexes along each direction (stops at edge/occupied).
/// Swift status adds bonus range.
/// </summary>
public class DashResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        // Root prevents all movement
        if (caster.HasStatus(StatusEffectType.Root))
            return new List<HexCoord>();

        int bonusRange = caster.GetStatusStacks(StatusEffectType.Swift);
        int totalRange = action.range + bonusRange;

        var targets = new List<HexCoord>();

        for (int dir = 0; dir < 6; dir++)
        {
            HexCoord current = caster.Coord;
            for (int step = 0; step < totalRange; step++)
            {
                HexCoord next = current.Neighbor(dir);
                if (!grid.TryGetTile(next, out _)) break;
                if (IsOccupied(next, allUnits)) break;
                targets.Add(next);
                current = next;
            }
        }

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        int dist = caster.Coord.DistanceTo(target);
        Debug.Log($"Dash: {caster.DisplayName} dashed to {target}.");
        BattleLog.AddAction($"Dash {dist}");
        caster.ForceMoveTo(target);
        caster.NotifyMoved();
        TokenManager.Instance?.OnUnitEnterHex(caster, target, allUnits);
    }

    private static bool IsOccupied(HexCoord coord, List<Unit> allUnits)
    {
        foreach (var unit in allUnits)
        {
            if (unit.IsAlive && unit.Coord == coord) return true;
        }
        return false;
    }
}
