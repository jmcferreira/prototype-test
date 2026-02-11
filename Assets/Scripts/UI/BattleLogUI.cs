using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrollable battle log panel positioned below the player info panel.
/// Subscribes to BattleLog events and appends styled text entries.
/// </summary>
public class BattleLogUI : MonoBehaviour
{
    private Text _logText;
    private ScrollRect _scrollRect;
    private RectTransform _contentRect;

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
        // Player panel bottom edge is at y = 80 - 155 = -75 from center-left.
        var rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0.5f);
        rootRect.anchorMax = new Vector2(0f, 0.5f);
        rootRect.pivot = new Vector2(0f, 1f); // top-left pivot to hang below
        rootRect.anchoredPosition = new Vector2(12f, -81f); // 6px gap below player panel bottom
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

        // Scroll view area (below header)
        var scrollGo = new GameObject("ScrollArea");
        scrollGo.transform.SetParent(transform, false);
        _scrollRect = scrollGo.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 20f;
        var scrollImg = scrollGo.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = true;
        var scrollRectTr = scrollGo.GetComponent<RectTransform>();
        scrollRectTr.anchorMin = Vector2.zero;
        scrollRectTr.anchorMax = Vector2.one;
        scrollRectTr.offsetMin = new Vector2(4f, 4f);
        scrollRectTr.offsetMax = new Vector2(-4f, -22f);

        // Mask for clipping
        scrollGo.AddComponent<Mask>().showMaskGraphic = false;

        // Content container (grows vertically as text is added)
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(scrollGo.transform, false);
        _contentRect = contentGo.AddComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(1f, 1f);
        _contentRect.pivot = new Vector2(0.5f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;
        _contentRect.sizeDelta = new Vector2(0f, 0f);

        var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vl = contentGo.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 1;
        vl.childAlignment = TextAnchor.UpperLeft;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.padding = new RectOffset(4, 4, 2, 2);

        _scrollRect.content = _contentRect;

        // Single Text component for all log entries (rich text for styling)
        var textGo = new GameObject("LogText");
        textGo.transform.SetParent(contentGo.transform, false);
        _logText = textGo.AddComponent<Text>();
        _logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _logText.fontSize = 12;
        _logText.alignment = TextAnchor.UpperLeft;
        _logText.color = new Color(0.8f, 0.8f, 0.8f);
        _logText.raycastTarget = false;
        _logText.supportRichText = true;
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _logText.verticalOverflow = VerticalWrapMode.Overflow;
        _logText.text = "";
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
                // Blank line before turn headers (except first)
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

        // Force layout rebuild so ContentSizeFitter recalculates, then scroll
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}
