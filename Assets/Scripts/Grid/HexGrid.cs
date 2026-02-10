using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a rectangular hex grid using offset-coordinate iteration
/// stored as axial coordinates. Attach to an empty GameObject in the scene.
/// </summary>
public class HexGrid : MonoBehaviour
{
    [Header("Grid dimensions (offset-rectangle)")]
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 7;

    [Header("Hex settings")]
    [SerializeField] private float hexSize = 1f;

    private readonly Dictionary<HexCoord, HexTile> _tiles = new();

    public int Columns => columns;
    public int Rows => rows;
    public float HexSize => hexSize;
    public IReadOnlyDictionary<HexCoord, HexTile> Tiles => _tiles;

    private void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                // Convert offset (col, row) → axial (q, r)
                // Using even-q offset for flat-top hexes:
                //   q = col
                //   r = row - (col + (col & 1)) / 2
                int q = col;
                int r = row - (col + (col & 1)) / 2;

                var coord = new HexCoord(q, r);
                SpawnTile(coord);
            }
        }

        CenterCamera();
    }

    private void SpawnTile(HexCoord coord)
    {
        var go = new GameObject();
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        var tile = go.AddComponent<HexTile>();
        tile.Init(coord, hexSize);
        go.transform.SetParent(transform);
        _tiles[coord] = tile;
    }

    /// <summary>
    /// Tries to get the tile at the given axial coordinate.
    /// </summary>
    public bool TryGetTile(HexCoord coord, out HexTile tile)
    {
        return _tiles.TryGetValue(coord, out tile);
    }

    /// <summary>
    /// Moves the main camera so the grid is roughly centered in view.
    /// </summary>
    private void CenterCamera()
    {
        if (Camera.main == null) return;

        // Average position of the corner tiles
        var min = new HexCoord(0, 0).ToWorldPosition(hexSize);
        int lastCol = columns - 1;
        int lastRow = rows - 1;
        int qMax = lastCol;
        int rMax = lastRow - (lastCol + (lastCol & 1)) / 2;
        var max = new HexCoord(qMax, rMax).ToWorldPosition(hexSize);

        var center = (min + max) / 2f;
        Camera.main.transform.position = new Vector3(center.x, center.y, -10f);
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = Mathf.Max(max.y - min.y, max.x - min.x) / 2f + 2f;
    }
}
