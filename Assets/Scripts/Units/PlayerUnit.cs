using System.Collections;
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
    private enum State { Inactive, Idle, CardSelected, ResolvingFate }

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
    private bool _fateDrawnForAction;

    // Enemy intent hover (chip or panel)
    private EnemyUnit _hoveredEnemy;
    private EnemyUnit _panelHoverEnemy;
    private readonly List<(HexCoord target, HexTile tile)> _intentTiles = new();

    // Intent colours
    private static readonly Color IntentMove   = new Color(1f, 0.75f, 0.25f, 0.7f);   // orange
    private static readonly Color IntentAttack = new Color(1f, 0.25f, 0.20f, 0.7f);    // red

    public void InitHand(CardData[] cardDatas)
    {
        Hand = new Hand(cardDatas);
    }

    private void OnEnable()
    {
        UnitInfoPanel.OnEnemyPanelHoverEnter += HandlePanelHoverEnter;
        UnitInfoPanel.OnEnemyPanelHoverExit += HandlePanelHoverExit;
    }

    private void OnDisable()
    {
        UnitInfoPanel.OnEnemyPanelHoverEnter -= HandlePanelHoverEnter;
        UnitInfoPanel.OnEnemyPanelHoverExit -= HandlePanelHoverExit;
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
        base.OnTurnStart();
        Hand?.TickCooldowns();
        _handUI?.SwitchToHand(Hand);
        _handUI?.Refresh(Hand);
        Debug.Log($"--- {DisplayName}'s turn — pick a card or pass. ---");
        EnterIdle();
    }

    protected override List<Unit> GetAliveUnitsForPassive()
    {
        return _turnManager?.GetAliveUnits() ?? new List<Unit> { this };
    }

    public override void OnTurnEnd()
    {
        _panelHoverEnemy = null;
        ClearIntentHover();
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

    public override void IncreaseRandomCardCooldown(int amount)
    {
        if (Hand == null || Hand.Cards.Count == 0) return;
        int index = Random.Range(0, Hand.Cards.Count);
        var card = Hand.Cards[index];
        card.IncreaseCooldown(amount);
        Debug.Log($"  Poison: {card.Data.cardName} cooldown +{amount}");
        _handUI?.Refresh(Hand);
    }

    private void Update()
    {
        if (_state == State.Idle)
            UpdateEnemyIntentHover();
        else if (_state == State.CardSelected)
            UpdateCardSelected();
    }

    // --- Idle: waiting for card click from HandUI ---

    private void EnterIdle()
    {
        _state = State.Idle;
        _handUI?.SetSelectedCard(-1);
        _handUI?.HideActionStep();
    }

    // ── Panel hover handlers ────────────────────────────────────────────

    private void HandlePanelHoverEnter(EnemyUnit enemy)
    {
        if (_state != State.Idle) return;
        if (!enemy.IsAlive) return;

        // Clear any existing chip-based hover
        ClearIntentHover();

        _panelHoverEnemy = enemy;
        _hoveredEnemy = enemy;
        _hoveredEnemy.SetChipHighlighted(true);

        // Show hex intent highlights (intent popup is already shown by UnitInfoPanel)
        var allAlive = _turnManager.GetAliveUnits();
        var intents = enemy.ComputeIntent(allAlive, out _);

        foreach (var (target, effect) in intents)
        {
            if (!Grid.TryGetTile(target, out HexTile tile)) continue;
            bool isMove = effect == CardEffect.Move || effect == CardEffect.Dash || effect == CardEffect.Jump;
            Color color = isMove ? IntentMove : IntentAttack;
            tile.SetIntentHighlight(true, color);
            _intentTiles.Add((target, tile));
        }
    }

    private void HandlePanelHoverExit(EnemyUnit enemy)
    {
        if (_panelHoverEnemy != enemy) return;
        _panelHoverEnemy = null;
        ClearIntentHover();
    }

    // ── Chip hover ──────────────────────────────────────────────────────

    private void UpdateEnemyIntentHover()
    {
        // If panel hover is active, don't override with chip-based hover
        if (_panelHoverEnemy != null) return;

        var tileUnderMouse = _hexInteraction?.GetTileUnderMouse();
        EnemyUnit enemy = null;

        if (tileUnderMouse != null)
        {
            var allUnits = _turnManager.GetAliveUnits();
            foreach (var u in allUnits)
            {
                if (u is EnemyUnit eu && eu.IsAlive && eu.Coord == tileUnderMouse.Coord)
                {
                    enemy = eu;
                    break;
                }
            }
        }

        // Same enemy — nothing to do
        if (enemy == _hoveredEnemy) return;

        // Clear previous
        ClearIntentHover();

        if (enemy == null) return;

        // Show new intent
        _hoveredEnemy = enemy;
        _hoveredEnemy.SetChipHighlighted(true);

        var allAlive = _turnManager.GetAliveUnits();
        var intents = enemy.ComputeIntent(allAlive, out CardData intentCard);

        // Show intent card popup on the enemy's side panel
        var panel = UnitInfoPanel.GetPanel(enemy);
        if (panel != null && intentCard != null)
            panel.ShowIntent(intentCard);

        foreach (var (target, effect) in intents)
        {
            if (!Grid.TryGetTile(target, out HexTile tile)) continue;

            bool isMove = effect == CardEffect.Move || effect == CardEffect.Dash || effect == CardEffect.Jump;
            Color color = isMove ? IntentMove : IntentAttack;
            tile.SetIntentHighlight(true, color);
            _intentTiles.Add((target, tile));
        }
    }

    private void ClearIntentHover()
    {
        if (_hoveredEnemy != null)
        {
            // Hide intent card popup on the enemy's panel
            var panel = UnitInfoPanel.GetPanel(_hoveredEnemy);
            panel?.HideIntent();

            _hoveredEnemy.SetChipHighlighted(false);
            _hoveredEnemy = null;
        }
        foreach (var (_, tile) in _intentTiles)
            tile.SetIntentHighlight(false);
        _intentTiles.Clear();
    }

    private void HandleCardClicked(int index)
    {
        if (Hand == null || index < 0 || index >= Hand.Cards.Count) return;

        // Allow deselect/switch only when no actions have been resolved yet
        if (_state == State.CardSelected && _currentActionIndex == 0)
        {
            if (index == _selectedCardIndex)
            {
                // Re-click same card → deselect
                CancelCard();
                return;
            }

            // Click different card → switch to it (if ready)
            var other = Hand.Cards[index];
            if (!other.IsReady || other.ActionCount == 0) return;
            CancelCard();
            EnterCardSelected(other, index);
            return;
        }

        // Only allow fresh selection from Idle
        if (_state != State.Idle) return;

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

        if (!CanAffordCard(card.Data))
        {
            Debug.Log($"[{card.Data.cardName}] not enough {card.Data.costType} ({card.Data.costAmount} needed).");
            return;
        }

        EnterCardSelected(card, index);
    }

    private void HandlePassClicked()
    {
        if (_state != State.Idle) return;

        Debug.Log("Player passed.");
        BattleLog.AddAction("Passed");
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
        ClearIntentHover();
        _selectedCard = card;
        _selectedCardIndex = index;
        _currentActionIndex = 0;
        _multiTargetRemaining = 0;
        _multiTargetExclude.Clear();
        _handUI?.SetSelectedCard(index);
        _handUI?.ShowExpandedCard(card.Data);
        _state = State.CardSelected;
        Debug.Log($"[{card.Data.cardName}] selected.");
        BattleLog.AddCardName(card.Data.cardName);
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

        // Auto-resolve self-targeting effects (Status, Heal — no click needed)
        if (action.targetSelf && (action.effect == CardEffect.Status || action.effect == CardEffect.Heal))
        {
            if (targets.Count > 0)
            {
                Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: " +
                          $"{Hand.DescribeAction(action)} — auto-applying to self.");
                _selectedCard.ResolveAction(_currentActionIndex, this, allUnits, Grid, Coord);
                TriggerStatuses(StatusTrigger.OnAction);
            }
            else
            {
                Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: " +
                          $"{Hand.DescribeAction(action)} — skipped (not needed).");
            }
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

        string desc = Hand.DescribeAction(action);
        string multiHint = _multiTargetRemaining > 0
            ? $" [target {action.maxTargets - _multiTargetRemaining + 1}/{action.maxTargets}]"
            : "";

        if (targets.Count == 0)
        {
            // No valid targets — show the step but only allow skip
            bool canSkip = true;
            _handUI?.ShowActionStep(_currentActionIndex + 1, _selectedCard.ActionCount,
                desc + multiHint + " (no targets)", canSkip);
            Debug.Log($"  Action {_currentActionIndex + 1}/{_selectedCard.ActionCount}: " +
                      $"{desc}{multiHint} — no valid targets, click skip.");
            return;
        }

        HighlightTargets(true);
        bool skip = !action.mandatory || _multiTargetRemaining > 0;
        _handUI?.ShowActionStep(_currentActionIndex + 1, _selectedCard.ActionCount, desc + multiHint, skip);
        string hint = skip ? "click a target or skip" : "click a target (mandatory)";
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

        var action = _selectedCard.GetAction(_currentActionIndex);

        // Attack actions: route through fate draw coroutine (first hit only)
        if (IsAttackEffect(action.effect) && !_fateDrawnForAction && FateManager.Instance != null)
        {
            StartCoroutine(ResolveAttackWithFate(clicked, action));
            return;
        }

        // Non-attack actions or multi-target subsequent hits: resolve immediately
        ResolveClickedTarget(clicked);
    }

    private void ResolveClickedTarget(HexCoord clicked)
    {
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

        // Clear fate context when advancing to next action
        FateCombatContext.Clear();
        _fateDrawnForAction = false;
        _currentActionIndex++;
        ShowCurrentAction();
    }

    private IEnumerator ResolveAttackWithFate(HexCoord clicked, CardAction action)
    {
        _state = State.ResolvingFate;
        ClearTargetHover();
        ClearHighlights();

        // Find defender unit at the clicked hex (for single-target; null for AoE)
        Unit defender = null;
        if (action.effect == CardEffect.Attack || action.effect == CardEffect.AttackLine)
        {
            var allUnits = _turnManager.GetAliveUnits();
            foreach (var u in allUnits)
            {
                if (u.IsAlive && u.Coord == clicked && u != this)
                {
                    defender = u;
                    break;
                }
            }
        }

        // Fate draws: player is attacker, defender may be enemy (AI pick) or null
        bool isPlayerDefender = defender != null && defender.Team == Team.Player;
        yield return StartCoroutine(FateManager.Instance.ResolveFateDraws(
            this, defender, isPlayerAttacker: true, isPlayerDefender: isPlayerDefender));

        _fateDrawnForAction = true;
        _state = State.CardSelected;

        // Now resolve the action with fate context set
        ResolveClickedTarget(clicked);
    }

    private static bool IsAttackEffect(CardEffect effect)
    {
        return effect == CardEffect.Attack
            || effect == CardEffect.AttackAoE
            || effect == CardEffect.AttackLine;
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
        SpendCardCost(_selectedCard.Data);
        _selectedCard.StartCooldown();
        ClearTargetHover();
        ClearHighlights();
        _hexInteraction?.ClearSelection();
        _selectedCard = null;
        _selectedCardIndex = -1;
        _validTargetSet = null;
        _multiTargetRemaining = 0;
        _multiTargetExclude.Clear();
        _fateDrawnForAction = false;
        FateCombatContext.Clear();
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
        _fateDrawnForAction = false;
        FateCombatContext.Clear();
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
