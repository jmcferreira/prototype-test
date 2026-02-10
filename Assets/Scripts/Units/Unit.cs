using UnityEngine;

public enum Team { Player, Enemy }

/// <summary>
/// Base class for any actor that occupies a hex tile.
/// </summary>
public abstract class Unit : MonoBehaviour
{
    public Team Team { get; private set; }
    public HexCoord Coord { get; private set; }

    private HexGrid _grid;
    private float _hexSize;

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
    }

    /// <summary>
    /// Move this unit to an adjacent hex. Returns true if the move was valid.
    /// </summary>
    public bool TryMoveTo(HexCoord target)
    {
        if (Coord.DistanceTo(target) != 1) return false;
        if (!_grid.TryGetTile(target, out _)) return false;

        PlaceAt(target);
        Debug.Log($"{Team} moved from {Coord} to {target}");
        Coord = target;
        return true;
    }

    private void PlaceAt(HexCoord coord)
    {
        transform.position = coord.ToWorldPosition(_hexSize) + Vector3.up * 0.1f;
    }

    /// <summary>Called by TurnManager when this unit's turn begins.</summary>
    public virtual void OnTurnStart() { }

    /// <summary>Called by TurnManager when this unit's turn ends.</summary>
    public virtual void OnTurnEnd() { }
}
