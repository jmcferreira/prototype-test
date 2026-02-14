using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Line attack: targets an enemy at range, deals damage (and optional status)
/// to every unit on the hex line between caster and target (excluding caster).
/// Used by Firebolt-style cards.
/// </summary>
public class AttackLineResolver : ICardResolver
{
    public List<HexCoord> GetValidTargets(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid)
    {
        var targets = new List<HexCoord>();
        if (caster.HasStatus(StatusEffectType.Blind))
            return targets;

        foreach (var unit in allUnits)
        {
            if (unit == caster || unit.Team == caster.Team || !unit.IsAlive) continue;
            if (caster.Coord.DistanceTo(unit.Coord) <= action.range)
                targets.Add(unit.Coord);
        }
        return targets;
    }

    public void Resolve(CardAction action, Unit caster, List<Unit> allUnits, HexGrid grid, HexCoord target)
    {
        caster.NotifyAttacked();

        // Consume Strength once for the whole line
        int strBonus = CombatResolver.ConsumeAttackerBonuses(caster);
        int fateDmg = FateCombatContext.DamageBonus;

        var line = HexLineDraw(caster.Coord, target);

        foreach (var hex in line)
        {
            if (hex == caster.Coord) continue;

            Unit hitUnit = null;
            foreach (var unit in allUnits)
            {
                if (unit.IsAlive && unit.Coord == hex)
                {
                    hitUnit = unit;
                    break;
                }
            }
            if (hitUnit == null) continue;

            var result = CombatResolver.ResolveHit(caster, hitUnit, action.damage, strBonus, fateDmg);
            CombatResolver.LogHit(caster, hitUnit, result);

            if (!result.dodged)
            {
                CombatResolver.ApplyHitStatus(hitUnit, action);
                CombatResolver.ApplyFateStatuses(caster, hitUnit);
            }
        }
    }

    /// <summary>
    /// Hex line draw using cube coordinate interpolation and rounding.
    /// Returns all hex coords from a to b inclusive.
    /// </summary>
    private static List<HexCoord> HexLineDraw(HexCoord a, HexCoord b)
    {
        int N = a.DistanceTo(b);
        var results = new List<HexCoord>();
        if (N == 0)
        {
            results.Add(a);
            return results;
        }

        float aq = a.q + 1e-6f;
        float ar = a.r + 1e-6f;
        float aS = -aq - ar;
        float bq = b.q + 1e-6f;
        float br = b.r + 1e-6f;
        float bS = -bq - br;

        for (int i = 0; i <= N; i++)
        {
            float t = (float)i / N;
            float q = aq + (bq - aq) * t;
            float r = ar + (br - ar) * t;
            float s = aS + (bS - aS) * t;
            results.Add(CubeRound(q, r, s));
        }
        return results;
    }

    private static HexCoord CubeRound(float q, float r, float s)
    {
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float dq = Mathf.Abs(rq - q);
        float dr = Mathf.Abs(rr - r);
        float ds = Mathf.Abs(rs - s);

        if (dq > dr && dq > ds)
            rq = -rr - rs;
        else if (dr > ds)
            rr = -rq - rs;

        return new HexCoord(rq, rr);
    }
}
