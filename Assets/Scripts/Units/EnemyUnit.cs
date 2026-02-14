using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy unit. On its turn, picks the first ready card and executes
/// all of its actions step-by-step with visible delays (like the player flow).
/// Highlights valid targets, picks the best one, resolves, then advances.
/// Supports multi-target attacks, auto-resolve effects, and new card types.
/// </summary>
public class EnemyUnit : Unit
{
    private List<CardInstance> _cards;
    private TurnManager _turnManager;

    // Timing (seconds)
    private const float DelayTurnStart    = 0.6f;
    private const float DelayShowTargets  = 0.4f;
    private const float DelayAfterResolve = 0.5f;
    private const float DelayTurnEnd      = 0.4f;

    public void InitCards(CardData[] cardDatas)
    {
        _cards = new List<CardInstance>();
        foreach (var data in cardDatas)
            _cards.Add(new CardInstance(data));
    }

    public void SetTurnManager(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }

    public override void ReduceCardCooldown(int cardIndex, int amount)
    {
        if (cardIndex >= 0 && cardIndex < _cards.Count)
            _cards[cardIndex].ReduceCooldown(amount);
    }

    public override void IncreaseRandomCardCooldown(int amount)
    {
        if (_cards == null || _cards.Count == 0) return;
        int index = Random.Range(0, _cards.Count);
        _cards[index].IncreaseCooldown(amount);
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        // Tick cooldowns
        foreach (var card in _cards)
            card.TickCooldown();

        StartCoroutine(ExecuteTurn());
    }

    protected override List<Unit> GetAliveUnitsForPassive()
    {
        return _turnManager?.GetAliveUnits() ?? new List<Unit> { this };
    }

