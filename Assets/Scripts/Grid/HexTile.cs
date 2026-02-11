using UnityEngine;

/// <summary>
/// A single hex tile on the grid. Generates its own flat-top hex mesh at Start.
/// Supports highlight (hover) and selection color states.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexTile : MonoBehaviour
{
    private static readonly Color DefaultColor      = new Color(0.85f, 0.9f, 0.85f);
    private static readonly Color HoverColor        = new Color(0.95f, 1f, 0.8f);
    private static readonly Color SelectedColor     = new Color(0.4f, 0.8f, 1f);
    private static readonly Color TargetHoverColor  = new Color(0.2f, 0.65f, 0.95f);

    public HexCoord Coord { get; private set; }

    private static Mesh _sharedHexMesh;
    private Material _mat;
    private bool _isSelected;
    private bool _isTargetHovered;
    private bool _hasIntent;
    private Color _intentColor;

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

        _mat = new Material(Shader.Find("Unlit/Color"));
        _mat.color = DefaultColor;
        GetComponent<MeshRenderer>().material = _mat;
    }

    public void SetHovered(bool hovered)
    {
        if (_isSelected) return;
        if (_hasIntent && !hovered) { _mat.color = _intentColor; return; }
        _mat.color = hovered ? HoverColor : DefaultColor;
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        _isTargetHovered = false;
        _mat.color = selected ? SelectedColor : DefaultColor;
    }

    /// <summary>
    /// Hover effect for valid-target tiles (brighter than selected).
    /// Only works when the tile is already selected (highlighted as a target).
    /// </summary>
    public void SetTargetHovered(bool hovered)
    {
        if (!_isSelected) return;
        _isTargetHovered = hovered;
        _mat.color = hovered ? TargetHoverColor : SelectedColor;
    }

    /// <summary>
    /// Show enemy intent highlight. Uses a separate color layer that doesn't
    /// interfere with selection/hover. Clear with SetIntentHighlight(false, ...).
    /// </summary>
    public void SetIntentHighlight(bool show, Color color = default)
    {
        _hasIntent = show;
        _intentColor = color;
        if (show && !_isSelected)
            _mat.color = color;
        else if (!show && !_isSelected)
            _mat.color = DefaultColor;
    }

    /// <summary>
    /// Creates a flat-top hexagon mesh with the given outer radius.
    /// </summary>
    private static Mesh CreateFlatTopHexMesh(float outerRadius)
    {
        float r = outerRadius * 0.95f;
        var verts = new Vector3[7];
        var tris = new int[18];

        verts[0] = Vector3.zero;

        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i;
            float angleRad = Mathf.Deg2Rad * angleDeg;
            verts[i + 1] = new Vector3(r * Mathf.Cos(angleRad), 0f, r * Mathf.Sin(angleRad));
        }

        for (int i = 0; i < 6; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = (i < 5) ? i + 2 : 1;
            tris[i * 3 + 2] = i + 1;
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
