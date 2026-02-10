using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-controlled unit. States:
///   Idle         → click a card in the HandUI, or click Pass
///   CardSelected → step through each action: pick target or skip, then advance
/// Supports multi-target attacks, auto-resolve effects, and ReduceCooldown.
/// </summary>
public class PlayerUnit : Unit
{
    private enum State { Inactive, Idle, CardSelected }

    public Hand Hand { get; private set; }

    private State _state = State.Inactive;
    private CardInstance _selectedCard;
    private int _selectedCardIndex = -1;
    private int _currentActionIndex;
    private List<HexCoord> _validTargets;
    private HashSet<HexCoord> _validTargetSet;
    private HexTile _hoveredTargetTile;
    private HexInteraction _hexInteraction;
    private TurnManager _turnManager;
    private HandUI _handUI;

    // Multi-target tracking
    private int _multiTargetRemaining;
    private HashSet<HexCoord> _multiTargetExclude = new();

    public void InitHand(CardData[] cardDatas)
    {
        Hand = new Hand(cardDatas);
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
        _handUI.OnSkipClicked += HandleSkipClicked;
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
        _handUI?.HideActionStep();
        _state = State.Inactive;
        base.OnTurnEnd(); // Poison damage + status decay
    }

    public override void ReduceCardCooldown(int cardIndex, int amount)
    {
        Hand?.ReduceCardCooldown(cardIndex, amount);
        _handUI?.Refresh(Hand);
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
        _handUI?.HideActionStep();
    }

    private void HandleCardClicked(int index)
    {
        // Only allow card selection/deselection from Idle
        if (_state != State.Idle) return;

        if (Hand == null || index < 0 || index >= Hand.Cards.Count) return;

        var card = Hand.Cards[index];
        if (!card.IsReady)
        {
            Debug.Log($"[{card.Data.cardName}] on cooldown ({card.CooldownRemaining} turns).");
            return;
        }

        if (card.ActionCount == 0)
        {
            Debug.Log($"[{card.Data.cardName}] has no actions.");
            return;
        }

        EnterCardSelected(card, index);
    }

    private void HandlePassClicked()
    {
        if (_state != State.Idle) return;

        Debug.Log("Player passed.");
        _turnManager.EndCurrentTurn();
    }

    private void HandleSkipClicked()
    {
        if (_state != State.CardSelected) return;

        // If in multi-target mode, skip ends the remaining hits
        if (_multiTargetRemaining > 0)
        {
            Debug.Log($"  Skipped remaining multi-target hits.");
            ClearTargetHover();
            ClearHighlights();
            _multiTargetRemaining = 0;
            _multiTargetExclude.Clear();
            _currentActionIndex++;
            ShowCurrentAction();
            return;
        }

        var action = _selectedCard.GetAction(_currentActionIndex);
        if (action.mandatory) return; // can't skip mandatory actions
        SkipCurrentAction();
    }

    // --- CardSelected: stepping through actions ---

    private void EnterCardSelected(CardInstance card, int index)
    {
        _selectedCard = card;
        _selectedCardIndex = index;
        _currentActionIndex = 0;
        _multiTargetRemaining = 0;
        _multiTargetExclude.Clear();
        _handUI?.SetSelectedCard(index);
        _handUI?.ShowExpandedCard(card.Data);
        _state = State.CardSelected;
        Debug.Log($"[{card.Data.cardName}] selected.");
        ShowCurrentAction();
    }

    /// <summary>
    /// Show valid targets for the current action. Auto-skips if none available.
    /// Auto-resolves self-targeting status effects, ReduceCooldown, and PushAoE.
    /// </summary>
    private void ShowCurrentAction()
    {
        // Past the last action → card is done
        if (_currentActionIndex >= _selectedCard.ActionCount)
        {
            FinishCard();
            return;
        }

        var action = _selectedCard.GetAction(_currentActionIndex);
        var allUnits = _turnManager.GetAliveUnits();
        var targets = _selectedCard.GetValidTargetsForAction(_currentActionIndex, this, allUnits, Grid);

        if (targets.Count == 0)
        {
            // Auto-skip actions with no valid targets
            Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: " +
                      $"{Hand.DescribeAction(action)} — no valid targets, skipping.");
            _currentActionIndex++;
            ShowCurrentAction();
            return;
        }

