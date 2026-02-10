using UnityEngine;

/// <summary>
/// One step in a card's action sequence.
/// A card can have multiple actions resolved in order.
/// </summary>
[System.Serializable]
public class CardAction
{
    public CardEffect effect;

    [Min(1)] public int range = 1;
    [Min(0)] public int damage = 0;
    [Min(0)] public int pushDistance = 1;

    [Tooltip("If true, this action cannot be skipped by the player")]
    public bool mandatory;

    [Header("Status Effect (only used when effect = Status, or on attacks that inflict status)")]
    public StatusEffectType statusEffect;
    [Min(0)] public int statusStacks = 0;

    [Tooltip("If true, this status targets the caster instead of enemies")]
    public bool targetSelf;

    [Header("Multi-target (Attack only — 0 or 1 = single target)")]
    [Min(1)] public int maxTargets = 1;

    [Header("Range bonus (Attack only)")]
    [Min(0)] public int bonusDamage = 0;
    [Tooltip("If true, bonusDamage is added when target is at max range")]
    public bool bonusDamageAtMaxRange;

    [Header("Cooldown reduction")]
    [Tooltip("Index (0-based) of the card in hand whose cooldown is reduced")]
    public int targetCardIndex = -1;
}
