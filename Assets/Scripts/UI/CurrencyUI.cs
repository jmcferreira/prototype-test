using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small panel at the top-left of the screen displaying Gold and XP.
/// Subscribes to CurrencyManager.OnChanged for live updates.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    private Text _goldText;
    private Text _xpText;

    public void Init()
    {
        BuildUI();
        CurrencyManager.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        CurrencyManager.OnChanged -= Refresh;
    }

    private void BuildUI()
    {
        // Root panel at top-left
        var rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(12f, -10f);
        rootRect.sizeDelta = new Vector2(160f, 50f);

        // Dark background
        var bgImg = gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.07f, 0.07f, 0.1f, 0.85f);
        bgImg.raycastTarget = false;

        // Gold row
        var goldGo = new GameObject("GoldText");
        goldGo.transform.SetParent(transform, false);
        _goldText = goldGo.AddComponent<Text>();
        _goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _goldText.fontSize = 14;
        _goldText.fontStyle = FontStyle.Bold;
        _goldText.alignment = TextAnchor.MiddleLeft;
        _goldText.color = new Color(1f, 0.82f, 0.25f); // gold
        _goldText.raycastTarget = false;
        var goldRect = goldGo.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0f, 0.5f);
        goldRect.anchorMax = new Vector2(1f, 1f);
        goldRect.offsetMin = new Vector2(10f, 2f);
        goldRect.offsetMax = new Vector2(-6f, -4f);

        // XP row
        var xpGo = new GameObject("XPText");
        xpGo.transform.SetParent(transform, false);
        _xpText = xpGo.AddComponent<Text>();
        _xpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _xpText.fontSize = 14;
        _xpText.fontStyle = FontStyle.Bold;
        _xpText.alignment = TextAnchor.MiddleLeft;
        _xpText.color = new Color(0.45f, 0.75f, 1f); // blue
        _xpText.raycastTarget = false;
        var xpRect = xpGo.GetComponent<RectTransform>();
        xpRect.anchorMin = new Vector2(0f, 0f);
        xpRect.anchorMax = new Vector2(1f, 0.5f);
        xpRect.offsetMin = new Vector2(10f, 4f);
        xpRect.offsetMax = new Vector2(-6f, -2f);
    }

    private void Refresh()
    {
        if (_goldText != null)
            _goldText.text = $"\u2B24 Gold: {CurrencyManager.Gold}";
        if (_xpText != null)
            _xpText.text = $"\u2605 XP: {CurrencyManager.XP}";
    }
}
