using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy unit. On its turn, picks the first ready card and executes
/// all of its actions step-by-step with visible delays (like the player flow).
/// Highlights valid targets, picks the best one, resolves, then advances.
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

    public override void OnTurnStart()
    {
        // Tick cooldowns
        foreach (var card in _cards)
            card.TickCooldown();

        StartCoroutine(ExecuteTurn());
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
            yield return new WaitForSeconds(DelayTurnEnd);
            _turnManager.EndCurrentTurn();
            yield break;
        }

        // Try each card in priority order
        CardInstance chosenCard = null;
        foreach (var card in _cards)
        {
            if (!card.IsReady || card.ActionCount == 0) continue;

            // Check if at least one action has valid targets
            bool anyActionValid = false;
            for (int i = 0; i < card.ActionCount; i++)
            {
                var targets = card.GetValidTargetsForAction(i, this, allUnits, Grid);
                if (targets.Count > 0) { anyActionValid = true; break; }
            }
            if (!anyActionValid) continue;

            chosenCard = card;
            break;
        }

        if (chosenCard == null)
        {
            Debug.Log($"{DisplayName} has no playable cards — passing.");
            yield return new WaitForSeconds(DelayTurnEnd);
            _turnManager.EndCurrentTurn();
            yield break;
        }

        Debug.Log($"{DisplayName} plays [{chosenCard.Data.cardName}].");

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

            // Resolve
            Debug.Log($"  {DisplayName} resolves {Hand.DescribeAction(action)} at {best}.");
            chosenCard.ResolveAction(i, this, allUnits, Grid, best);
            TriggerStatuses(StatusTrigger.OnAction);

            // Clear chosen highlight
            HighlightCoord(best, false);
            yield return new WaitForSeconds(DelayAfterResolve);
        }

        chosenCard.StartCooldown();
        yield return new WaitForSeconds(DelayTurnEnd);
        _turnManager.EndCurrentTurn();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

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
