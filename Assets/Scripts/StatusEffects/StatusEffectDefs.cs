using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven registry of all status effect behaviours.
/// To add a new status effect:
///   1. Add an entry to the StatusEffectType enum
///   2. Add a Def here with the desired parameters
///   3. (Optional) If it needs a unique passive modifier, add a check where needed
///      (like Blind blocks attack targeting in AttackResolver).
/// </summary>
public static class StatusEffectDefs
{
    public class Def
    {
        /// <summary>Display name shown in UI and logs.</summary>
        public string Name;

        /// <summary>Short description for tooltip.</summary>
        public string Description;

        /// <summary>Unicode character used as icon on the unit info panel.</summary>
        public string Icon;

        /// <summary>Color of the icon on the unit info panel.</summary>
        public Color IconColor;

        /// <summary>Maximum number of stacks that can accumulate.</summary>
        public int MaxStacks;

        /// <summary>When this status deals its damage (None if it's a passive modifier).</summary>
        public StatusTrigger DamageTrigger;

        /// <summary>HP lost per stack when the trigger fires.</summary>
        public int DamagePerStack;

        /// <summary>Stacks removed at the end of each turn (0 = permanent until cleansed).</summary>
        public int DecayPerTurn;

        /// <summary>Flat damage dealt each time the trigger fires (independent of stacks).</summary>
        public int FlatDamage;

        /// <summary>If > 0, increases a random card's cooldown by this amount on trigger.</summary>
        public int CooldownPenalty;

        /// <summary>If true, the afflicted unit cannot target enemies with attacks.</summary>
        public bool BlocksAttackTargeting;
    }

    // ─── EDIT THIS TABLE TO ADD / TWEAK STATUS EFFECTS ─────────────────
    private static readonly Dictionary<StatusEffectType, Def> _defs = new()
    {
        {
            StatusEffectType.Burn, new Def
            {
                Name = "Burn",
                Description = "Consumed when hit by an attack. Adds bonus damage equal to stacks.",
                Icon = "\u25B2",                            // ▲ flame-like triangle
                IconColor = new Color(1f, 0.5f, 0f),       // orange
                MaxStacks = 5,
                DamageTrigger = StatusTrigger.None,         // consumed on next attack hit
                DamagePerStack = 0,
                DecayPerTurn = 0,                           // persists until consumed
                BlocksAttackTargeting = false,
            }
        },
        {
            StatusEffectType.Poison, new Def
            {
                Name = "Poison",
                Description = "Deals 1 damage and +1 cooldown on a random card at end of turn. Decays 1/turn.",
                Icon = "\u25CF",                            // ● droplet-like circle
                IconColor = new Color(0.2f, 0.85f, 0.2f),  // green
                MaxStacks = 5,
                DamageTrigger = StatusTrigger.OnTurnEnd,
                DamagePerStack = 0,                         // stacks = duration only
                FlatDamage = 1,                             // 1 damage per turn end
                CooldownPenalty = 1,                        // +1 CD on a random card
                DecayPerTurn = 1,
                BlocksAttackTargeting = false,
            }
        },
        {
            StatusEffectType.Blind, new Def
            {
                Name = "Blind",
                Description = "Cannot target enemies with attacks. Wears off after 1 turn.",
                Icon = "\u25C9",                            // ◉ eye-like fisheye
                IconColor = new Color(0.7f, 0.5f, 0.9f),   // purple
                MaxStacks = 1,
                DamageTrigger = StatusTrigger.None,
                DamagePerStack = 0,
                DecayPerTurn = 1,
                BlocksAttackTargeting = true,
            }
        },
        {
            StatusEffectType.Swift, new Def
            {
                Name = "Swift",
                Description = "Adds bonus movement range equal to stacks. Decays 1/turn.",
                Icon = "\u2192",                            // → arrow
                IconColor = new Color(1f, 0.85f, 0.2f),    // yellow
                MaxStacks = 3,
                DamageTrigger = StatusTrigger.None,
                DamagePerStack = 0,
                DecayPerTurn = 1,
                BlocksAttackTargeting = false,
            }
        },
        {
            StatusEffectType.Root, new Def
            {
                Name = "Root",
                Description = "Cannot move, dash, or jump. Decays 1/turn.",
                Icon = "\u2A02",                            // ⊗ cross-circle
                IconColor = new Color(0.55f, 0.35f, 0.15f), // brown
                MaxStacks = 3,
                DamageTrigger = StatusTrigger.None,
                DamagePerStack = 0,
                DecayPerTurn = 1,
                BlocksAttackTargeting = false,
            }
        },
        {
            StatusEffectType.Strength, new Def
            {
                Name = "Strength",
                Description = "Consumed on next attack. Adds bonus damage equal to stacks.",
                Icon = "\u2694",                            // ⚔ crossed swords
                IconColor = new Color(1f, 0.35f, 0.35f),   // red
                MaxStacks = 5,
                DamageTrigger = StatusTrigger.None,         // consumed on next attack (like Burn but self)
                DamagePerStack = 0,
                DecayPerTurn = 0,                           // persists until consumed
                BlocksAttackTargeting = false,
            }
        },
    };
    // ────────────────────────────────────────────────────────────────────

    public static Def Get(StatusEffectType type) => _defs[type];

    /// <summary>
    /// Iterate all registered definitions (used by Unit to tick statuses generically).
    /// </summary>
    public static IEnumerable<KeyValuePair<StatusEffectType, Def>> All => _defs;
}
