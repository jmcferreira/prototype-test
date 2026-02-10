using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-controlled unit with an explicit two-state interaction model:
///   Idle         → click a card in the HandUI to select it, or click Pass
///   CardSelected → valid targets highlighted, click one to resolve and end turn
/// </summary>
public class PlayerUnit : Unit
{
    private enum State { Inactive, Idle, CardSelected }

    public Hand Hand { get; private set; }

    private State _state = State.Inactive;
    private CardInstance _selectedCard;
    private int _selectedCardIndex = -1;
    private List<HexCoord> _validTargets;
    private HashSet<HexCoord> _validTargetSet;
    private HexTile _hoveredTargetTile;
    private Unit _enemy;
    private HexInteraction _hexInteraction;
    private TurnManager _turnManager;
    private HandUI _handUI;

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

    public void SetHandUI(HandUI handUI)
    {
        _handUI = handUI;
        _handUI.OnCardClicked += HandleCardClicked;
        _handUI.OnPassClicked += HandlePassClicked;
    }

    public override void OnTurnStart()
    {
        Hand?.TickCooldowns();
        _handUI?.Refresh(Hand);
        Debug.Log("--- Your turn — pick a card or pass. ---");
        EnterIdle();
    }

    public override void OnTurnEnd()
    {
        ClearTargetHover();
        ClearHighlights();
        _selectedCard = null;
        _selectedCardIndex = -1;
        _validTargetSet = null;
        _handUI?.SetSelectedCard(-1);
        _state = State.Inactive;
    }

    private void Update()
    {
        if (_state == State.CardSelected)
            UpdateCardSelected();
    }

    // --- Idle: waiting for card click from HandUI ---

    private void EnterIdle()
    {
        _state = State.Idle;
        _handUI?.SetSelectedCard(-1);
    }

    private void HandleCardClicked(int index)
    {
        if (_state != State.Idle) return;

        if (Hand == null || index < 0 || index >= Hand.Cards.Count) return;

        var card = Hand.Cards[index];
        if (!card.IsReady)
        {
            Debug.Log($"[{card.Data.cardName}] on cooldown ({card.CooldownRemaining} turns).");
            return;
        }

        var targets = card.GetValidTargets(this, _enemy, Grid);
        if (targets.Count == 0)
        {
            Debug.Log($"[{card.Data.cardName}] has no valid targets.");
            return;
        }

        EnterCardSelected(card, index, targets);
    }

    private void HandlePassClicked()
    {
        if (_state != State.Idle) return;

        Debug.Log("Player passed.");
        _turnManager.EndCurrentTurn();
    }

    // --- CardSelected: waiting for target click ---

    private void EnterCardSelected(CardInstance card, int index, List<HexCoord> targets)
    {
        _selectedCard = card;
        _selectedCardIndex = index;
        _validTargets = targets;
        _validTargetSet = new HashSet<HexCoord>(targets);
        _hoveredTargetTile = null;
        HighlightTargets(true);
        _handUI?.SetSelectedCard(index);
        _state = State.CardSelected;
        Debug.Log($"[{card.Data.cardName}] selected — click a highlighted hex.");
    }

    private void UpdateCardSelected()
    {
        // Track hover over valid targets
        var tileUnderMouse = _hexInteraction?.GetTileUnderMouse();
        UpdateTargetHover(tileUnderMouse);

        if (!Input.GetMouseButtonDown(0)) return;
        if (tileUnderMouse == null) return;

        HexCoord clicked = tileUnderMouse.Coord;

        if (!_validTargetSet.Contains(clicked)) return;

        // Resolve and end turn
        ClearTargetHover();
        _selectedCard.Resolve(this, _enemy, Grid, clicked);
        ClearHighlights();
        _hexInteraction?.ClearSelection();
        _selectedCard = null;
        _selectedCardIndex = -1;
        _validTargetSet = null;
        _handUI?.SetSelectedCard(-1);
        _handUI?.Refresh(Hand);
        _turnManager.EndCurrentTurn();
    }

    private void UpdateTargetHover(HexTile tileUnderMouse)
    {
        // Clear previous hover
        if (_hoveredTargetTile != null && _hoveredTargetTile != tileUnderMouse)
            _hoveredTargetTile.SetTargetHovered(false);

        // Apply hover if over a valid target
        if (tileUnderMouse != null && _validTargetSet.Contains(tileUnderMouse.Coord))
        {
            tileUnderMouse.SetTargetHovered(true);
            _hoveredTargetTile = tileUnderMouse;
        }
        else
        {
            _hoveredTargetTile = null;
        }
    }

    private void ClearTargetHover()
    {
        if (_hoveredTargetTile != null)
        {
            _hoveredTargetTile.SetTargetHovered(false);
            _hoveredTargetTile = null;
        }
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
