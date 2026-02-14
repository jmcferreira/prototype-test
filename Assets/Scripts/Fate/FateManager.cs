using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that orchestrates Fate card draws during combat.
/// Handles player interactive selection (via FateSelectionUI) and AI auto-pick.
/// Sets FateCombatContext before attack resolution.
/// </summary>
public class FateManager : MonoBehaviour
{
    public static FateManager Instance { get; private set; }

    private FateSelectionUI _selectionUI;

    private void Awake()
    {
        Instance = this;
    }

    public void SetSelectionUI(FateSelectionUI ui)
    {
        _selectionUI = ui;
    }

    /// <summary>
    /// Full fate draw sequence for a single-target attack. Sets FateCombatContext.
    /// Yields until player selections are complete.
    /// </summary>
    /// <param name="attacker">The attacking unit.</param>
    /// <param name="defender">The unit being attacked (for defense fate). Can be null for AoE.</param>
    /// <param name="isPlayerAttacker">True if the player is the attacker (interactive pick).</param>
    /// <param name="isPlayerDefender">True if the player is the defender (interactive pick).</param>
    public IEnumerator ResolveFateDraws(Unit attacker, Unit defender, bool isPlayerAttacker, bool isPlayerDefender)
    {
        FateCombatContext.Clear();

        // --- Attacker fate draw ---
        if (attacker.FateDeck != null)
        {
            var drawn = attacker.FateDeck.Draw(2);
            if (drawn.Length > 0)
            {
                FateCardData picked;
                if (isPlayerAttacker && drawn.Length >= 2)
                {
                    // Player picks attack fate interactively
                    picked = null;
                    yield return StartCoroutine(PlayerPickFate(drawn, "Attack Fate", p => picked = p));
                }
                else
                {
                    // AI picks: choose card with highest damageBonus
                    picked = AIPickAttackFate(drawn);
                }

                if (picked != null)
                {
                    FateCombatContext.DamageBonus = picked.damageBonus;
                    if (picked.statusStacks > 0)
                    {
                        FateCombatContext.AttackerStatusEffect = picked.statusEffect;
                        FateCombatContext.AttackerStatusStacks = picked.statusStacks;
                    }
                    BattleLog.AddAction($"Fate ({attacker.DisplayName}): {picked.cardName}");
                }

                attacker.FateDeck.DiscardAll(drawn);
            }
        }

        // --- Defender fate draw (single-target only) ---
        if (defender != null && defender.FateDeck != null && defender.IsAlive)
        {
            var drawn = defender.FateDeck.Draw(2);
            if (drawn.Length > 0)
            {
                FateCardData picked;
                if (isPlayerDefender && drawn.Length >= 2)
                {
                    // Player picks defense fate interactively
                    picked = null;
                    yield return StartCoroutine(PlayerPickFate(drawn, "Defense Fate", p => picked = p));
                }
                else
                {
                    // AI picks: choose card with highest blockBonus + dodgeBonus
                    picked = AIPickDefenseFate(drawn);
                }

                if (picked != null)
                {
                    // Apply defensive modifiers directly to the defender
                    if (picked.blockBonus > 0)
                    {
                        defender.AddBlock(picked.blockBonus);
                        BattleLog.AddAction($"Fate: {defender.DisplayName} gains {picked.blockBonus} Block");
                    }
                    if (picked.dodgeBonus > 0)
                    {
                        defender.ApplyStatus(StatusEffectType.Dodge, picked.dodgeBonus);
                        BattleLog.AddAction($"Fate: {defender.DisplayName} gains Dodge {picked.dodgeBonus}");
                    }
                    if (picked.statusStacks > 0)
                    {
                        FateCombatContext.DefenderStatusEffect = picked.statusEffect;
                        FateCombatContext.DefenderStatusStacks = picked.statusStacks;
                    }
                    BattleLog.AddAction($"Fate ({defender.DisplayName}): {picked.cardName}");
                }

                defender.FateDeck.DiscardAll(drawn);
            }
        }
    }

    private IEnumerator PlayerPickFate(FateCardData[] drawn, string header, Action<FateCardData> onPicked)
    {
        if (_selectionUI == null)
        {
            // Fallback: auto-pick first card if no UI
            onPicked?.Invoke(drawn[0]);
            yield break;
        }

        bool waiting = true;
        _selectionUI.Show(drawn[0], drawn[1], header, (picked) =>
        {
            onPicked?.Invoke(picked);
            waiting = false;
        });

        while (waiting)
            yield return null;
    }

    private static FateCardData AIPickAttackFate(FateCardData[] drawn)
    {
        if (drawn.Length == 0) return null;
        if (drawn.Length == 1) return drawn[0];

        // Prefer higher damage bonus, then status stacks
        int score0 = drawn[0].damageBonus * 10 + drawn[0].statusStacks;
        int score1 = drawn[1].damageBonus * 10 + drawn[1].statusStacks;
        return score0 >= score1 ? drawn[0] : drawn[1];
    }

    private static FateCardData AIPickDefenseFate(FateCardData[] drawn)
    {
        if (drawn.Length == 0) return null;
        if (drawn.Length == 1) return drawn[0];

        // Prefer block, then dodge
        int score0 = drawn[0].blockBonus * 10 + drawn[0].dodgeBonus * 15 + drawn[0].statusStacks;
        int score1 = drawn[1].blockBonus * 10 + drawn[1].dodgeBonus * 15 + drawn[1].statusStacks;
        return score0 >= score1 ? drawn[0] : drawn[1];
    }
}
