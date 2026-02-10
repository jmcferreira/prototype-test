using UnityEngine;

/// <summary>
/// Player-controlled unit. During their turn, click an adjacent hex to move.
/// </summary>
public class PlayerUnit : Unit
{
    private bool _canAct;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    public override void OnTurnStart()
    {
        _canAct = true;
        Debug.Log("Click an adjacent hex to move (or press Space to skip).");
    }

    public override void OnTurnEnd()
    {
        _canAct = false;
    }

    private void Update()
    {
        if (!_canAct) return;
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        // Raycast onto the XZ ground plane at y=0
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
