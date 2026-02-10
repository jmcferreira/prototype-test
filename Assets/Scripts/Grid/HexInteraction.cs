using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse hover highlighting and click selection on the hex grid.
/// Attach to any GameObject in the scene.
/// </summary>
public class HexInteraction : MonoBehaviour
{
    private HexGrid _grid;
    private Camera _cam;
    private HexTile _hoveredTile;
    private HexTile _selectedTile;

    public HexTile SelectedTile => _selectedTile;

    /// <summary>
    /// Event fired when a tile is clicked. Listeners can consume the selection.
    /// </summary>
    public event System.Action<HexTile> OnTileClicked;

    private void Start()
    {
        _grid = FindObjectOfType<HexGrid>();
        _cam = Camera.main;
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
}
