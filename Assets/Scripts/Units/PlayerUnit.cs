using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-controlled unit. During their turn:
///   1–4: select a card → valid targets are highlighted
///   Click a highlighted hex to resolve the card
///   Space: end turn without playing a card
///   Escape: cancel card selection
/// Cards handle their own targeting via ICardResolver — PlayerUnit
/// only manages the select → target → resolve flow.
/// </summary>
public class PlayerUnit : Unit
{
    public Hand Hand { get; private set; }

    private bool _canAct;
    private CardInstance _selectedCard;
    private List<HexCoord> _validTargets;
    private Unit _enemy;
    private HexInteraction _hexInteraction;

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

    public override void OnTurnStart()
    {
        _canAct = true;
        _selectedCard = null;
        _validTargets = null;

        Hand?.TickCooldowns();
        Debug.Log("--- Your hand ---");
        Hand?.LogHand();
        Debug.Log("Press 1-4 to play a card, Space to end turn.");
    }

    public override void OnTurnEnd()
    {
        _canAct = false;
        CancelSelection();
    }

    private void Update()
    {
        if (!_canAct) return;

        if (_selectedCard != null)
        {
            HandleTargeting();
        }
        else
        {
            HandleCardSelection();
        }
    }

    private void HandleCardSelection()
    {
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

            _selectedCard = card;
            _validTargets = targets;
            HighlightTargets(true);
            Debug.Log($"[{card.Data.cardName}] selected — click a highlighted hex to resolve. Escape to cancel.");
            break;
        }
    }

    private void HandleTargeting()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Card cancelled.");
            CancelSelection();
            Debug.Log("Press 1-4 to play a card, Space to end turn.");
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;

        var tile = _hexInteraction?.SelectedTile;
        if (tile == null) return;

        HexCoord clicked = tile.Coord;

        if (!_validTargets.Contains(clicked))
        {
            Debug.Log($"{clicked} is not a valid target for [{_selectedCard.Data.cardName}].");
            return;
        }

        // Resolve the effect
        _selectedCard.Resolve(this, _enemy, Grid, clicked);
        HighlightTargets(false);
        _selectedCard = null;
        _validTargets = null;
        _canAct = false;
        _hexInteraction?.ClearSelection();
    }

    private void CancelSelection()
    {
        if (_validTargets != null)
            HighlightTargets(false);

        _selectedCard = null;
        _validTargets = null;
        _hexInteraction?.ClearSelection();
    }

    private void HighlightTargets(bool highlight)
    {
        if (_validTargets == null) return;

        foreach (var coord in _validTargets)
        {
            if (Grid.TryGetTile(coord, out HexTile tile))
                tile.SetSelected(highlight);
        }
    }
}
