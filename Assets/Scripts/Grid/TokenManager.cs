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
    /// Place a token on a hex tile. Creates an appropriate visual per token type.
    /// </summary>
    public void PlaceToken(HexToken token, HexCoord coord, HexGrid grid)
    {
        // Remove existing token on this hex if any
        if (_tokens.ContainsKey(coord))
            RemoveToken(coord);

        var go = new GameObject($"Token_{token.Name}_{coord}");

        // Position on the hex
        if (grid.TryGetTile(coord, out HexTile tile))
            go.transform.position = tile.transform.position + Vector3.up * 0.02f;
        else
            go.transform.position = coord.ToWorldPosition(grid.HexSize) + Vector3.up * 0.02f;

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = token.Color;

        if (token is LootToken)
            BuildCoinVisual(go, mat, token.Color);
        else
            BuildCubeVisual(go, mat, token.Color);

        _tokens[coord] = (token, go);
    }

    private static void BuildCubeVisual(GameObject parent, Material mat, Color labelColor)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(parent.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
        DestroyCollider(cube);
        cube.GetComponent<MeshRenderer>().material = mat;
        cube.name = "TokenCube";

        // Small label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(parent.transform);
        labelGo.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "\u2588"; // block character
        tm.fontSize = 32;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = labelColor;
        tm.fontStyle = FontStyle.Bold;
    }

    private static void BuildCoinVisual(GameObject parent, Material mat, Color labelColor)
    {
        // Flat cylinder = coin shape
        var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.transform.SetParent(parent.transform);
        coin.transform.localPosition = Vector3.zero;
        coin.transform.localScale = new Vector3(0.35f, 0.025f, 0.35f);
        DestroyCollider(coin);
        coin.GetComponent<MeshRenderer>().material = mat;
        coin.name = "Coin";

        // Darker rim ring (slightly larger cylinder underneath)
        var rimMat = new Material(Shader.Find("Unlit/Color"));
        rimMat.color = new Color(0.75f, 0.55f, 0.1f);
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.transform.SetParent(parent.transform);
        rim.transform.localPosition = new Vector3(0f, -0.005f, 0f);
        rim.transform.localScale = new Vector3(0.40f, 0.025f, 0.40f);
        DestroyCollider(rim);
        rim.GetComponent<MeshRenderer>().material = rimMat;
        rim.name = "CoinRim";

        // "$" label on top
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(parent.transform);
        labelGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = "$";
        tm.fontSize = 48;
        tm.characterSize = 0.10f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.85f, 0.6f, 0.05f);
        tm.fontStyle = FontStyle.Bold;
    }

    private static void DestroyCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
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
