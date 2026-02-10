using UnityEngine;

/// <summary>
/// Player-controlled unit. During their turn, click an adjacent hex to move,
/// or press 1–4 to play a card (effect execution not implemented yet).
/// </summary>
public class PlayerUnit : Unit
{
    public Hand Hand { get; private set; }

    private bool _canAct;
    private bool _cardPlayed;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    public void InitHand(CardData[] cardDatas)
    {
        Hand = new Hand(cardDatas);
    }

    public override void OnTurnStart()
    {
        _canAct = true;
        _cardPlayed = false;

        Hand?.TickCooldowns();
        Debug.Log("--- Your hand ---");
        Hand?.LogHand();
        Debug.Log("Click adjacent hex to move, 1-4 to play a card, Space to end turn.");
    }

    public override void OnTurnEnd()
    {
        _canAct = false;
    }

    private void Update()
    {
        if (!_canAct) return;

        HandleCardInput();
        HandleMoveInput();
    }

    private void HandleCardInput()
    {
        if (_cardPlayed) return;

        // Keys 1–4 map to card indices 0–3
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                var played = Hand?.TryPlay(i);
                if (played != null)
                {
                    _cardPlayed = true;
                    Debug.Log($"Card effect: {played.Data.effect} (execution not wired yet)");
                }
                break;
            }
        }
    }

    private void HandleMoveInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 worldPoint = ray.GetPoint(enter);
        HexCoord clicked = HexCoord.FromWorldPosition(worldPoint);

        if (TryMoveTo(clicked))
        {
            _canAct = false; // one move per turn
        }
        else
        {
            Debug.Log($"Can't move to {clicked} — must be an adjacent tile.");
        }
    }
}
