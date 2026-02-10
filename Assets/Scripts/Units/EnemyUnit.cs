using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy unit. On its turn, picks the first ready card and executes
/// all of its actions automatically (resolve if valid target, skip otherwise).
/// </summary>
public class EnemyUnit : Unit
{
    private List<CardInstance> _cards;
    private Unit _player;
    private TurnManager _turnManager;

    public void InitCards(CardData[] cardDatas)
    {
        _cards = new List<CardInstance>();
        foreach (var data in cardDatas)
            _cards.Add(new CardInstance(data));
    }

    public void SetPlayer(Unit player)
    {
        _player = player;
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

        // Try each card in priority order
        foreach (var card in _cards)
        {
            if (!card.IsReady || card.ActionCount == 0) continue;

            // Check if at least one action has valid targets
            bool anyActionValid = false;
            for (int i = 0; i < card.ActionCount; i++)
            {
                var targets = card.GetValidTargetsForAction(i, this, _player, Grid);
                if (targets.Count > 0) { anyActionValid = true; break; }
            }
            if (!anyActionValid) continue;

            Debug.Log($"Enemy plays [{card.Data.cardName}].");

            // Execute all actions in order
            for (int i = 0; i < card.ActionCount; i++)
            {
                var action = card.GetAction(i);
                var targets = card.GetValidTargetsForAction(i, this, _player, Grid);

                if (targets.Count == 0)
                {
                    Debug.Log($"  Enemy skips {Hand.DescribeAction(action)} (no targets).");
                    continue;
                }

                // Pick the target closest to the player
                HexCoord best = targets[0];
                int bestDist = best.DistanceTo(_player.Coord);
                for (int t = 1; t < targets.Count; t++)
                {
                    int dist = targets[t].DistanceTo(_player.Coord);
                    if (dist < bestDist) { bestDist = dist; best = targets[t]; }
                }

                Debug.Log($"  Enemy resolves {Hand.DescribeAction(action)} at {best}.");
                card.ResolveAction(i, this, _player, Grid, best);
                TriggerStatuses(StatusTrigger.OnAction); // Burn etc.
            }

            card.StartCooldown();
            Invoke(nameof(EndTurn), 0.05f);
            return;
        }

        Debug.Log("Enemy has no playable cards — passing.");
        Invoke(nameof(EndTurn), 0.05f);
    }

    private void EndTurn()
    {
        _turnManager.EndCurrentTurn();
    }
}
