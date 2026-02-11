using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places a Web token on an empty hex within range (no unit, no existing token).
/// </summary>
public class PlaceWebResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();

        foreach (var kvp in grid.Tiles)
        {
            HexCoord coord = kvp.Key;
            if (caster.Coord.DistanceTo(coord) > action.range) continue;
            if (caster.Coord == coord) continue;

            // Skip occupied hexes
            bool occupied = false;
            foreach (var u in allUnits)
            {
                if (u.IsAlive && u.Coord == coord) { occupied = true; break; }
            }
            if (occupied) continue;

            // Skip hexes that already have a token
            if (TokenManager.Instance != null && TokenManager.Instance.HasToken(coord)) continue;

            targets.Add(coord);
        }

        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        if (TokenManager.Instance == null) return;

        TokenManager.Instance.PlaceToken(new WebToken(caster), target, grid);
        Debug.Log($"PlaceWeb: {caster.DisplayName} placed a Web at {target}.");
        BattleLog.AddAction($"Placed Web at {target}");
    }
}
