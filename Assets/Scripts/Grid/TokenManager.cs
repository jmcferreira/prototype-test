using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all hex tokens/traps on the grid. Singleton pattern.
/// Tokens are placed on hexes and trigger when units enter.
/// Provides visual representation (small cubes) for each token.
/// </summary>
public class TokenManager : MonoBehaviour
{
    public static TokenManager Instance { get; private set; }

    private readonly Dictionary<HexCoord, (HexToken token, GameObject visual)> _tokens = new();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Check if a hex already has a token on it.
    /// </summary>
    public bool HasToken(HexCoord coord) => _tokens.ContainsKey(coord);

    /// <summary>
    /// Place a token on a hex tile. Creates a small cube visual.
    /// </summary>
    public void PlaceToken(HexToken token, HexCoord coord, HexGrid grid)
    {
        // Remove existing token on this hex if any
        if (_tokens.ContainsKey(coord))
            RemoveToken(coord);

        // Create visual: small cube sitting on the hex
        var go = new GameObject($"Token_{token.Name}_{coord}");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(go.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);

        // Remove collider so it doesn't interfere with hex raycasts
        var collider = cube.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = token.Color;
        cube.GetComponent<MeshRenderer>().material = mat;
        cube.name = "TokenCube";

        // Position on the hex
        if (grid.TryGetTile(coord, out HexTile tile))
            go.transform.position = tile.transform.position + Vector3.up * 0.02f;
        else
            go.transform.position = coord.ToWorldPosition(grid.HexSize) + Vector3.up * 0.02f;

        // Small label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform);
        labelGo.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "\u2588"; // █ block character to represent web
        tm.fontSize = 32;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = token.Color;
        tm.fontStyle = FontStyle.Bold;

        _tokens[coord] = (token, go);
    }

    /// <summary>
    /// Remove a token from a hex.
    /// </summary>
    public void RemoveToken(HexCoord coord)
    {
        if (_tokens.TryGetValue(coord, out var entry))
        {
            Object.Destroy(entry.visual);
            _tokens.Remove(coord);
        }
    }

    /// <summary>
    /// Called by movement resolvers when a unit enters a hex.
    /// Checks if there's a token and triggers it.
    /// </summary>
    public void OnUnitEnterHex(Unit unit, HexCoord coord, List<Unit> allUnits)
    {
        if (!_tokens.TryGetValue(coord, out var entry)) return;

        bool consumed = entry.token.OnUnitEnter(unit, allUnits);
        if (consumed)
            RemoveToken(coord);
    }

    /// <summary>
    /// Get the token at a coordinate, if any. Used for UI hover info.
    /// </summary>
    public HexToken GetToken(HexCoord coord)
    {
        return _tokens.TryGetValue(coord, out var entry) ? entry.token : null;
    }
}
