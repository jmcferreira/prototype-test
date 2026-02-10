using UnityEngine;

/// <summary>
/// Player-controlled unit. During their turn:
///   1–4: select a card
///   For Move: click a hex or use Q/W/E/A/S/D for direction
///   For Attack/Push/Pull: auto-targets the enemy
///   Space: end turn without playing a card
///   Escape: cancel card selection
/// </summary>
public class PlayerUnit : Unit
{
    public Hand Hand { get; private set; }

    private bool _canAct;
    private CardInstance _selectedCard;
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

        Hand?.TickCooldowns();
        Debug.Log("--- Your hand ---");
        Hand?.LogHand();
        Debug.Log("Press 1-4 to play a card, Space to end turn.");
    }

    public override void OnTurnEnd()
    {
        _canAct = false;
        _selectedCard = null;
        _hexInteraction?.ClearSelection();
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

            var card = Hand?.TryPlay(i);
            if (card == null) break;

            if (card.Data.effect == CardEffect.Move)
            {
                _selectedCard = card;
                Debug.Log($"[{card.Data.cardName}] selected — click a hex or pick direction: Q=E, W=NE, E=NW, A=W, S=SW, D=SE");
            }
            else
            {
                bool success = CardEffectExecutor.Execute(card, this, _enemy, Grid);
                if (!success)
                {
                    card.ResetCooldown();
                    Debug.Log("Effect failed — card cooldown refunded.");
                }
                FinishAction();
            }
            break;
        }
    }

    private void HandleTargeting()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _selectedCard.ResetCooldown();
            _selectedCard = null;
            _hexInteraction?.ClearSelection();
            Debug.Log("Card cancelled — cooldown refunded.");
            Debug.Log("Press 1-4 to play a card, Space to end turn.");
            return;
        }

        // Keyboard direction: Q=E(0), W=NE(1), E=NW(2), A=W(3), S=SW(4), D=SE(5)
        int dir = -1;
        if (Input.GetKeyDown(KeyCode.Q)) dir = 0;
        else if (Input.GetKeyDown(KeyCode.W)) dir = 1;
        else if (Input.GetKeyDown(KeyCode.E)) dir = 2;
        else if (Input.GetKeyDown(KeyCode.A)) dir = 3;
        else if (Input.GetKeyDown(KeyCode.S)) dir = 4;
        else if (Input.GetKeyDown(KeyCode.D)) dir = 5;

        if (dir >= 0)
        {
            bool success = CardEffectExecutor.ExecuteMove(_selectedCard.Data, this, Grid, dir);
            if (!success)
            {
                _selectedCard.ResetCooldown();
                Debug.Log("Move failed — card cooldown refunded.");
            }
            _selectedCard = null;
            FinishAction();
            return;
        }

        // Mouse click: use the selected tile from HexInteraction
        if (_hexInteraction != null && _hexInteraction.SelectedTile != null && Input.GetMouseButtonDown(0))
        {
            HexCoord target = _hexInteraction.SelectedTile.Coord;
            // Find which direction this hex is relative to us and walk there
            bool success = TryMoveToward(target);
            if (!success)
            {
                _selectedCard.ResetCooldown();
                Debug.Log("Move failed — card cooldown refunded.");
            }
            _selectedCard = null;
            FinishAction();
        }
    }

    private bool TryMoveToward(HexCoord target)
    {
        // Find the direction from current coord toward the target
        int bestDir = -1;
        int bestDist = int.MaxValue;

        for (int i = 0; i < 6; i++)
        {
            HexCoord neighbor = Coord.Neighbor(i);
            int dist = neighbor.DistanceTo(target);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir = i;
            }
        }

        if (bestDir < 0) return false;
        return CardEffectExecutor.ExecuteMove(_selectedCard.Data, this, Grid, bestDir);
    }

    private void FinishAction()
    {
        _canAct = false;
        _hexInteraction?.ClearSelection();
    }
}
