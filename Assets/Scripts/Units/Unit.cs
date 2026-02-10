using UnityEngine;

public enum Team { Player, Enemy }

/// <summary>
/// An actor that occupies a hex tile on the grid.
/// </summary>
public class Unit : MonoBehaviour
{
    public Team Team { get; private set; }
    public HexCoord Coord { get; private set; }

    public void Init(Team team, HexCoord startCoord, float hexSize)
    {
        Team = team;
        Coord = startCoord;
        transform.position = startCoord.ToWorldPosition(hexSize) + Vector3.up * 0.1f;
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
}
