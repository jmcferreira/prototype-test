using UnityEngine;

/// <summary>
/// Runtime state for one card in a player's hand.
/// Wraps a CardData asset and tracks its current cooldown.
/// </summary>
public class CardInstance
{
    public CardData Data { get; }
    public int CooldownRemaining { get; private set; }

    public bool IsReady => CooldownRemaining <= 0;

    public CardInstance(CardData data)
    {
        Data = data;
        CooldownRemaining = 0;
    }

    /// <summary>
    /// Play this card. Starts its cooldown. Returns false if still cooling down.
    /// </summary>
    public bool TryPlay()
    {
        if (!IsReady)
        {
            Debug.Log($"[{Data.cardName}] still on cooldown ({CooldownRemaining} turns left).");
            return false;
        }

        CooldownRemaining = Data.cooldown;
        Debug.Log($"[{Data.cardName}] played! Cooldown set to {Data.cooldown}.");
        return true;
    }

    /// <summary>
    /// Refund the cooldown (e.g. if the effect failed or was cancelled).
    /// </summary>
    public void ResetCooldown()
    {
        CooldownRemaining = 0;
    }

    /// <summary>
    /// Tick cooldown down by 1. Called at the start of the owning player's turn.
    /// </summary>
    public void TickCooldown()
    {
        if (CooldownRemaining > 0)
        {
            CooldownRemaining--;
            Debug.Log($"[{Data.cardName}] cooldown ticked: {CooldownRemaining} turns left.");
        }
    }
}
