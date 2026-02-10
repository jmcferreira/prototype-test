using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Big banner at the top of the screen showing whose turn it is.
/// Shows "Player's Turn" (blue) or "Enemy Turn — {name}" (red).
/// Subscribes to TurnManager.OnTurnChanged.
/// </summary>
public class TurnBannerUI : MonoBehaviour
{
    private TurnManager _turnManager;
    private Image _bgImage;
    private Text _mainText;
    private Text _subText;

    private static readonly Color PlayerBg = new Color(0.12f, 0.22f, 0.55f, 0.88f);
    private static readonly Color EnemyBg  = new Color(0.55f, 0.12f, 0.12f, 0.88f);

    public void Init(TurnManager turnManager)
    {
        _turnManager = turnManager;
        _turnManager.OnTurnChanged += OnTurnChanged;

        BuildUI();

        // Show initial state
        if (_turnManager.CurrentUnit != null)
            OnTurnChanged(_turnManager.CurrentUnit);
    }

    private void OnDestroy()
    {
        if (_turnManager != null)
            _turnManager.OnTurnChanged -= OnTurnChanged;
    }

    private void BuildUI()
    {
        var rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f);
        rect.sizeDelta = new Vector2(400f, 70f);

        // Background
        _bgImage = gameObject.AddComponent<Image>();
        _bgImage.color = PlayerBg;
        _bgImage.raycastTarget = false;

        // Main text (large)
        var mainGo = new GameObject("MainText");
        mainGo.transform.SetParent(transform, false);
        _mainText = mainGo.AddComponent<Text>();
        _mainText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _mainText.fontSize = 30;
        _mainText.fontStyle = FontStyle.Bold;
        _mainText.alignment = TextAnchor.MiddleCenter;
        _mainText.color = Color.white;
        _mainText.raycastTarget = false;
        var mainRect = mainGo.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0f, 0.3f);
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = new Vector2(10f, 0f);
        mainRect.offsetMax = new Vector2(-10f, 0f);

        // Sub text (smaller, shows unit name for enemies)
        var subGo = new GameObject("SubText");
        subGo.transform.SetParent(transform, false);
        _subText = subGo.AddComponent<Text>();
        _subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _subText.fontSize = 16;
        _subText.alignment = TextAnchor.MiddleCenter;
        _subText.color = new Color(1f, 1f, 1f, 0.7f);
        _subText.raycastTarget = false;
        var subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = Vector2.zero;
        subRect.anchorMax = new Vector2(1f, 0.35f);
        subRect.offsetMin = new Vector2(10f, 0f);
        subRect.offsetMax = new Vector2(-10f, 0f);
    }

    private void OnTurnChanged(Unit unit)
    {
        if (unit == null) return;

        bool isPlayer = unit.Team == Team.Player;
        _bgImage.color = isPlayer ? PlayerBg : EnemyBg;
        _mainText.text = isPlayer ? "Player's Turn" : "Enemy Turn";
        _subText.text = isPlayer ? "" : unit.DisplayName;
    }
}