    private IEnumerator ExecuteTurn()
    {
        // Brief pause so the player can see the banner change
        yield return new WaitForSeconds(DelayTurnStart);

        var allUnits = _turnManager.GetAliveUnits();

        // Find primary target (closest player-team unit)
        Unit primaryTarget = FindPrimaryTarget(allUnits);
        if (primaryTarget == null)
        {
            Debug.Log($"{DisplayName} has no targets — passing.");
            BattleLog.AddAction("Passed (no targets)");
            yield return new WaitForSeconds(DelayTurnEnd);
            _turnManager.EndCurrentTurn();
            yield break;
        }

        // Try each card — prefer cards that can deal damage
        CardInstance chosenCard = null;

        // First pass: find a card with a damage action that has valid targets
        foreach (var card in _cards)
        {
            if (!card.IsReady || card.ActionCount == 0) continue;

            bool canDealDamage = false;
            for (int i = 0; i < card.ActionCount; i++)
            {
                var action = card.GetAction(i);
                var targets = card.GetValidTargetsForAction(i, this, allUnits, Grid);
                if (targets.Count > 0 && IsDamageEffect(action.effect))
                {
                    canDealDamage = true;
                    break;
                }
            }
            if (canDealDamage) { chosenCard = card; break; }
        }

        // Second pass: fall back to any card with at least one valid action
        if (chosenCard == null)
        {
            foreach (var card in _cards)
            {
                if (!card.IsReady || card.ActionCount == 0) continue;

                for (int i = 0; i < card.ActionCount; i++)
                {
                    var targets = card.GetValidTargetsForAction(i, this, allUnits, Grid);
                    if (targets.Count > 0) { chosenCard = card; break; }
                }
                if (chosenCard != null) break;
            }
        }

        if (chosenCard == null)
        {
            Debug.Log($"{DisplayName} has no playable cards — passing.");
            BattleLog.AddAction("Passed");
            yield return new WaitForSeconds(DelayTurnEnd);
            _turnManager.EndCurrentTurn();
            yield break;
        }

        Debug.Log($"{DisplayName} plays [{chosenCard.Data.cardName}].");
        BattleLog.AddCardName(chosenCard.Data.cardName);

        // Step through each action with delays (like the player)
        for (int i = 0; i < chosenCard.ActionCount; i++)
        {
            var action = chosenCard.GetAction(i);

            // Refresh alive list (positions/HP may have changed)
            allUnits = _turnManager.GetAliveUnits();
            var targets = chosenCard.GetValidTargetsForAction(i, this, allUnits, Grid);

            if (targets.Count == 0)
            {
                Debug.Log($"  {DisplayName} skips {Hand.DescribeAction(action)} (no targets).");
                continue;
            }

            // Auto-resolve self-targeting status effects (no visible target needed)
            if (action.targetSelf && action.effect == CardEffect.Status)
            {
                Debug.Log($"  {DisplayName} resolves {Hand.DescribeAction(action)} on self.");
                chosenCard.ResolveAction(i, this, allUnits, Grid, Coord);
                TriggerStatuses(StatusTrigger.OnAction);
                yield return new WaitForSeconds(DelayAfterResolve);
                continue;
            }

            // Auto-resolve ReduceCooldown (no visible target needed)
            if (action.effect == CardEffect.ReduceCooldown)
            {
                Debug.Log($"  {DisplayName} resolves {Hand.DescribeAction(action)}.");
                chosenCard.ResolveAction(i, this, allUnits, Grid, Coord);
                yield return new WaitForSeconds(DelayAfterResolve);
                continue;
            }

            // Highlight all valid targets
            HighlightCoords(targets, true);
            yield return new WaitForSeconds(DelayShowTargets);

            // Pick the best target (closest to primary target)
            primaryTarget = FindPrimaryTarget(allUnits) ?? primaryTarget;
            HexCoord best = PickBestTarget(targets, primaryTarget);

            // Clear highlights, then highlight just the chosen target briefly
            HighlightCoords(targets, false);
            HighlightCoord(best, true);
            yield return new WaitForSeconds(DelayShowTargets);

            // Fate draw for attack actions (once per action, before first resolve)
            if (IsDamageEffect(action.effect) && FateManager.Instance != null)
            {
                // Find defender at best target hex (for single-target/line; null for AoE)
                Unit defender = null;
                if (action.effect != CardEffect.AttackAoE)
                {
                    foreach (var u in allUnits)
                    {
                        if (u.IsAlive && u.Coord == best && u != this)
                        { defender = u; break; }
                    }
                }

                bool isPlayerDefender = defender != null && defender.Team == Team.Player;
                yield return StartCoroutine(FateManager.Instance.ResolveFateDraws(
                    this, defender, isPlayerAttacker: false, isPlayerDefender: isPlayerDefender));
            }

            // Resolve
            Debug.Log($"  {DisplayName} resolves {Hand.DescribeAction(action)} at {best}.");
            chosenCard.ResolveAction(i, this, allUnits, Grid, best);
            TriggerStatuses(StatusTrigger.OnAction);

            // Clear chosen highlight
            HighlightCoord(best, false);

            // Multi-target: resolve on additional targets (reuses same fate context)
            if (action.maxTargets > 1)
            {
                var hitSet = new HashSet<HexCoord> { best };
                for (int mt = 1; mt < action.maxTargets; mt++)
                {
                    allUnits = _turnManager.GetAliveUnits();
                    var remaining = chosenCard.GetValidTargetsForAction(i, this, allUnits, Grid);
                    // Filter out already-hit
                    remaining.RemoveAll(c => hitSet.Contains(c));
                    if (remaining.Count == 0) break;

                    yield return new WaitForSeconds(DelayShowTargets);
                    HexCoord nextBest = PickBestTarget(remaining, primaryTarget);
                    HighlightCoord(nextBest, true);
                    yield return new WaitForSeconds(DelayShowTargets);

                    Debug.Log($"  {DisplayName} resolves {Hand.DescribeAction(action)} at {nextBest} (multi-target).");
                    chosenCard.ResolveAction(i, this, allUnits, Grid, nextBest);
                    TriggerStatuses(StatusTrigger.OnAction);
                    HighlightCoord(nextBest, false);
                    hitSet.Add(nextBest);
                }
            }

            // Clear fate context after action completes
            FateCombatContext.Clear();

            yield return new WaitForSeconds(DelayAfterResolve);
        }

        chosenCard.StartCooldown();
        yield return new WaitForSeconds(DelayTurnEnd);
        _turnManager.EndCurrentTurn();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static bool IsDamageEffect(CardEffect effect)
    {
        return effect == CardEffect.Attack
            || effect == CardEffect.AttackAoE
            || effect == CardEffect.AttackLine;
    }

    private Unit FindPrimaryTarget(List<Unit> allUnits)
    {
        Unit best = null;
        int closestDist = int.MaxValue;
        foreach (var u in allUnits)
        {
            if (u.Team == Team || !u.IsAlive) continue;
            int d = Coord.DistanceTo(u.Coord);
            if (d < closestDist) { closestDist = d; best = u; }
        }
        return best;
    }

    private static HexCoord PickBestTarget(List<HexCoord> targets, Unit primaryTarget)
    {
        HexCoord best = targets[0];
        int bestDist = best.DistanceTo(primaryTarget.Coord);
        for (int t = 1; t < targets.Count; t++)
        {
            int dist = targets[t].DistanceTo(primaryTarget.Coord);
            if (dist < bestDist) { bestDist = dist; best = targets[t]; }
        }
        return best;
    }

    /// <summary>
    /// Preview what this enemy would do on its turn (same AI logic, no side effects).
    /// Returns a list of (target hex, effect type) pairs for display.
    /// Also outputs the CardData of the chosen card (null if passing).
    /// </summary>
    public List<(HexCoord target, CardEffect effect)> ComputeIntent(List<Unit> allUnits, out CardData chosenCardData)
    {
        chosenCardData = null;
        var intents = new List<(HexCoord, CardEffect)>();

        Unit primaryTarget = FindPrimaryTarget(allUnits);
        if (primaryTarget == null) return intents;

        // Same card selection logic as ExecuteTurn
        CardInstance chosenCard = null;

        foreach (var card in _cards)
        {
            if (!card.IsReady || card.ActionCount == 0) continue;
            bool canDealDamage = false;
            for (int i = 0; i < card.ActionCount; i++)
            {
                var action = card.GetAction(i);
                var targets = card.GetValidTargetsForAction(i, this, allUnits, Grid);
                if (targets.Count > 0 && IsDamageEffect(action.effect))
                { canDealDamage = true; break; }
            }
            if (canDealDamage) { chosenCard = card; break; }
        }

        if (chosenCard == null)
        {
            foreach (var card in _cards)
            {
                if (!card.IsReady || card.ActionCount == 0) continue;
                for (int i = 0; i < card.ActionCount; i++)
                {
                    var targets = card.GetValidTargetsForAction(i, this, allUnits, Grid);
                    if (targets.Count > 0) { chosenCard = card; break; }
                }
                if (chosenCard != null) break;
            }
        }

        if (chosenCard == null) return intents;

        chosenCardData = chosenCard.Data;

        // Preview each action's best target
        for (int i = 0; i < chosenCard.ActionCount; i++)
        {
            var action = chosenCard.GetAction(i);
            var targets = chosenCard.GetValidTargetsForAction(i, this, allUnits, Grid);
            if (targets.Count == 0) continue;
            if (action.targetSelf || action.effect == CardEffect.ReduceCooldown) continue;

            HexCoord best = PickBestTarget(targets, primaryTarget);
            intents.Add((best, action.effect));
        }

        return intents;
    }

    private void HighlightCoords(List<HexCoord> coords, bool highlight)
    {
        foreach (var coord in coords)
            HighlightCoord(coord, highlight);
    }

    private void HighlightCoord(HexCoord coord, bool highlight)
    {
        if (Grid.TryGetTile(coord, out HexTile tile))
            tile.SetSelected(highlight);
    }
}
