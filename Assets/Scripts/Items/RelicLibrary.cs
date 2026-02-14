using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool of all available relics with tier-weighted random selection.
/// Tier odds: Common 60%, Uncommon 30%, Rare 10%.
/// </summary>
public static class RelicLibrary
{
    private static readonly RelicDef[] AllRelics = new[]
    {
        // Common (60%)
        new RelicDef
        {
            id = "iron_skin", relicName = "Iron Skin",
            description = "Gain 1 Block at the start of each turn",
            tier = ItemTier.Common, effect = RelicEffect.StartingBlock, value = 1,
        },
        new RelicDef
        {
            id = "vitality_pendant", relicName = "Vitality Pendant",
            description = "+2 Maximum HP",
            tier = ItemTier.Common, effect = RelicEffect.MaxHPBonus, value = 2,
        },
        new RelicDef
        {
            id = "gold_tooth", relicName = "Gold Tooth",
            description = "Start each scenario with +3 Gold",
            tier = ItemTier.Common, effect = RelicEffect.GoldBonus, value = 3,
        },
        new RelicDef
        {
            id = "thick_hide", relicName = "Thick Hide",
            description = "Gain 2 Block at the start of each turn",
            tier = ItemTier.Common, effect = RelicEffect.StartingBlock, value = 2,
        },

        // Uncommon (30%)
        new RelicDef
        {
            id = "healing_herb", relicName = "Healing Herb",
            description = "+10% healing between scenarios",
            tier = ItemTier.Uncommon, effect = RelicEffect.HealBonus, value = 10,
        },
        new RelicDef
        {
            id = "battle_scars", relicName = "Battle Scars",
            description = "+4 Maximum HP",
            tier = ItemTier.Uncommon, effect = RelicEffect.MaxHPBonus, value = 4,
        },
        new RelicDef
        {
            id = "razor_edge", relicName = "Razor Edge",
            description = "+1 damage on all attacks",
            tier = ItemTier.Uncommon, effect = RelicEffect.DamageBonus, value = 1,
        },

        // Rare (10%)
        new RelicDef
        {
            id = "obsidian_blade", relicName = "Obsidian Blade",
            description = "+2 damage on all attacks",
            tier = ItemTier.Rare, effect = RelicEffect.DamageBonus, value = 2,
        },
        new RelicDef
        {
            id = "immortal_heart", relicName = "Immortal Heart",
            description = "+6 Maximum HP",
            tier = ItemTier.Rare, effect = RelicEffect.MaxHPBonus, value = 6,
        },
    };

    /// <summary>
    /// Select N distinct relics using tier-weighted random rolls.
    /// Common: 60%, Uncommon: 30%, Rare: 10%.
    /// </summary>
    public static RelicDef[] GetRandomRelics(int count)
    {
        // Build tier buckets
        var common = new List<RelicDef>();
        var uncommon = new List<RelicDef>();
        var rare = new List<RelicDef>();

        foreach (var r in AllRelics)
        {
            switch (r.tier)
            {
                case ItemTier.Common: common.Add(r); break;
                case ItemTier.Uncommon: uncommon.Add(r); break;
                case ItemTier.Rare: rare.Add(r); break;
            }
        }

        var selected = new List<RelicDef>();
        var usedIds = new HashSet<string>();
        int attempts = 0;

        while (selected.Count < count && attempts < 50)
        {
            attempts++;
            float roll = Random.value;
            List<RelicDef> pool;

            if (roll < 0.60f)
                pool = common;
            else if (roll < 0.90f)
                pool = uncommon;
            else
                pool = rare;

            if (pool.Count == 0) continue;

            var pick = pool[Random.Range(0, pool.Count)];
            if (usedIds.Contains(pick.id)) continue;

            selected.Add(pick);
            usedIds.Add(pick.id);
        }

        // Fallback: fill remaining slots from any relic
        if (selected.Count < count)
        {
            foreach (var r in AllRelics)
            {
                if (selected.Count >= count) break;
                if (!usedIds.Contains(r.id))
                {
                    selected.Add(r);
                    usedIds.Add(r.id);
                }
            }
        }

        return selected.ToArray();
    }
}
