using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrollable battle log panel positioned below the player info panel.
/// Subscribes to BattleLog events and appends styled text entries.
/// Content height is resized in LateUpdate to avoid first-frame layout issues.
/// </summary>
public class BattleLogUI : MonoBehaviour
{
    private Text _logText;
    private ScrollRect _scrollRect;
    private RectTransform _contentRect;
    private bool _dirty;

    public void Init()
    {
        BuildUI();
        BattleLog.OnEntryAdded += OnEntry;

        // Render any entries that were added before we subscribed
        foreach (var entry in BattleLog.Entries)
            AppendEntry(entry);
    }

    private void OnDestroy()
    {
        BattleLog.OnEntryAdded -= OnEntry;
    }

    private void BuildUI()
    {
        // Root panel: anchored below the player panel on the left.
        // Player panel: anchor (0, 0.5), pivot (0, 0.5), y=80, height=310.
        // Player panel bottom edge at y = 80 - 155 = -75.
        var rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0.5f);
        rootRect.anchorMax = new Vector2(0f, 0.5f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(12f, -81f);
        rootRect.sizeDelta = new Vector2(286f, 234f);

        // Dark background
        var bgImg = gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.07f, 0.07f, 0.1f, 0.9f);
        bgImg.raycastTarget = true;

        // Header label
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(transform, false);
        var headerText = headerGo.AddComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 12;
        headerText.fontStyle = FontStyle.Bold;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(0.7f, 0.7f, 0.8f);
        headerText.text = "Battle Log";
        headerText.raycastTarget = false;
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 20f);

        // Divider under header
        var divGo = new GameObject("Divider");
        divGo.transform.SetParent(transform, false);
        var divImg = divGo.AddComponent<Image>();
        divImg.color = new Color(0.3f, 0.3f, 0.4f, 0.6f);
        divImg.raycastTarget = false;
        var divRect = divGo.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0f, 1f);
        divRect.anchorMax = new Vector2(1f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.anchoredPosition = new Vector2(0f, -20f);
        divRect.sizeDelta = new Vector2(-8f, 1f);

        // Viewport (masked scroll area below header)
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(transform, false);
        var vpImg = viewportGo.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRect = viewportGo.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = new Vector2(4f, 4f);
        vpRect.offsetMax = new Vector2(-4f, -22f);

        // Content rect: stretches full width, height grown manually
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        _contentRect = contentGo.AddComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(1f, 1f);
        _contentRect.pivot = new Vector2(0f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;
        _contentRect.sizeDelta = new Vector2(0f, 0f);

        // Text lives on the content GO — renders all log entries
        _logText = contentGo.AddComponent<Text>();
        _logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _logText.fontSize = 12;
        _logText.alignment = TextAnchor.UpperLeft;
        _logText.color = new Color(0.85f, 0.85f, 0.85f);
        _logText.raycastTarget = false;
        _logText.supportRichText = true;
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _logText.verticalOverflow = VerticalWrapMode.Overflow;
        _logText.text = "";

        // ScrollRect on root panel
        _scrollRect = gameObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 20f;
        _scrollRect.viewport = vpRect;
        _scrollRect.content = _contentRect;
    }

    private void OnEntry(BattleLog.Entry entry)
    {
        AppendEntry(entry);
    }

    private void AppendEntry(BattleLog.Entry entry)
    {
        if (_logText == null) return;

        string line;
        switch (entry.Type)
        {
            case BattleLog.EntryType.TurnHeader:
                string sep = _logText.text.Length > 0 ? "\n" : "";
                line = $"{sep}<color=#8888cc><b>{entry.Text}</b></color>";
                break;
            case BattleLog.EntryType.CardName:
                line = $"<color=#ffcc44><b>  {entry.Text}</b></color>";
                break;
            default:
                line = $"  <color=#aaaaaa>{entry.Text}</color>";
                break;
        }

        _logText.text += (_logText.text.Length > 0 ? "\n" : "") + line;
        _dirty = true;
    }

    /// <summary>
    /// Resize content to fit text and auto-scroll.
    /// Done in LateUpdate so the canvas layout pass has already resolved
    /// all rect widths — Text.preferredHeight then returns the correct value.
    /// </summary>
    private void LateUpdate()
    {
        if (!_dirty) return;
        _dirty = false;

        // preferredHeight uses the Text's current rect width (set by anchors)
        float h = _logText.preferredHeight + 4f;
        _contentRect.sizeDelta = new Vector2(0f, h);

        // Scroll to bottom
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}
