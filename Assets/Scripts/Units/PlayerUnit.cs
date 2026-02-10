using UnityEngine;

/// <summary>
/// Player-controlled unit. Keyboard-only card flow:
///   1–4: select a card
///   For Move: Q/W/E/A/S/D to pick hex direction (E/NE/NW/W/SW/SE)
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

    public void InitHand(CardData[] cardDatas)
    {
        Hand = new Hand(cardDatas);
    }

    public void SetEnemy(Unit enemy)
    {
        _enemy = enemy;
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
                Debug.Log($"[{card.Data.cardName}] selected — pick direction: Q=E, W=NE, E=NW, A=W, S=SW, D=SE");
            }
            else
            {
                // Auto-target the enemy for Attack/Push/Pull
                bool success = CardEffectExecutor.Execute(card, this, _enemy, Grid);
                if (!success)
                {
                    card.ResetCooldown();
                    Debug.Log("Effect failed — card cooldown refunded.");
                }
                _canAct = false;
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
            Debug.Log("Card cancelled — cooldown refunded.");
            Debug.Log("Press 1-4 to play a card, Space to end turn.");
            return;
        }

        // Direction keys: Q=E(0), W=NE(1), E=NW(2), A=W(3), S=SW(4), D=SE(5)
        int dir = -1;
        if (Input.GetKeyDown(KeyCode.Q)) dir = 0;
        else if (Input.GetKeyDown(KeyCode.W)) dir = 1;
        else if (Input.GetKeyDown(KeyCode.E)) dir = 2;
        else if (Input.GetKeyDown(KeyCode.A)) dir = 3;
        else if (Input.GetKeyDown(KeyCode.S)) dir = 4;
        else if (Input.GetKeyDown(KeyCode.D)) dir = 5;

        if (dir < 0) return;

        bool success = CardEffectExecutor.ExecuteMove(_selectedCard.Data, this, Grid, dir);
        if (!success)
        {
            _selectedCard.ResetCooldown();
            Debug.Log("Move failed — card cooldown refunded.");
        }

        _selectedCard = null;
        _canAct = false;
    }
}
