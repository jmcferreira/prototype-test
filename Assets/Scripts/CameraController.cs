using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Top-down camera controls:
///   Scroll wheel  — zoom in / out
///   Middle-mouse drag or Arrow keys — pan
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float panSpeed = 10f;

    private Camera _cam;
    private Vector3 _dragOrigin;
    private bool _dragging;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        if (_cam == null) return;

        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f)) return;
        // Don't zoom when pointer is over UI (card panel)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        float newSize = _cam.orthographicSize - scroll * zoomSpeed;
        _cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
    }

    private void HandlePan()
    {
        // Arrow / WASD keys
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            // Scale pan speed with zoom so it feels consistent
            float speed = panSpeed * (_cam.orthographicSize / 8f) * Time.deltaTime;
            _cam.transform.position += new Vector3(h * speed, 0f, v * speed);
        }

        // Middle-mouse drag
        if (Input.GetMouseButtonDown(2))
        {
            _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);
            _dragging = true;
        }
        if (Input.GetMouseButtonUp(2))
        {
            _dragging = false;
        }
        if (_dragging)
        {
            Vector3 current = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = _dragOrigin - current;
            _cam.transform.position += new Vector3(delta.x, 0f, delta.z);
        }
    }
}