        // Auto-resolve self-targeting status effects (no click needed)
        if (action.targetSelf && action.effect == CardEffect.Status)
        {
            Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: " +
                      $"{Hand.DescribeAction(action)} — auto-applying to self.");
            _selectedCard.ResolveAction(_currentActionIndex, this, allUnits, Grid, Coord);
            TriggerStatuses(StatusTrigger.OnAction);
            _currentActionIndex++;
            ShowCurrentAction();
            return;
        }

        // Auto-resolve ReduceCooldown (no click needed)
        if (action.effect == CardEffect.ReduceCooldown)
        {
            Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: " +
                      $"{Hand.DescribeAction(action)} — auto-resolving.");
            _selectedCard.ResolveAction(_currentActionIndex, this, allUnits, Grid, Coord);
            _currentActionIndex++;
            ShowCurrentAction();
            return;
        }

        // Set up multi-target if needed (first entry into this action)
        if (_multiTargetRemaining == 0 && action.maxTargets > 1)
        {
            _multiTargetRemaining = action.maxTargets;
            _multiTargetExclude.Clear();
        }

        // Filter out already-hit targets for multi-target
        if (_multiTargetRemaining > 0 && _multiTargetExclude.Count > 0)
        {
            var filtered = new List<HexCoord>();
            foreach (var t in targets)
            {
                if (!_multiTargetExclude.Contains(t))
                    filtered.Add(t);
            }
            targets = filtered;

            if (targets.Count == 0)
            {
                Debug.Log($"  No more valid targets for multi-target.");
                _multiTargetRemaining = 0;
                _multiTargetExclude.Clear();
                _currentActionIndex++;
                ShowCurrentAction();
                return;
            }
        }

        _validTargets = targets;
        _validTargetSet = new HashSet<HexCoord>(targets);
        _hoveredTargetTile = null;
        HighlightTargets(true);

        string desc = Hand.DescribeAction(action);
        bool canSkip = !action.mandatory || _multiTargetRemaining > 0;
        string multiHint = _multiTargetRemaining > 0
            ? $" [target {action.maxTargets - _multiTargetRemaining + 1}/{action.maxTargets}]"
            : "";
        _handUI?.ShowActionStep(_currentActionIndex + 1, _selectedCard.ActionCount, desc + multiHint, canSkip);
        string hint = canSkip ? "click a target or skip" : "click a target (mandatory)";
        Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: {desc}{multiHint} — {hint}.");
    }

    private void UpdateCardSelected()
    {
        // Right-click to cancel entire card (back to Idle)
        if (Input.GetMouseButtonDown(1))
        {
            CancelCard();
            return;
        }

        // Track hover over valid targets
        var tileUnderMouse = _hexInteraction?.GetTileUnderMouse();
        UpdateTargetHover(tileUnderMouse);

        if (!Input.GetMouseButtonDown(0)) return;
        if (tileUnderMouse == null) return;

        HexCoord clicked = tileUnderMouse.Coord;
        if (!_validTargetSet.Contains(clicked)) return;

        // Resolve this action
        ClearTargetHover();
        ClearHighlights();
        var allUnits = _turnManager.GetAliveUnits();
        _selectedCard.ResolveAction(_currentActionIndex, this, allUnits, Grid, clicked);
        TriggerStatuses(StatusTrigger.OnAction); // Burn etc.

        // Multi-target: resolve one hit, continue if more remain
        if (_multiTargetRemaining > 0)
        {
            _multiTargetRemaining--;
            _multiTargetExclude.Add(clicked);

            if (_multiTargetRemaining > 0)
            {
                // Show remaining targets for next hit
                ShowCurrentAction();
                return;
            }
            else
            {
                // All hits done
                _multiTargetExclude.Clear();
            }
        }

        _currentActionIndex++;
        ShowCurrentAction();
    }

    private void SkipCurrentAction()
    {
        var action = _selectedCard.GetAction(_currentActionIndex);
        Debug.Log($"  Skipped {Hand.DescribeAction(action)}.");
        ClearTargetHover();
        ClearHighlights();
        _currentActionIndex++;
        ShowCurrentAction();
    }

    private void FinishCard()
    {
        _selectedCard.StartCooldown();
        ClearTargetHover();
        ClearHighlights();
        _hexInteraction?.ClearSelection();
        _selectedCard = null;
        _selectedCardIndex = -1;
        _validTargetSet = null;
        _multiTargetRemaining = 0;
        _multiTargetExclude.Clear();
        _handUI?.SetSelectedCard(-1);
        _handUI?.HideActionStep();
        _handUI?.Refresh(Hand);
        _turnManager.EndCurrentTurn();
    }

    private void CancelCard()
    {
        ClearTargetHover();
        ClearHighlights();
        _selectedCard = null;
        _selectedCardIndex = -1;
        _validTargetSet = null;
        _multiTargetRemaining = 0;
        _multiTargetExclude.Clear();
        Debug.Log("Card cancelled.");
        EnterIdle();
    }

    // --- Hover helpers ---

    private void UpdateTargetHover(HexTile tileUnderMouse)
    {
        if (_hoveredTargetTile != null && _hoveredTargetTile != tileUnderMouse)
            _hoveredTargetTile.SetTargetHovered(false);

        if (tileUnderMouse != null && _validTargetSet != null && _validTargetSet.Contains(tileUnderMouse.Coord))
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

    // --- Highlight helpers ---

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
