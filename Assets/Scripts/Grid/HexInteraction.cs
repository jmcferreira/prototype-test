using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse hover highlighting and click selection on the hex grid.
/// Shows a floating tooltip over tokens (e.g. loot) when hovered.
/// Attach to any GameObject in the scene.
/// </summary>
public class HexInteraction : MonoBehaviour
{
    private HexGrid _grid;
    private Camera _cam;
    private HexTile _hoveredTile;
    private HexTile _selectedTile;

    // Token hover tooltip (world-space TextMesh)
    private GameObject _tokenTooltipGo;
    private TextMesh _tokenTooltipText;
    private HexCoord _tooltipCoord;
    private bool _tooltipVisible;

    public HexTile SelectedTile => _selectedTile;

    /// <summary>
    /// Event fired when a tile is clicked. Listeners can consume the selection.
    /// </summary>
    public event System.Action<HexTile> OnTileClicked;

    private void Start()
    {
        _grid = FindObjectOfType<HexGrid>();
        _cam = Camera.main;
        BuildTokenTooltip();
    }

    private void Update()
    {
        // Ignore when mouse is over UI elements (card buttons, etc.)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        UpdateHover();

        if (Input.GetMouseButtonDown(0))
            UpdateSelection();
    }

    private void UpdateHover()
    {
        HexTile newHover = GetTileUnderMouse();

        if (newHover == _hoveredTile) return;

        // Unhover previous
        _hoveredTile?.SetHovered(false);

        _hoveredTile = newHover;

        // Hover new
        _hoveredTile?.SetHovered(true);

        // Update token tooltip
        UpdateTokenTooltip(newHover);
    }

    private void UpdateSelection()
    {
        HexTile clicked = GetTileUnderMouse();
        if (clicked == null) return;

        // Deselect previous
        if (_selectedTile != null && _selectedTile != clicked)
            _selectedTile.SetSelected(false);

        // Toggle selection if clicking same tile, otherwise select new
        if (_selectedTile == clicked)
        {
            _selectedTile.SetSelected(false);
            _selectedTile = null;
        }
        else
        {
            _selectedTile = clicked;
            _selectedTile.SetSelected(true);
        }

        OnTileClicked?.Invoke(clicked);
    }

    /// <summary>
    /// Clear the current selection (called after action resolution).
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedTile != null)
        {
            _selectedTile.SetSelected(false);
            _selectedTile = null;
        }
    }

    public HexTile GetTileUnderMouse()
    {
        if (_cam == null || _grid == null) return null;

        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float enter)) return null;

        Vector3 worldPoint = ray.GetPoint(enter);
        HexCoord coord = HexCoord.FromWorldPosition(worldPoint);

        _grid.TryGetTile(coord, out HexTile tile);
        return tile;
    }

    // ── Token tooltip ──────────────────────────────────────────────────

    private void BuildTokenTooltip()
    {
        _tokenTooltipGo = new GameObject("TokenTooltip");

        // Background bar (dark, slightly transparent)
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(_tokenTooltipGo.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bg.transform.localScale = new Vector3(1.2f, 0.35f, 1f);
        var bgCol = bg.GetComponent<Collider>();
        if (bgCol != null) Object.Destroy(bgCol);
        var bgMat = new Material(Shader.Find("Unlit/Color"));
        bgMat.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        bg.GetComponent<MeshRenderer>().material = bgMat;
        bg.name = "TooltipBG";

        // Text label
        var textGo = new GameObject("TooltipText");
        textGo.transform.SetParent(_tokenTooltipGo.transform);
        textGo.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        textGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _tokenTooltipText = textGo.AddComponent<TextMesh>();
        _tokenTooltipText.fontSize = 48;
        _tokenTooltipText.characterSize = 0.06f;
        _tokenTooltipText.anchor = TextAnchor.MiddleCenter;
        _tokenTooltipText.alignment = TextAlignment.Center;
        _tokenTooltipText.color = new Color(1f, 0.85f, 0.3f);
        _tokenTooltipText.fontStyle = FontStyle.Bold;

        _tokenTooltipGo.SetActive(false);
        _tooltipVisible = false;
    }

    private void UpdateTokenTooltip(HexTile tile)
    {
        if (TokenManager.Instance == null || _tokenTooltipGo == null)
        {
            HideTokenTooltip();
            return;
        }

        if (tile == null)
        {
            HideTokenTooltip();
            return;
        }

        var token = TokenManager.Instance.GetToken(tile.Coord);
        if (token == null)
        {
            HideTokenTooltip();
            return;
        }

        // Position above the token
        Vector3 tilePos = tile.transform.position;
        _tokenTooltipGo.transform.position = tilePos + new Vector3(0f, 0.15f, -0.55f);
        _tokenTooltipText.text = token.Description;
        _tokenTooltipText.color = token.Color;

        if (!_tooltipVisible)
        {
            _tokenTooltipGo.SetActive(true);
            _tooltipVisible = true;
        }

        _tooltipCoord = tile.Coord;
    }

    private void HideTokenTooltip()
    {
        if (_tooltipVisible)
        {
            _tokenTooltipGo.SetActive(false);
            _tooltipVisible = false;
        }
    }
}
