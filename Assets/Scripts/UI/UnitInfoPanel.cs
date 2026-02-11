using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Screen-space side panel showing a unit's portrait, name, HP, and status effects.
/// Player panel anchors to the left, enemy panels stack on the right.
/// Border colour brightens when it is that unit's turn.
/// Status effects show as "Icon Name X" with hover tooltips.
/// Enemy panels show intent card popup on hover (panel or chip).
/// Panels expand 30% on hover for readability (IPointerEnter/Exit).
/// </summary>
public class UnitInfoPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Static registry so external systems (PlayerUnit chip hover) can find panels by unit
    private static readonly Dictionary<Unit, UnitInfoPanel> _registry = new();
    public static UnitInfoPanel GetPanel(Unit unit) =>
        _registry.TryGetValue(unit, out var p) ? p : null;

    private Unit _unit;
    private TurnManager _turnManager;
    private bool _isLeft;

    private Image _borderImage;
    private Text _nameText;
    private Text _hpText;
    private readonly Dictionary<StatusEffectType, Text> _statusTexts = new();
    private GameObject _deceasedOverlay;

    // Tooltip
    private GameObject _tooltipGo;
    private Text _tooltipText;

    // Intent card popup (enemy panels only)
    private GameObject _intentGo;
    private Text _intentCardName;
    private Text _intentActions;
    private bool _intentVisible;
    private bool _panelHovered;

    private Color _borderActiveColor;
    private Color _borderInactiveColor;
    private static readonly Color DeceasedBorderColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);

    public void Init(Unit unit, TurnManager turnManager, bool isLeft, int stackIndex = 0)
    {
        _unit = unit;
        _turnManager = turnManager;
        _isLeft = isLeft;

        Color teamColor = unit.Team == Team.Player
            ? new Color(0.3f, 0.55f, 1f)
            : new Color(1f, 0.3f, 0.3f);
        _borderActiveColor = teamColor;
        _borderInactiveColor = new Color(teamColor.r * 0.25f, teamColor.g * 0.25f, teamColor.b * 0.25f, 0.85f);

        BuildPanel(isLeft, teamColor, stackIndex);
        Refresh();

        _unit.OnChanged += Refresh;
        _registry[unit] = this;
    }

    private void OnDestroy()
    {
        if (_unit != null)
        {
            _unit.OnChanged -= Refresh;
            _registry.Remove(_unit);
        }
    }

    // ── Hover scale (IPointerEnterHandler / IPointerExitHandler) ────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.3f;

        // For enemy panels, also show intent
        if (!_isLeft && _unit != null && _unit.IsAlive && _unit.Team == Team.Enemy)
        {
            _panelHovered = true;
            ComputeAndShowIntent();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;

        if (!_isLeft)
        {
            _panelHovered = false;
            HideIntent();
        }
    }

    /// <summary>
    /// Show intent card popup next to this panel. Called by PlayerUnit chip hover
    /// or by panel hover EventTrigger.
    /// </summary>
    public void ShowIntent(CardData cardData)
    {
        if (_intentGo == null || cardData == null) return;
        _intentCardName.text = cardData.cardName;

        // Build action list, one per line
        var lines = new List<string>();
        if (cardData.actions != null)
        {
            foreach (var action in cardData.actions)
                lines.Add(Hand.DescribeAction(action));
        }
        _intentActions.text = string.Join("\n", lines);
        _intentGo.SetActive(true);
        _intentVisible = true;
    }

    /// <summary>
    /// Hide the intent card popup.
    /// </summary>
    public void HideIntent()
    {
        if (_intentGo != null && _intentVisible)
        {
            _intentGo.SetActive(false);
            _intentVisible = false;
        }
    }

    // ── Build ──────────────────────────────────────────────────────────

    private void BuildPanel(bool isLeft, Color teamColor, int stackIndex)
    {
        var rect = gameObject.AddComponent<RectTransform>();

        bool hasPassive = _unit.Passive != null;
        if (isLeft)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(12f, 80f);
            rect.sizeDelta = new Vector2(180f, hasPassive ? 310f : 260f);
        }
        else
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            // Stack multiple enemy panels vertically (with gap between panels)
            float y = -100f + stackIndex * 245f;
            rect.anchoredPosition = new Vector2(-12f, y);
            rect.sizeDelta = new Vector2(170f, hasPassive ? 230f : 190f);
        }

        // Coloured border (bright = active turn, dim = inactive)
        _borderImage = gameObject.AddComponent<Image>();
        _borderImage.color = _borderInactiveColor;
        // Raycast target on all panels for hover detection
        _borderImage.raycastTarget = true;

        // Inner dark background
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.13f, 0.95f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        // Vertical layout
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 3;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Avatar
        int avatarSize = isLeft ? 100 : 60;
        BuildAvatar(innerGo.transform, teamColor, avatarSize);

        // Name
        _nameText = CreateText(innerGo.transform, "Name", isLeft ? 18 : 15, FontStyle.Bold, Color.white, isLeft ? 26 : 22);

        // HP
        _hpText = CreateText(innerGo.transform, "HP", isLeft ? 24 : 18, FontStyle.Normal, new Color(0.95f, 0.25f, 0.25f), isLeft ? 30 : 24);

        // Status list (vertical, one row per active status)
        BuildStatusList(innerGo.transform, isLeft);

        // Passive skill description
        if (_unit.Passive != null)
            BuildPassiveSection(innerGo.transform, _unit.Passive, isLeft);

        // Deceased overlay (hidden until unit dies)
        BuildDeceasedOverlay();

        // Tooltip (hidden by default)
        BuildTooltip();

        // Intent card popup (enemy panels only)
        if (!isLeft)
            BuildIntentPopup();
    }

    private void BuildAvatar(Transform parent, Color teamColor, int size)
    {
        var go = new GameObject("Avatar");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = CreateGradientSprite(teamColor);
        img.color = Color.white;
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;

        // Large initial letter
        var letterGo = new GameObject("Letter");
        letterGo.transform.SetParent(go.transform, false);
        var txt = letterGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = size > 60 ? 36 : 24;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 1f, 1f, 0.85f);
        txt.text = _unit.DisplayName.Length > 0
            ? _unit.DisplayName.Substring(0, 1).ToUpper()
            : "?";
        txt.raycastTarget = false;
        var r = letterGo.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private void BuildStatusList(Transform parent, bool isLeft)
    {
        var containerGo = new GameObject("StatusList");
        containerGo.transform.SetParent(parent, false);
        var vl = containerGo.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 1;
        vl.childAlignment = TextAnchor.UpperLeft;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        var containerLe = containerGo.AddComponent<LayoutElement>();
        containerLe.flexibleHeight = 1;

        int fontSize = isLeft ? 13 : 11;
        float rowHeight = isLeft ? 18f : 15f;

        foreach (var kvp in StatusEffectDefs.All)
        {
            var type = kvp.Key;
            var def = kvp.Value;

            var rowGo = new GameObject($"Status_{def.Name}");
            rowGo.transform.SetParent(containerGo.transform, false);

            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = rowHeight;

            // Text: "Icon Name X" — also serves as raycast target for hover
            // (Can't put Image + Text on same GO — both are Graphic-derived)
            var stText = rowGo.AddComponent<Text>();
            stText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stText.fontSize = fontSize;
            stText.alignment = TextAnchor.MiddleLeft;
            stText.color = def.IconColor;
            stText.raycastTarget = true;

            // Hover events for tooltip
            var trigger = rowGo.AddComponent<EventTrigger>();
            string desc = def.Description ?? def.Name;
            AddPointerEvent(trigger, EventTriggerType.PointerEnter, desc);
            AddPointerEvent(trigger, EventTriggerType.PointerExit, null);

            rowGo.SetActive(false);
            _statusTexts[type] = stText;
        }
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, string tooltipDesc)
    {
        var entry = new EventTrigger.Entry { eventID = eventType };
        if (eventType == EventTriggerType.PointerEnter)
        {
            entry.callback.AddListener((_) => ShowTooltip(tooltipDesc));
        }
        else
        {
            entry.callback.AddListener((_) => HideTooltip());
        }
        trigger.triggers.Add(entry);
    }

    private void BuildTooltip()
    {
        _tooltipGo = new GameObject("Tooltip");
        _tooltipGo.transform.SetParent(transform, false);

        // Dark background (Image also ensures RectTransform exists)
        var bgImg = _tooltipGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        bgImg.raycastTarget = false;

        var tooltipRect = _tooltipGo.GetComponent<RectTransform>();
        // Position to the right of the panel for left panels, left for right panels
        if (_isLeft)
        {
            tooltipRect.anchorMin = new Vector2(1f, 0.5f);
            tooltipRect.anchorMax = new Vector2(1f, 0.5f);
            tooltipRect.pivot = new Vector2(0f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(6f, 0f);
        }
        else
        {
            tooltipRect.anchorMin = new Vector2(0f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0f, 0.5f);
            tooltipRect.pivot = new Vector2(1f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(-6f, 0f);
        }
        tooltipRect.sizeDelta = new Vector2(200f, 50f);

        // Text
        var textGo = new GameObject("TooltipText");
        textGo.transform.SetParent(_tooltipGo.transform, false);
        _tooltipText = textGo.AddComponent<Text>();
        _tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _tooltipText.fontSize = 11;
        _tooltipText.fontStyle = FontStyle.Italic;
        _tooltipText.alignment = TextAnchor.MiddleCenter;
        _tooltipText.color = new Color(0.85f, 0.85f, 0.85f);
        _tooltipText.raycastTarget = false;
        _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;
        var txtRect = textGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(6f, 4f);
        txtRect.offsetMax = new Vector2(-6f, -4f);

        _tooltipGo.SetActive(false);
    }

    private void ShowTooltip(string description)
    {
        if (_tooltipGo == null || string.IsNullOrEmpty(description)) return;
        _tooltipText.text = description;
        _tooltipGo.SetActive(true);
    }

    private void HideTooltip()
    {
        if (_tooltipGo != null)
            _tooltipGo.SetActive(false);
    }

    // ── Intent card popup ───────────────────────────────────────────────

    private void BuildIntentPopup()
    {
        _intentGo = new GameObject("IntentPopup");
        _intentGo.transform.SetParent(transform, false);

        // Dark background — raycastTarget=true so hover propagates to panel
        var bgImg = _intentGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.06f, 0.12f, 0.95f);
        bgImg.raycastTarget = true;

        var intentRect = _intentGo.GetComponent<RectTransform>();
        // Stretch to match full panel height, positioned to its left
        intentRect.anchorMin = new Vector2(0f, 0f);
        intentRect.anchorMax = new Vector2(0f, 1f);
        intentRect.pivot = new Vector2(1f, 0.5f);
        intentRect.anchoredPosition = new Vector2(-6f, 0f);
        intentRect.sizeDelta = new Vector2(170f, 0f);

        // Vertical layout inside
        var layoutGo = new GameObject("IntentLayout");
        layoutGo.transform.SetParent(_intentGo.transform, false);
        var layoutRect = layoutGo.AddComponent<RectTransform>();
        layoutRect.anchorMin = Vector2.zero;
        layoutRect.anchorMax = Vector2.one;
        layoutRect.offsetMin = new Vector2(8f, 4f);
        layoutRect.offsetMax = new Vector2(-8f, -4f);
        var vl = layoutGo.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 4;
        vl.childAlignment = TextAnchor.UpperLeft;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // "Next:" label
        var headerGo = new GameObject("IntentHeader");
        headerGo.transform.SetParent(layoutGo.transform, false);
        var headerText = headerGo.AddComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 10;
        headerText.fontStyle = FontStyle.Normal;
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.color = new Color(0.6f, 0.6f, 0.7f);
        headerText.text = "Next:";
        headerText.raycastTarget = false;
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 14;

        // Card name (bold, orange-red)
        var nameGo = new GameObject("IntentCardName");
        nameGo.transform.SetParent(layoutGo.transform, false);
        _intentCardName = nameGo.AddComponent<Text>();
        _intentCardName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _intentCardName.fontSize = 13;
        _intentCardName.fontStyle = FontStyle.Bold;
        _intentCardName.alignment = TextAnchor.MiddleLeft;
        _intentCardName.color = new Color(1f, 0.6f, 0.3f);
        _intentCardName.raycastTarget = false;
        var nameLe = nameGo.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 18;

        // Action list (one line per action, grey)
        var actionsGo = new GameObject("IntentActions");
        actionsGo.transform.SetParent(layoutGo.transform, false);
        _intentActions = actionsGo.AddComponent<Text>();
        _intentActions.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _intentActions.fontSize = 11;
        _intentActions.fontStyle = FontStyle.Normal;
        _intentActions.alignment = TextAnchor.UpperLeft;
        _intentActions.color = new Color(0.8f, 0.8f, 0.8f);
        _intentActions.raycastTarget = false;
        _intentActions.verticalOverflow = VerticalWrapMode.Overflow;
        var actionsLe = actionsGo.AddComponent<LayoutElement>();
        actionsLe.flexibleHeight = 1;

        _intentGo.SetActive(false);
        _intentVisible = false;
    }

    private void ComputeAndShowIntent()
    {
        if (_unit is not EnemyUnit enemy) return;
        var allUnits = _turnManager?.GetAliveUnits();
        if (allUnits == null) return;

        enemy.ComputeIntent(allUnits, out CardData cardData);
        if (cardData != null)
            ShowIntent(cardData);
    }

    private void BuildPassiveSection(Transform parent, PassiveSkill passive, bool isLeft)
    {
        // Divider line
        var divGo = new GameObject("PassiveDivider");
        divGo.transform.SetParent(parent, false);
        var divImg = divGo.AddComponent<Image>();
        divImg.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        divImg.raycastTarget = false;
        var divLe = divGo.AddComponent<LayoutElement>();
        divLe.preferredHeight = 1;

        // Passive name (bold, gold)
        var nameText = CreateText(parent, "PassiveName", isLeft ? 13 : 11, FontStyle.Bold,
            new Color(1f, 0.85f, 0.4f), isLeft ? 18 : 16);
        nameText.text = $"\u2726 {passive.Name}"; // ✦ star decoration

        // Passive description (italic, grey)
        var descText = CreateText(parent, "PassiveDesc", isLeft ? 11 : 10, FontStyle.Italic,
            new Color(0.7f, 0.7f, 0.7f), isLeft ? 28 : 22);
        descText.text = passive.Description;
    }

    private void BuildDeceasedOverlay()
    {
        _deceasedOverlay = new GameObject("DeceasedOverlay");
        _deceasedOverlay.transform.SetParent(transform, false);

        // Semi-transparent dark overlay covering the whole panel
        var overlayImg = _deceasedOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);
        overlayImg.raycastTarget = false;
        var overlayRect = _deceasedOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // "Deceased" text centered on the overlay
        var txtGo = new GameObject("DeceasedText");
        txtGo.transform.SetParent(_deceasedOverlay.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.7f, 0.2f, 0.2f);
        txt.text = "Deceased";
        txt.raycastTarget = false;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        _deceasedOverlay.SetActive(false);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static Text CreateText(Transform parent, string name, int fontSize,
        FontStyle style, Color color, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        return txt;
    }

    private static Sprite CreateGradientSprite(Color baseColor)
    {
        const int size = 64;
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;

        Color dark = baseColor * 0.35f;
        dark.a = 1f;
        Color light = baseColor;
        light.a = 1f;

        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                // Slight radial vignette
                float dx = (x - size * 0.5f) / (size * 0.5f);
                float dy = (y - size * 0.5f) / (size * 0.5f);
                float vignette = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 0.25f);
                tex.SetPixel(x, y, Color.Lerp(dark, light, t * vignette));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    // ── Refresh ────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (_unit == null) return;

        _nameText.text = _unit.DisplayName;

        if (!_unit.IsAlive)
        {
            _hpText.text = "";
            if (_deceasedOverlay != null)
                _deceasedOverlay.SetActive(true);

            // Hide all status entries
            foreach (var kvp in _statusTexts)
                kvp.Value.gameObject.SetActive(false);
            HideTooltip();
            HideIntent();
            return;
        }

        string hearts = new string('\u2665', Mathf.Max(0, _unit.HP));
        _hpText.text = hearts;

        foreach (var kvp in _statusTexts)
        {
            int stacks = _unit.GetStatusStacks(kvp.Key);
            var def = StatusEffectDefs.Get(kvp.Key);
            kvp.Value.gameObject.SetActive(stacks > 0);
            if (stacks > 0)
                kvp.Value.text = $"{def.Icon} {def.Name} {stacks}";
        }
    }

    // ── Turn highlight ─────────────────────────────────────────────────

    private void Update()
    {
        if (_turnManager == null || _unit == null) return;
        if (!_unit.IsAlive)
        {
            _borderImage.color = DeceasedBorderColor;
            return;
        }
        bool active = _turnManager.CurrentUnit == _unit;
        _borderImage.color = active ? _borderActiveColor : _borderInactiveColor;
    }
}
