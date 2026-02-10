using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-controlled unit with an explicit two-state interaction model:
///   Idle         → press 1–4 to select a card, or Space to end turn
///   CardSelected → valid targets highlighted, click one to resolve and end turn
/// </summary>
public class PlayerUnit : Unit
{
    private enum State { Inactive, Idle, CardSelected }

    public Hand Hand { get; private set; }

    private State _state = State.Inactive;
    private CardInstance _selectedCard;
    private List<HexCoord> _validTargets;
    private Unit _enemy;
    private HexInteraction _hexInteraction;
    private TurnManager _turnManager;

    public void InitHand(CardData[] cardDatas)
    {
        Hand = new Hand(cardDatas);
    }

    public void SetEnemy(Unit enemy)
    {
        _enemy = enemy;
    }

    public void SetHexInteraction(HexInteraction hexInteraction)
    {
        _hexInteraction = hexInteraction;
    }

    public void SetTurnManager(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }

    public override void OnTurnStart()
    {
        Hand?.TickCooldowns();
        Debug.Log("--- Your hand ---");
        Hand?.LogHand();
        Debug.Log("Press 1-4 to play a card, Space to end turn.");
        EnterIdle();
    }

    public override void OnTurnEnd()
    {
        ClearHighlights();
        _selectedCard = null;
        _validTargets = null;
        _state = State.Inactive;
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.CardSelected:
                UpdateCardSelected();
                break;
        }
    }

    // --- Idle: waiting for card selection or pass ---

    private void EnterIdle()
    {
        _state = State.Idle;
    }

    private void UpdateIdle()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Player passed.");
            _turnManager.EndCurrentTurn();
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            var card = Hand?.Cards[i];
            if (card == null || !card.IsReady)
            {
                Debug.Log(card == null
                    ? $"No card at slot {i + 1}."
                    : $"[{card.Data.cardName}] on cooldown ({card.CooldownRemaining} turns).");
                break;
            }

            var targets = card.GetValidTargets(this, _enemy, Grid);
            if (targets.Count == 0)
            {
                Debug.Log($"[{card.Data.cardName}] has no valid targets.");
                break;
            }

            EnterCardSelected(card, targets);
            break;
        }
    }

    // --- CardSelected: waiting for target click ---

    private void EnterCardSelected(CardInstance card, List<HexCoord> targets)
    {
        _selectedCard = card;
        _validTargets = targets;
        HighlightTargets(true);
        _state = State.CardSelected;
        Debug.Log($"[{card.Data.cardName}] selected — click a highlighted hex to resolve.");
    }

    private void UpdateCardSelected()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var tile = _hexInteraction?.SelectedTile;
        if (tile == null) return;

        HexCoord clicked = tile.Coord;

        if (!_validTargets.Contains(clicked))
        {
            Debug.Log($"{clicked} is not a valid target for [{_selectedCard.Data.cardName}].");
            return;
        }

        // Resolve and end turn
        _selectedCard.Resolve(this, _enemy, Grid, clicked);
        ClearHighlights();
        _hexInteraction?.ClearSelection();
        _selectedCard = null;
        _validTargets = null;
        _turnManager.EndCurrentTurn();
    }

    // --- Helpers ---

    private void HighlightTargets(bool highlight)
    {
        if (_validTargets == null) return;
        foreach (var coord in _validTargets)
        {
            if (Grid.TryGetTile(coord, out HexTile tile))
                tile.SetSelected(highlight);
        }
    }

    private void ClearHighlights()
    {
        HighlightTargets(false);
    }
}
