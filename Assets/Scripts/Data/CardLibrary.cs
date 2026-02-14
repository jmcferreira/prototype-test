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
            "player_default"       => CreatePlayerDeck(),
            "player_guardian"      => CreateGuardianDeck(),
            "spider"               => CreateSpiderDeck(),
            "cultist"              => CreateCultistDeck(),
            "champion_darkside"    => CreateDarksideChampionDeck(),
            "champion_brotherhood" => CreateBrotherhoodChampionDeck(),
            "skeleton"             => CreateSkeletonDeck(),
            "witch"                => CreateWitchDeck(),
            "soldier"              => CreateSoldierDeck(),
            "sniper"               => CreateSniperDeck(),
            "guerrilla"            => CreateGuerrillaDeck(),
            _                      => System.Array.Empty<CardData>(),
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

    // ── Guardian (melee tank) ─────────────────────────────────────────────

    private static CardData[] CreateGuardianDeck()
    {
        // Card 1 — Shield Bash: Move 1 → Attack 2 Range 1. Basic melee engage.
        var shieldBash = ScriptableObject.CreateInstance<CardData>();
        shieldBash.cardName = "Shield Bash";
        shieldBash.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 1 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 2 },
        };
        shieldBash.cooldown = 0;

        // Card 2 — Protect: Heal 2 (self) → Dodge 1 (self). Defensive recovery.
        var protect = ScriptableObject.CreateInstance<CardData>();
        protect.cardName = "Protect";
        protect.actions = new[]
        {
            new CardAction { effect = CardEffect.Heal, damage = 2, targetSelf = true },
            new CardAction { effect = CardEffect.Status, statusEffect = StatusEffectType.Dodge,
                             statusStacks = 1, targetSelf = true },
        };
        protect.cooldown = 1;

        // Card 3 — War Cry: PushAoE 1 Range 1 → Strength 2 (self). Area clear + power up.
        var warCry = ScriptableObject.CreateInstance<CardData>();
        warCry.cardName = "War Cry";
        warCry.actions = new[]
        {
            new CardAction { effect = CardEffect.PushAoE, range = 1, pushDistance = 1 },
            new CardAction { effect = CardEffect.Status, statusEffect = StatusEffectType.Strength,
                             statusStacks = 2, targetSelf = true },
        };
        warCry.cooldown = 2;

        // Card 4 — Bulwark (Ultimate): AttackAoE 3 Range 1 → Heal 2 (self). Melee AoE + sustain.
        var bulwark = ScriptableObject.CreateInstance<CardData>();
        bulwark.cardName = "Bulwark";
        bulwark.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackAoE, range = 1, damage = 3 },
            new CardAction { effect = CardEffect.Heal, damage = 2, targetSelf = true },
        };
        bulwark.cooldown = 4;
        bulwark.startCooldown = 4;

        return new[] { shieldBash, protect, warCry, bulwark };
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

    // ── Darkside Champion ─────────────────────────────────────────────

    private static CardData[] CreateDarksideChampionDeck()
    {
        // Card 1 — Shadow Strike: Move 2 → Attack 2 Range 1
        var shadowStrike = ScriptableObject.CreateInstance<CardData>();
        shadowStrike.cardName = "Shadow Strike";
        shadowStrike.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 2 },
        };
        shadowStrike.cooldown = 0;

        // Card 2 — Drain Life: Attack 2 Range 2 → Heal 1 (self)
        var drainLife = ScriptableObject.CreateInstance<CardData>();
        drainLife.cardName = "Drain Life";
        drainLife.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 2, damage = 2 },
            new CardAction { effect = CardEffect.Heal, damage = 1, targetSelf = true },
        };
        drainLife.cooldown = 1;

        // Card 3 — Dark Blast: AttackAoE 2 Range 2 + Burn 1
        var darkBlast = ScriptableObject.CreateInstance<CardData>();
        darkBlast.cardName = "Dark Blast";
        darkBlast.actions = new[]
        {
            new CardAction { effect = CardEffect.AttackAoE, range = 2, damage = 2,
                             statusEffect = StatusEffectType.Burn, statusStacks = 1 },
        };
        darkBlast.cooldown = 2;

        // Card 4 — Unleash (Ultimate): Strength 3 (self) → AttackAoE 3 Range 1
        var unleash = ScriptableObject.CreateInstance<CardData>();
        unleash.cardName = "Unleash";
        unleash.actions = new[]
        {
            new CardAction { effect = CardEffect.Status, statusEffect = StatusEffectType.Strength,
                             statusStacks = 3, targetSelf = true },
            new CardAction { effect = CardEffect.AttackAoE, range = 1, damage = 3 },
        };
        unleash.cooldown = 4;
        unleash.startCooldown = 4;

        return new[] { shadowStrike, drainLife, darkBlast, unleash };
    }

    // ── Brotherhood Champion ──────────────────────────────────────────

    private static CardData[] CreateBrotherhoodChampionDeck()
    {
        // Card 1 — Advance: Move 2 → Attack 2 Range 1
        var advance = ScriptableObject.CreateInstance<CardData>();
        advance.cardName = "Advance";
        advance.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 2 },
        };
        advance.cooldown = 0;

        // Card 2 — Rally: Heal 2 (self) → Strength 1 (self)
        var rally = ScriptableObject.CreateInstance<CardData>();
        rally.cardName = "Rally";
        rally.actions = new[]
        {
            new CardAction { effect = CardEffect.Heal, damage = 2, targetSelf = true },
            new CardAction { effect = CardEffect.Status, statusEffect = StatusEffectType.Strength,
                             statusStacks = 1, targetSelf = true },
        };
        rally.cooldown = 1;

        // Card 3 — Barrage: Attack 1 Range 3 (3 targets)
        var barrage = ScriptableObject.CreateInstance<CardData>();
        barrage.cardName = "Barrage";
        barrage.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 1, maxTargets = 3 },
        };
        barrage.cooldown = 2;

        // Card 4 — Charge (Ultimate): Dash 3 → Attack 4 Range 1 → Push 2
        var charge = ScriptableObject.CreateInstance<CardData>();
        charge.cardName = "Charge";
        charge.actions = new[]
        {
            new CardAction { effect = CardEffect.Dash, range = 3 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 4 },
            new CardAction { effect = CardEffect.Push, range = 1, pushDistance = 2 },
        };
        charge.cooldown = 4;
        charge.startCooldown = 4;

        return new[] { advance, rally, barrage, charge };
    }

    // ── Skeleton (cheap melee) ────────────────────────────────────────

    private static CardData[] CreateSkeletonDeck()
    {
        // Card 1 — Bone Bash: Move 2 → Attack 1 Range 1
        var boneBash = ScriptableObject.CreateInstance<CardData>();
        boneBash.cardName = "Bone Bash";
        boneBash.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 1 },
        };
        boneBash.cooldown = 0;

        // Card 2 — Rattle: Move 1 → Attack 2 Range 1
        var rattle = ScriptableObject.CreateInstance<CardData>();
        rattle.cardName = "Rattle";
        rattle.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 1 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 2 },
        };
        rattle.cooldown = 1;

        return new[] { boneBash, rattle };
    }

    // ── Witch (ranged support) ────────────────────────────────────────

    private static CardData[] CreateWitchDeck()
    {
        // Card 1 — Hex Bolt: Attack 2 Range 3 + Blind 1
        var hexBolt = ScriptableObject.CreateInstance<CardData>();
        hexBolt.cardName = "Hex Bolt";
        hexBolt.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 2,
                             statusEffect = StatusEffectType.Blind, statusStacks = 1 },
        };
        hexBolt.cooldown = 0;

        // Card 2 — Curse: Poison 2 Range 3 → Burn 1 Range 3
        var curse = ScriptableObject.CreateInstance<CardData>();
        curse.cardName = "Curse";
        curse.actions = new[]
        {
            new CardAction { effect = CardEffect.Status, range = 3,
                             statusEffect = StatusEffectType.Poison, statusStacks = 2 },
            new CardAction { effect = CardEffect.Status, range = 3,
                             statusEffect = StatusEffectType.Burn, statusStacks = 1 },
        };
        curse.cooldown = 2;

        // Card 3 — Dark Ritual: Heal 3 (self) → Move 2
        var darkRitual = ScriptableObject.CreateInstance<CardData>();
        darkRitual.cardName = "Dark Ritual";
        darkRitual.actions = new[]
        {
            new CardAction { effect = CardEffect.Heal, damage = 3, targetSelf = true },
            new CardAction { effect = CardEffect.Move, range = 2 },
        };
        darkRitual.cooldown = 2;

        return new[] { hexBolt, curse, darkRitual };
    }

    // ── Soldier (cheap melee + block) ─────────────────────────────────

    private static CardData[] CreateSoldierDeck()
    {
        // Card 1 — Sword Strike: Move 1 → Attack 2 Range 1
        var swordStrike = ScriptableObject.CreateInstance<CardData>();
        swordStrike.cardName = "Sword Strike";
        swordStrike.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 1 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 2 },
        };
        swordStrike.cooldown = 0;

        // Card 2 — Shield Wall: Dodge 1 (self) → Attack 1 Range 1
        var shieldWall = ScriptableObject.CreateInstance<CardData>();
        shieldWall.cardName = "Shield Wall";
        shieldWall.actions = new[]
        {
            new CardAction { effect = CardEffect.Status, statusEffect = StatusEffectType.Dodge,
                             statusStacks = 1, targetSelf = true },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 1 },
        };
        shieldWall.cooldown = 1;

        return new[] { swordStrike, shieldWall };
    }

    // ── Sniper (long range) ───────────────────────────────────────────

    private static CardData[] CreateSniperDeck()
    {
        // Card 1 — Aimed Shot: Attack 2 Range 4 (bonus +2 at max range)
        var aimedShot = ScriptableObject.CreateInstance<CardData>();
        aimedShot.cardName = "Aimed Shot";
        aimedShot.actions = new[]
        {
            new CardAction { effect = CardEffect.Attack, range = 4, damage = 2,
                             bonusDamage = 2, bonusDamageAtMaxRange = true },
        };
        aimedShot.cooldown = 0;

        // Card 2 — Reposition: Move 2 → Attack 1 Range 3
        var reposition = ScriptableObject.CreateInstance<CardData>();
        reposition.cardName = "Reposition";
        reposition.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 3, damage = 1 },
        };
        reposition.cooldown = 1;

        return new[] { aimedShot, reposition };
    }

    // ── Guerrilla (mobile skirmisher) ─────────────────────────────────

    private static CardData[] CreateGuerrillaDeck()
    {
        // Card 1 — Hit and Run: Move 2 → Attack 1 Range 1 → Move 1
        var hitAndRun = ScriptableObject.CreateInstance<CardData>();
        hitAndRun.cardName = "Hit and Run";
        hitAndRun.actions = new[]
        {
            new CardAction { effect = CardEffect.Move, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 1 },
            new CardAction { effect = CardEffect.Move, range = 1 },
        };
        hitAndRun.cooldown = 0;

        // Card 2 — Ambush: Dash 2 → Attack 3 Range 1
        var ambush = ScriptableObject.CreateInstance<CardData>();
        ambush.cardName = "Ambush";
        ambush.actions = new[]
        {
            new CardAction { effect = CardEffect.Dash, range = 2 },
            new CardAction { effect = CardEffect.Attack, range = 1, damage = 3 },
        };
        ambush.cooldown = 2;

        return new[] { hitAndRun, ambush };
    }
}
