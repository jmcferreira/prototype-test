using UnityEngine;

/// <summary>
/// Centralized damage pipeline. All attack resolvers delegate here so that
/// Strength, Burn, Block, Dodge, and future fate-card modifiers are applied
/// in one consistent place.
/// </summary>
public static class CombatResolver
{
    public struct HitResult
    {
        public int baseDamage;
        public int strengthBonus;
        public int burnBonus;
        public int totalRaw;
        public int blocked;
        public bool dodged;
        public int finalDamage;
    }

    /// <summary>
    /// Consume and return Strength bonus from the caster.
    /// Call ONCE per attack action (shared across multi-target hits).
    /// </summary>
    public static int ConsumeAttackerBonuses(Unit caster)
    {
        return caster.ConsumeStatus(StatusEffectType.Strength);
    }

    /// <summary>
    /// Resolve a single attack hit against a target.
    /// Handles Burn consumption, Dodge check, Block absorption, and HP damage.
    /// </summary>
    /// <param name="caster">The attacking unit (for logging).</param>
    /// <param name="target">The unit being hit.</param>
    /// <param name="baseDamage">Damage before any status bonuses.</param>
    /// <param name="strengthBonus">Pre-consumed Strength bonus from caster.</param>
    public static HitResult ResolveHit(Unit caster, Unit target, int baseDamage, int strengthBonus)
    {
        var result = new HitResult
        {
            baseDamage = baseDamage,
            strengthBonus = strengthBonus,
        };

        // Consume Burn from target
        result.burnBonus = target.ConsumeStatus(StatusEffectType.Burn);
        result.totalRaw = baseDamage + strengthBonus + result.burnBonus;

        // Dodge check — consume 1 stack, negate the hit entirely
        if (target.HasStatus(StatusEffectType.Dodge))
        {
            target.ConsumeStatusStacks(StatusEffectType.Dodge, 1);
            result.dodged = true;
            result.finalDamage = 0;
            return result;
        }

        // Block absorption — reduce damage, consume Block points
        if (target.Block > 0)
        {
            result.blocked = target.ReduceBlock(result.totalRaw);
        }
        result.finalDamage = Mathf.Max(0, result.totalRaw - result.blocked);

        // Apply damage to HP
        if (result.finalDamage > 0)
            target.TakeHit(result.finalDamage);

        return result;
    }

    /// <summary>
    /// Log the hit result to BattleLog. Call after ResolveHit.
    /// </summary>
    public static void LogHit(Unit caster, Unit target, HitResult result)
    {
        if (result.dodged)
        {
            BattleLog.AddAction($"{target.DisplayName} dodged {caster.DisplayName}'s attack!");
            return;
        }

        string msg = $"{caster.DisplayName} dealt {result.finalDamage} dmg to {target.DisplayName}";

        var extras = new System.Collections.Generic.List<string>();
        if (result.strengthBonus > 0) extras.Add($"+{result.strengthBonus} Str");
        if (result.burnBonus > 0) extras.Add($"+{result.burnBonus} Burn");
        if (result.blocked > 0) extras.Add($"{result.blocked} blocked");

        if (extras.Count > 0)
            msg += $" ({string.Join(", ", extras)})";

        BattleLog.AddAction(msg);
    }

    /// <summary>
    /// Apply a post-hit status effect (e.g., Root on Crippling Shot).
    /// Only applies if the hit wasn't dodged and the target is still alive.
    /// </summary>
    public static void ApplyHitStatus(Unit target, CardAction action)
    {
        if (action.statusStacks > 0 && target.IsAlive)
        {
            target.ApplyStatus(action.statusEffect, action.statusStacks);
            var def = StatusEffectDefs.Get(action.statusEffect);
            BattleLog.AddAction($"Applied {def.Name} {action.statusStacks} to {target.DisplayName}");
        }
    }
}
