using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrollable battle log panel positioned below the player info panel.
/// Uses FIXED content width (not stretch anchors) so Text.preferredHeight
/// returns correct values immediately, without waiting for canvas layout.
/// </summary>
public class BattleLogUI : MonoBehaviour
{
    private const float PanelWidth = 286f;
    private const float PanelHeight = 234f;
    private const float Padding = 4f;
    private const float HeaderHeight = 20f;
    private const float DividerHeight = 1f;
    // Content width = panel width minus padding on both sides
    private const float ContentWidth = PanelWidth - Padding * 2 - 4f; // 274

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
        rootRect.anchoredPosition = new Vector2(12f, -106f);
        rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

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
        headerRect.sizeDelta = new Vector2(0f, HeaderHeight);

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
        divRect.anchoredPosition = new Vector2(0f, -HeaderHeight);
        divRect.sizeDelta = new Vector2(-8f, DividerHeight);

        // Viewport (masked scroll area below header)
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(transform, false);
        var vpImg = viewportGo.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        var vpRect = viewportGo.GetComponent<RectTransform>();
        // Fixed size viewport — no stretch anchors
        vpRect.anchorMin = new Vector2(0f, 0f);
        vpRect.anchorMax = new Vector2(0f, 0f);
        vpRect.pivot = new Vector2(0f, 0f);
        vpRect.anchoredPosition = new Vector2(Padding, Padding);
        vpRect.sizeDelta = new Vector2(ContentWidth, PanelHeight - HeaderHeight - DividerHeight - Padding * 2);

        // Content rect: FIXED width, height grows with text
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        _contentRect = contentGo.AddComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(0f, 1f);
        _contentRect.pivot = new Vector2(0f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;
        _contentRect.sizeDelta = new Vector2(ContentWidth, 0f);

        // Text on a child GO — stretches to fill the fixed-width content rect
        var textGo = new GameObject("LogText");
        textGo.transform.SetParent(contentGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _logText = textGo.AddComponent<Text>();
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

    private void LateUpdate()
    {
        if (!_dirty) return;
        _dirty = false;

        // Calculate height using TextGenerator with explicit width.
        // This works even before canvas layout resolves because we use
        // a fixed ContentWidth rather than relying on rect resolution.
        var settings = _logText.GetGenerationSettings(new Vector2(ContentWidth, 0f));
        settings.scaleFactor = 1f; // GetGenerationSettings multiplies by canvas scale; reset to 1
        var gen = _logText.cachedTextGenerator;
        float h = gen.GetPreferredHeight(_logText.text, settings) + Padding;
        _contentRect.sizeDelta = new Vector2(ContentWidth, h);

        // Scroll to bottom
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}
