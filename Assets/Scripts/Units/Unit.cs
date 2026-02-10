using UnityEngine;

public enum Team { Player, Enemy }

/// <summary>
/// Base class for any actor that occupies a hex tile.
/// </summary>
public abstract class Unit : MonoBehaviour
{
    public Team Team { get; private set; }
    public HexCoord Coord { get; private set; }
    public int HP { get; private set; } = 3;
    public bool IsAlive => HP > 0;

    private HexGrid _grid;
    private float _hexSize;
    private TextMesh _hpText;

    public void Init(Team team, HexCoord startCoord, HexGrid grid)
    {
        Team = team;
        _grid = grid;
        _hexSize = grid.HexSize;
        Coord = startCoord;
        PlaceAt(startCoord);
        gameObject.name = $"{team} Unit";

        // Simple colored cube so we can tell them apart
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = team == Team.Player ? Color.blue : Color.red;
        cube.GetComponent<MeshRenderer>().material = mat;

        // HP label floating above the unit
        var textGo = new GameObject("HP Label");
        textGo.transform.SetParent(transform);
        textGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        // Rotate to face the top-down camera (text on XZ plane, readable from Y+)
        textGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _hpText = textGo.AddComponent<TextMesh>();
        _hpText.characterSize = 0.2f;
        _hpText.fontSize = 48;
        _hpText.anchor = TextAnchor.MiddleCenter;
        _hpText.alignment = TextAlignment.Center;
        _hpText.color = Color.white;
        UpdateHPLabel();
    }

    /// <summary>
    /// Move this unit to an adjacent hex. Returns true if the move was valid.
    /// </summary>
    public bool TryMoveTo(HexCoord target)
    {
        if (Coord.DistanceTo(target) != 1) return false;
        if (!_grid.TryGetTile(target, out _)) return false;
        if (IsOccupied(target)) return false;

        PlaceAt(target);
        Debug.Log($"{Team} moved from {Coord} to {target}");
        Coord = target;
        return true;
    }

    private static bool IsOccupied(HexCoord coord)
    {
        foreach (var unit in FindObjectsOfType<Unit>())
        {
            if (unit.Coord == coord) return true;
        }
        return false;
    }

    /// <summary>
    /// Unconditionally move this unit to a hex (used by card effects like push/pull/dash).
    /// </summary>
    public void ForceMoveTo(HexCoord target)
    {
        Coord = target;
        PlaceAt(target);
    }

    /// <summary>
    /// Take damage. Logs HP remaining.
    /// </summary>
    public void TakeHit(int damage)
    {
        HP -= damage;
        UpdateHPLabel();
        Debug.Log($"{Team} took {damage} damage — HP: {HP}");
    }

    private void UpdateHPLabel()
    {
        if (_hpText != null)
        {
            // U+2665 = filled heart, one per HP
            string hearts = new string('\u2665', Mathf.Max(0, HP));
            _hpText.text = hearts;
        }
    }

    public HexGrid Grid => _grid;

    private void PlaceAt(HexCoord coord)
    {
        transform.position = coord.ToWorldPosition(_hexSize) + Vector3.up * 0.1f;
    }

    /// <summary>Called by TurnManager when this unit's turn begins.</summary>
    public virtual void OnTurnStart() { }

    /// <summary>Called by TurnManager when this unit's turn ends.</summary>
    public virtual void OnTurnEnd() { }
}
