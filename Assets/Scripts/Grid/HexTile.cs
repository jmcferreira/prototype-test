using UnityEngine;

/// <summary>
/// A single hex tile on the grid. Generates its own flat-top hex mesh at Start.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexTile : MonoBehaviour
{
    public HexCoord Coord { get; private set; }

    private static Mesh _sharedHexMesh;

    public void Init(HexCoord coord, float hexSize)
    {
        Coord = coord;
        transform.position = coord.ToWorldPosition(hexSize);
        gameObject.name = $"Hex {coord}";
    }

    private void Awake()
    {
        if (_sharedHexMesh == null)
            _sharedHexMesh = CreateFlatTopHexMesh(1f);

        GetComponent<MeshFilter>().sharedMesh = _sharedHexMesh;

        // Default material — plain white, unlit so it doesn't need lighting
        var renderer = GetComponent<MeshRenderer>();
        if (renderer.sharedMaterial == null)
        {
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.85f, 0.9f, 0.85f);
            renderer.sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Creates a flat-top hexagon mesh with the given outer radius.
    /// Vertices go clockwise starting from the right vertex.
    /// </summary>
    private static Mesh CreateFlatTopHexMesh(float outerRadius)
    {
        // Shrink slightly so there's a visible gap between tiles
        float r = outerRadius * 0.95f;
        var verts = new Vector3[7]; // center + 6 corners
        var tris = new int[18];     // 6 triangles × 3 indices

        verts[0] = Vector3.zero;

        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i;
            float angleRad = Mathf.Deg2Rad * angleDeg;
            verts[i + 1] = new Vector3(r * Mathf.Cos(angleRad), r * Mathf.Sin(angleRad), 0f);
        }

        for (int i = 0; i < 6; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i < 5) ? i + 2 : 1;
        }

        var mesh = new Mesh
        {
            name = "Hex",
            vertices = verts,
            triangles = tris,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
