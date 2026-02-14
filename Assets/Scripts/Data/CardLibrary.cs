using UnityEngine;

/// <summary>
/// Central registry of card deck definitions.
/// Each deck is identified by a string key used in UnitDef.deckId.
/// Returns fresh ScriptableObject instances on every call.
/// </summary>
public static class CardLibrary
{
    public static CardData[] GetDeck(string deckId)
    {
        return deckId switch
        {
            "player_default" => CreatePlayerDeck(),
            "spider"         => CreateSpiderDeck(),
            "cultist"        => CreateCultistDeck(),
            _                => System.Array.Empty<CardData>(),
        };
    }

    // ── Player ───────────────────────────────────────────────────────────

    private static CardData[] CreatePlayerDeck()
    {
        // Card 1 — Volley: Move 2 → Attack 1 Range 2 (up to 2 targets)
        var volley = ScriptableObject.CreateInstance<CardData>();
        volley.cardName = "Volley";
        volley.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 2, damage = 1, maxTargets = 2 },
        };
        volley.cooldown = 0;

        // Card 2 — Firebolt: Line Attack 2 Range 3 + Burn 2 → Move 1
        var firebolt = ScriptableObject.CreateInstance<CardData>();
        firebolt.cardName = "Firebolt";
        firebolt.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackLine, range = 3, damage = 2,
                             statusEffect = StatusEffectType.Burn, statusStacks = 2 },
            new CardAction { effect = CardEffect.Move, range = 1 },
        };
        firebolt.cooldown = 1;

        // Card 3 — Quick Maneuver: Push All 1 Range 1 → Move 2
        var quickManeuver = ScriptableObject.CreateInstance<CardData>();
        quickManeuver.cardName = "Quick Maneuver";
        quickManeuver.actions = new[]
        {
            new CardAction { effect = CardEffect.PushAoE, range = 1, pushDistance = 1 },
            new CardAction { effect = CardEffect.Move, range = 2 },
        };
        quickManeuver.cooldown = 2;

        // Card 4 — Crippling Shot: Attack 2 Range 2 + Root 1 → Reduce CD on Headshot
        var cripplingShot = ScriptableObject.CreateInstance<CardData>();
        cripplingShot.cardName = "Crippling Shot";
        cripplingShot.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 2, damage = 2,
                             statusEffect = StatusEffectType.Root, statusStacks = 1 },
            new CardAction { effect = CardEffect.ReduceCooldown, targetCardIndex = 4 },
        };
        cripplingShot.cooldown = 3;

        // Card 5 — Headshot! (Ultimate): Move 2 → Attack 3 Range 3 (5 at max range)
        // Starts on cooldown 6
        var headshot = ScriptableObject.CreateInstance<CardData>();
        headshot.cardName = "Headshot!";
        headshot.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 3,
                             bonusDamage = 2, bonusDamageAtMaxRange = true },
        };
        headshot.cooldown = 6;
        headshot.startCooldown = 6;

        return new[] { volley, firebolt, quickManeuver, cripplingShot, headshot };
    }

    // ── Spider ───────────────────────────────────────────────────────────

    private static CardData[] CreateSpiderDeck()
    {
        // Card 1 — Web Leap: Jump 2 → Place Web 1
        var webLeap = ScriptableObject.CreateInstance<CardData>();
        webLeap.cardName = "Web Leap";
        webLeap.actions = new[]
        {
            new CardAction { effect = CardEffect.Jump, range = 2 },
            new CardAction { effect = CardEffect.PlaceWeb, range = 1 },
        };
        webLeap.cooldown = 1;

        // Card 2 — Venomous Bite: Attack 0 Range 3 → Poison 2 Range 3
        var venomBite = ScriptableObject.CreateInstance<CardData>();
        venomBite.cardName = "Venomous Bite";
        venomBite.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 0 },
            new CardAction { effect = CardEffect.Status, range = 3, statusEffect = StatusEffectType.Poison, statusStacks = 2 },
        };
        venomBite.cooldown = 2;

        return new[] { webLeap, venomBite };
    }

    // ── Cultist ──────────────────────────────────────────────────────────

    private static CardData[] CreateCultistDeck()
    {
        // Card 1 — Dark Bolt: Move 1 → Attack 2 Range 3 (consistent ranged pressure)
        var darkBolt = ScriptableObject.CreateInstance<CardData>();
        darkBolt.cardName = "Dark Bolt";
        darkBolt.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 1 },
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 2 },
        };
        darkBolt.cooldown = 0;

        // Card 2 — Cleave: AttackAoE 3 Range 1 → Strength 1 (self)
        var cleave = ScriptableObject.CreateInstance<CardData>();
        cleave.cardName = "Cleave";
        cleave.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackAoE, range = 1, damage = 3 },
            new CardAction { effect = CardEffect.Status, range = 0, statusEffect = StatusEffectType.Strength, statusStacks = 1, targetSelf = true },
        };
        cleave.cooldown = 1;

        return new[] { darkBolt, cleave };
    }
}
