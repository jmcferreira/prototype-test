using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy unit. On its turn, scans its cards for one with valid targets
/// and resolves it against the player. One action per turn.
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
        // Try each card in priority order until one has valid targets
        foreach (var card in _cards)
        {
            var targets = card.GetValidTargets(this, _player, Grid);
            if (targets.Count == 0) continue;

            // Pick the target closest to the player (for move: walk toward them)
            HexCoord best = targets[0];
            int bestDist = best.DistanceTo(_player.Coord);
            for (int i = 1; i < targets.Count; i++)
            {
                int dist = targets[i].DistanceTo(_player.Coord);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = targets[i];
                }
            }

            Debug.Log($"Enemy plays [{card.Data.cardName}] targeting {best}.");
            card.Resolve(this, _player, Grid, best);

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
