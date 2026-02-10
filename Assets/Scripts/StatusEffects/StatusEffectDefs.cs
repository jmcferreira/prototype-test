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
                Icon = "\u25B2",                            // ▲ flame-like triangle
                IconColor = new Color(1f, 0.5f, 0f),       // orange
                MaxStacks = 3,
                DamageTrigger = StatusTrigger.OnAction,
                DamagePerStack = 1,
                DecayPerTurn = 0,
                BlocksAttackTargeting = false,
            }
        },
        {
            StatusEffectType.Poison, new Def
            {
                Name = "Poison",
                Icon = "\u25CF",                            // ● droplet-like circle
                IconColor = new Color(0.2f, 0.85f, 0.2f),  // green
                MaxStacks = 5,
                DamageTrigger = StatusTrigger.OnTurnEnd,
                DamagePerStack = 1,
                DecayPerTurn = 1,
                BlocksAttackTargeting = false,
            }
        },
        {
            StatusEffectType.Blind, new Def
            {
                Name = "Blind",
                Icon = "\u25C9",                            // ◉ eye-like fisheye
                IconColor = new Color(0.7f, 0.5f, 0.9f),   // purple
                MaxStacks = 1,
                DamageTrigger = StatusTrigger.None,
                DamagePerStack = 0,
                DecayPerTurn = 1,
                BlocksAttackTargeting = true,
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
