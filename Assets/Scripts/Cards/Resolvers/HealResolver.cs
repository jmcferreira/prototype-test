using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Heal: the caster recovers HP equal to action.damage (used as heal amount).
/// Valid target is the caster's own hex. Always available if HP is below max.
/// </summary>
public class HealResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();
        if (caster.HP < caster.MaxHP)
            targets.Add(caster.Coord);
        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        caster.Heal(action.damage);
        BattleLog.AddAction($"Heal {action.damage}");
    }
}
