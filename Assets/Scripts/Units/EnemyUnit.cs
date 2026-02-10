using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy unit. On its turn, picks the first ready card and executes
/// all of its actions automatically (resolve if valid target, skip otherwise).
/// Targets the closest enemy-team unit (the player) by default.
/// </summary>
public class EnemyUnit : Unit
{
    private List<CardInstance> _cards;
    private TurnManager _turnManager;

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

        var allUnits = _turnManager.GetAliveUnits();

        // Find primary target (closest enemy-team unit, i.e. the player)
        Unit primaryTarget = null;
        int closestDist = int.MaxValue;
        foreach (var u in allUnits)
        {
            if (u.Team == Team || !u.IsAlive) continue;
            int d = Coord.DistanceTo(u.Coord);
            if (d < closestDist) { closestDist = d; primaryTarget = u; }
        }

        if (primaryTarget == null)
        {
            Debug.Log($"{DisplayName} has no targets — passing.");
            Invoke(nameof(EndTurn), 0.05f);
            return;
        }

        // Try each card in priority order
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

            Debug.Log($"{DisplayName} plays [{card.Data.cardName}].");

            // Execute all actions in order
            for (int i = 0; i < card.ActionCount; i++)
            {
                var action = card.GetAction(i);
                // Refresh alive list in case positions/HP changed
                allUnits = _turnManager.GetAliveUnits();
                var targets = card.GetValidTargetsForAction(i, this, allUnits, Grid);

                if (targets.Count == 0)
                {
                    Debug.Log($"  {DisplayName} skips {Hand.DescribeAction(action)} (no targets).");
                    continue;
                }

                // Pick the target closest to primary target
                HexCoord best = targets[0];
                int bestDist = best.DistanceTo(primaryTarget.Coord);
                for (int t = 1; t < targets.Count; t++)
                {
                    int dist = targets[t].DistanceTo(primaryTarget.Coord);
                    if (dist < bestDist) { bestDist = dist; best = targets[t]; }
                }

                Debug.Log($"  {DisplayName} resolves {Hand.DescribeAction(action)} at {best}.");
                card.ResolveAction(i, this, allUnits, Grid, best);
                TriggerStatuses(StatusTrigger.OnAction); // Burn etc.
            }

            card.StartCooldown();
            Invoke(nameof(EndTurn), 0.05f);
            return;
        }

        Debug.Log($"{DisplayName} has no playable cards — passing.");
        Invoke(nameof(EndTurn), 0.05f);
    }

    private void EndTurn()
    {
        _turnManager.EndCurrentTurn();
    }
}
