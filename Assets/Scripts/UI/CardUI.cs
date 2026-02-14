using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual card styled after the Sunderfolk reference: gradient art header,
/// card name overlay, dark action rows with coloured effect-label pills,
/// sub-description text, and a CD badge in the bottom-right corner.
/// </summary>
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnClicked;
    public event Action OnSkipClicked;

    private Image _borderImage;
    private Image _innerImage;
    private Button _button;
    private int _cooldownMax;

    // CD badge
    private Image _cdBadgeOuter;
    private Image _cdBadgeInner;
    private Text _cdBadgeText;

    // Cooldown overlay
    private GameObject _cdOverlayGo;

    // Per-action-row tracking for step highlights
    private readonly List<Image> _rowImages = new();
    private readonly List<Color> _rowBaseColors = new();
    private readonly List<Text> _stepIndicators = new();
    private readonly List<Image> _pillBgs = new();
    private readonly List<Color> _pillBaseColors = new();
    private readonly List<Text> _pillTexts = new();
    private readonly List<GameObject> _skipButtons = new();

    // ── Colours ──────────────────────────────────────────────────────────

    private static readonly Color BorderReady    = new(0.55f, 0.48f, 0.30f);
    private static readonly Color BorderHover    = new(0.75f, 0.65f, 0.40f);
    private static readonly Color BorderCooldown = new(0.28f, 0.28f, 0.28f);
    private static readonly Color BorderSelected = new(0.4f, 0.7f, 1f);

    private static readonly Color InnerReady    = new(0.15f, 0.17f, 0.13f);
    private static readonly Color InnerCooldown = new(0.13f, 0.13f, 0.13f);

    private static readonly Color DarkRow1 = new(0.12f, 0.13f, 0.11f);
    private static readonly Color DarkRow2 = new(0.14f, 0.15f, 0.12f);

    // Step highlight
    private static readonly Color RowActive = new(0.20f, 0.38f, 0.65f, 0.90f);
    private static readonly Color RowDone   = new(0.18f, 0.38f, 0.18f, 0.70f);

    // Effect-label pill colours
    private static readonly Color LabelMove    = new(0.30f, 0.58f, 0.22f);
    private static readonly Color LabelAttack  = new(0.72f, 0.22f, 0.18f);
    private static readonly Color LabelCC      = new(0.78f, 0.48f, 0.12f);
    private static readonly Color LabelSupport = new(0.22f, 0.48f, 0.68f);

    private static readonly Vector3 NormalScale = new(0.7f, 0.7f, 1f);
    private static readonly Vector3 HoverScale  = Vector3.one;

    private bool _selected;
    private bool _hovered;
    private bool _isReady;

    private static Font _font;
    private static Font Font => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ── Public API ──────────────────────────────────────────────────────

    public void Build(CardData data)
    {
        _cooldownMax = data.cooldown;
        transform.localScale = NormalScale;

        // Outer border
        _borderImage = gameObject.AddComponent<Image>();
        _borderImage.color = BorderReady;

        _button = gameObject.AddComponent<Button>();
        _button.targetGraphic = _borderImage;
        _button.onClick.AddListener(() => OnClicked?.Invoke());
        var bc = _button.colors;
        bc.normalColor = Color.white;
        bc.highlightedColor = Color.white;
        bc.pressedColor = new Color(0.9f, 0.9f, 0.9f);
        bc.disabledColor = Color.white;
        _button.colors = bc;

        // Inner dark panel
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(transform, false);
        _innerImage = innerGo.AddComponent<Image>();
        _innerImage.color = InnerReady;
        _innerImage.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(5f, 5f);
        innerRect.offsetMax = new Vector2(-5f, -5f);

        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // ── Image area + card name ──
        BuildImageArea(innerGo.transform, data);

        // ── Action rows ──
        int count = data.actions != null ? data.actions.Length : 0;
        for (int i = 0; i < count; i++)
        {
            BuildThinDivider(innerGo.transform);
            BuildActionRow(innerGo.transform, data.actions[i], i);
        }

        // ── Cooldown overlay (above inner, below badge) ──
        BuildCooldownOverlay();

        // ── CD badge (above everything) ──
        BuildCDBadge();
    }

    public void Refresh(int cooldownRemaining, int cooldownMax, bool isReady)
    {
        _isReady = isReady;

        if (isReady)
        {
            _cdBadgeText.text = _cooldownMax.ToString();
            _cdBadgeText.color = Color.white;
            _cdBadgeInner.gameObject.SetActive(true);
            _cdOverlayGo.SetActive(false);
        }
        else
        {
            _cdBadgeText.text = cooldownRemaining.ToString();
            _cdBadgeText.color = Color.black;
            _cdBadgeInner.gameObject.SetActive(false);
            _cdOverlayGo.SetActive(true);
        }

        _button.interactable = isReady;
        UpdateVisuals();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        UpdateVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        UpdateVisuals();
    }

    // ── Step highlighting ───────────────────────────────────────────────

    public void SetActiveStep(int step, bool showSkip = false)
    {
        for (int i = 0; i < _rowImages.Count; i++)
        {
            bool isActive = (i == step);
            bool isDone = (step >= 0 && i < step);

            if (isActive)
            {
                _rowImages[i].color = RowActive;
                _stepIndicators[i].text = "\u25B6"; // ▶
                _stepIndicators[i].color = Color.white;
            }
            else if (isDone)
            {
                _rowImages[i].color = RowDone;
                _stepIndicators[i].text = "\u2713"; // ✓
                _stepIndicators[i].color = new Color(0.6f, 0.9f, 0.6f);
            }
            else
            {
                _rowImages[i].color = _rowBaseColors[i];
                _stepIndicators[i].text = "";
                _stepIndicators[i].color = Color.clear;
            }

            if (i < _skipButtons.Count)
                _skipButtons[i].SetActive(isActive && showSkip);
        }
    }

    public void ClearActiveStep() => SetActiveStep(-1, false);

    // ── Visuals ─────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        if (_selected)
            _borderImage.color = BorderSelected;
        else if (_hovered && _isReady)
            _borderImage.color = BorderHover;
        else
            _borderImage.color = _isReady ? BorderReady : BorderCooldown;

        _innerImage.color = _isReady ? InnerReady : InnerCooldown;
        transform.localScale = (_hovered && _isReady) ? HoverScale : NormalScale;
    }

    // ── Builders ────────────────────────────────────────────────────────

    private void BuildImageArea(Transform parent, CardData data)
    {
        var go = new GameObject("ImageArea");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        Color gradCol = data.actions != null && data.actions.Length > 0
            ? GetEffectGradientColor(data.actions[0].effect)
            : new Color(0.35f, 0.40f, 0.35f);
        img.sprite = CreateGradientSprite(gradCol);
        img.color = Color.white;
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 148f;

        // Dark strip at bottom for card name
        var stripGo = new GameObject("NameStrip");
        stripGo.transform.SetParent(go.transform, false);
        var stripImg = stripGo.AddComponent<Image>();
        stripImg.color = new Color(0f, 0f, 0f, 0.6f);
        stripImg.raycastTarget = false;
        var sr = stripGo.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(1f, 0f);
        sr.pivot = new Vector2(0.5f, 0f);
        sr.anchoredPosition = Vector2.zero;
        sr.sizeDelta = new Vector2(0f, 46f);

        // Card name text
        var nameGo = new GameObject("CardName");
        nameGo.transform.SetParent(stripGo.transform, false);
        var nameTxt = nameGo.AddComponent<Text>();
        nameTxt.font = Font;
        nameTxt.fontSize = 24;
        nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.alignment = TextAnchor.MiddleLeft;
        nameTxt.color = Color.white;
        nameTxt.text = data.cardName.ToUpper();
        nameTxt.raycastTarget = false;
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = Vector2.zero;
        nr.anchorMax = Vector2.one;
        nr.offsetMin = new Vector2(10f, 0f);
        nr.offsetMax = new Vector2(-10f, 0f);
    }

    private void BuildActionRow(Transform parent, CardAction action, int index)
    {
        var (label, labelColor) = GetEffectLabel(action);
        string subText = GetActionSubText(action);
        bool hasSub = !string.IsNullOrEmpty(subText);

        float rowHeight = hasSub ? 72f : 44f;

        // Row container
        var rowGo = new GameObject($"Action_{index}");
        rowGo.transform.SetParent(parent, false);
        var rowImg = rowGo.AddComponent<Image>();
        Color baseColor = (index % 2 == 0) ? DarkRow1 : DarkRow2;
        rowImg.color = baseColor;
        rowImg.raycastTarget = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.preferredHeight = rowHeight;
        _rowImages.Add(rowImg);
        _rowBaseColors.Add(baseColor);

        // Step indicator (left edge, hidden by default)
        var stepGo = new GameObject("Step");
        stepGo.transform.SetParent(rowGo.transform, false);
        var stepTxt = stepGo.AddComponent<Text>();
        stepTxt.font = Font;
        stepTxt.fontSize = 14;
        stepTxt.fontStyle = FontStyle.Bold;
        stepTxt.alignment = TextAnchor.MiddleCenter;
        stepTxt.color = Color.clear;
        stepTxt.raycastTarget = false;
        var stepR = stepGo.GetComponent<RectTransform>();
        stepR.anchorMin = new Vector2(0f, hasSub ? 0.4f : 0f);
        stepR.anchorMax = new Vector2(0f, 1f);
        stepR.pivot = new Vector2(0f, 0.5f);
        stepR.anchoredPosition = new Vector2(4f, 0f);
        stepR.sizeDelta = new Vector2(18f, 0f);
        _stepIndicators.Add(stepTxt);

        // Coloured pill label
        var pillGo = new GameObject("Pill");
        pillGo.transform.SetParent(rowGo.transform, false);
        var pillImg = pillGo.AddComponent<Image>();
        pillImg.color = labelColor;
        pillImg.raycastTarget = false;
        var pr = pillGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0f, 1f);
        pr.anchorMax = new Vector2(0f, 1f);
        pr.pivot = new Vector2(0f, 1f);
        pr.anchoredPosition = new Vector2(22f, -7f);
        float pillW = Mathf.Max(80f, label.Length * 12f + 24f);
        pr.sizeDelta = new Vector2(pillW, 28f);
        _pillBgs.Add(pillImg);
        _pillBaseColors.Add(labelColor);

        // Pill text
        var ptGo = new GameObject("PillText");
        ptGo.transform.SetParent(pillGo.transform, false);
        var ptTxt = ptGo.AddComponent<Text>();
        ptTxt.font = Font;
        ptTxt.fontSize = 17;
        ptTxt.fontStyle = FontStyle.Bold;
        ptTxt.alignment = TextAnchor.MiddleLeft;
        ptTxt.color = Color.white;
        ptTxt.text = label;
        ptTxt.raycastTarget = false;
        var ptr = ptGo.GetComponent<RectTransform>();
        ptr.anchorMin = Vector2.zero;
        ptr.anchorMax = Vector2.one;
        ptr.offsetMin = new Vector2(8f, 0f);
        ptr.offsetMax = new Vector2(-4f, 0f);
        _pillTexts.Add(ptTxt);

        // Sub-description text (below pill)
        if (hasSub)
        {
            var subGo = new GameObject("SubText");
            subGo.transform.SetParent(rowGo.transform, false);
            var subTxt = subGo.AddComponent<Text>();
            subTxt.font = Font;
            subTxt.fontSize = 13;
            subTxt.fontStyle = FontStyle.Italic;
            subTxt.alignment = TextAnchor.UpperLeft;
            subTxt.color = new Color(0.65f, 0.65f, 0.58f);
            subTxt.text = subText;
            subTxt.raycastTarget = false;
            subTxt.supportRichText = true;
            subTxt.verticalOverflow = VerticalWrapMode.Overflow;
            var subR = subGo.GetComponent<RectTransform>();
            subR.anchorMin = new Vector2(0f, 0f);
            subR.anchorMax = new Vector2(1f, 0.42f);
            subR.offsetMin = new Vector2(22f, 2f);
            subR.offsetMax = new Vector2(-10f, 0f);
        }

        // Skip button (right edge, hidden by default)
        var skipGo = new GameObject($"Skip_{index}");
        skipGo.transform.SetParent(rowGo.transform, false);
        var skipImg = skipGo.AddComponent<Image>();
        skipImg.color = new Color(0.72f, 0.28f, 0.28f);
        var skR = skipGo.GetComponent<RectTransform>();
        skR.anchorMin = new Vector2(1f, 0.5f);
        skR.anchorMax = new Vector2(1f, 0.5f);
        skR.pivot = new Vector2(1f, 0.5f);
        skR.anchoredPosition = new Vector2(-8f, 0f);
        skR.sizeDelta = new Vector2(28f, 28f);

        var skipBtn = skipGo.AddComponent<Button>();
        skipBtn.targetGraphic = skipImg;
        skipBtn.onClick.AddListener(() => OnSkipClicked?.Invoke());
        var sc = skipBtn.colors;
        sc.normalColor = Color.white;
        sc.highlightedColor = new Color(1.2f, 1.05f, 1.05f);
        sc.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        skipBtn.colors = sc;

        var skipTxtGo = new GameObject("Icon");
        skipTxtGo.transform.SetParent(skipGo.transform, false);
        var skipTxt = skipTxtGo.AddComponent<Text>();
        skipTxt.font = Font;
        skipTxt.fontSize = 18;
        skipTxt.fontStyle = FontStyle.Bold;
        skipTxt.alignment = TextAnchor.MiddleCenter;
        skipTxt.color = Color.white;
        skipTxt.text = "\u2716"; // ✖
        skipTxt.raycastTarget = false;
        var stR = skipTxtGo.GetComponent<RectTransform>();
        stR.anchorMin = Vector2.zero;
        stR.anchorMax = Vector2.one;
        stR.offsetMin = Vector2.zero;
        stR.offsetMax = Vector2.zero;

        skipGo.SetActive(false);
        _skipButtons.Add(skipGo);
    }

    private void BuildCooldownOverlay()
    {
        _cdOverlayGo = new GameObject("CooldownOverlay");
        _cdOverlayGo.transform.SetParent(transform, false);
        var img = _cdOverlayGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.50f);
        img.raycastTarget = false;
        var r = _cdOverlayGo.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        _cdOverlayGo.SetActive(false);
    }

    private void BuildCDBadge()
    {
        var circleSprite = CreateCircleSprite(64);

        // Container anchored bottom-right of the card
        var badgeGo = new GameObject("CDBadge");
        badgeGo.transform.SetParent(transform, false);
        var badgeRect = badgeGo.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 0f);
        badgeRect.anchorMax = new Vector2(1f, 0f);
        badgeRect.pivot = new Vector2(1f, 0f);
        badgeRect.anchoredPosition = new Vector2(-12f, 12f);
        badgeRect.sizeDelta = new Vector2(44f, 44f);

        // Outer white circle
        _cdBadgeOuter = badgeGo.AddComponent<Image>();
        _cdBadgeOuter.sprite = circleSprite;
        _cdBadgeOuter.color = Color.white;
        _cdBadgeOuter.raycastTarget = false;

        // Inner dark circle (creates hollow effect)
        var innerGo = new GameObject("InnerCircle");
        innerGo.transform.SetParent(badgeGo.transform, false);
        _cdBadgeInner = innerGo.AddComponent<Image>();
        _cdBadgeInner.sprite = circleSprite;
        _cdBadgeInner.color = new Color(0.12f, 0.13f, 0.11f);
        _cdBadgeInner.raycastTarget = false;
        var ir = innerGo.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(4f, 4f);
        ir.offsetMax = new Vector2(-4f, -4f);

        // Number text
        var numGo = new GameObject("CDNum");
        numGo.transform.SetParent(badgeGo.transform, false);
        _cdBadgeText = numGo.AddComponent<Text>();
        _cdBadgeText.font = Font;
        _cdBadgeText.fontSize = 22;
        _cdBadgeText.fontStyle = FontStyle.Bold;
        _cdBadgeText.alignment = TextAnchor.MiddleCenter;
        _cdBadgeText.color = Color.white;
        _cdBadgeText.text = _cooldownMax.ToString();
        _cdBadgeText.raycastTarget = false;
        var nr = numGo.GetComponent<RectTransform>();
        nr.anchorMin = Vector2.zero;
        nr.anchorMax = Vector2.one;
        nr.offsetMin = Vector2.zero;
        nr.offsetMax = Vector2.zero;
    }

    private static void BuildThinDivider(Transform parent)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.35f, 0.32f, 0.25f, 0.5f);
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 1f;
    }

    // ── Effect helpers ──────────────────────────────────────────────────

    private static (string label, Color color) GetEffectLabel(CardAction action)
    {
        switch (action.effect)
        {
            case CardEffect.Move:        return ($"Move {action.range}", LabelMove);
            case CardEffect.Dash:        return ($"Dash {action.range}", LabelMove);
            case CardEffect.Jump:        return ($"Jump {action.range}", LabelMove);
            case CardEffect.Attack:      return ($"Attack {action.damage}", LabelAttack);
            case CardEffect.AttackLine:  return ($"Line {action.damage}", LabelAttack);
            case CardEffect.AttackAoE:   return ($"AoE {action.damage}", LabelAttack);
            case CardEffect.Push:        return ($"Push {action.pushDistance}", LabelCC);
            case CardEffect.Pull:        return ($"Pull {action.pushDistance}", LabelCC);
            case CardEffect.PushAoE:     return ($"Push All {action.pushDistance}", LabelCC);
            case CardEffect.Heal:        return ($"Heal {action.damage}", LabelSupport);
            case CardEffect.ReduceCooldown: return ("Haste -1 CD", LabelSupport);
            case CardEffect.PlaceWeb:    return ("Place Web", LabelSupport);
            case CardEffect.Status:
                var def = StatusEffectDefs.Get(action.statusEffect);
                Color col = new Color(
                    def.IconColor.r * 0.6f + 0.15f,
                    def.IconColor.g * 0.6f + 0.15f,
                    def.IconColor.b * 0.6f + 0.15f);
                return ($"{def.Name} {action.statusStacks}", col);
            default: return (action.effect.ToString(), Color.gray);
        }
    }

    private static string GetActionSubText(CardAction action)
    {
        var parts = new List<string>();

        bool isAttack = action.effect == CardEffect.Attack
            || action.effect == CardEffect.AttackLine
            || action.effect == CardEffect.AttackAoE;
        bool isPushPull = action.effect == CardEffect.Push
            || action.effect == CardEffect.Pull
            || action.effect == CardEffect.PushAoE;

        // Range for attacks
        if (isAttack)
            parts.Add($"Range {action.range}");

        // Status on attacks
        if (isAttack && action.statusStacks > 0)
        {
            var d = StatusEffectDefs.Get(action.statusEffect);
            string hex = ColorUtility.ToHtmlStringRGB(d.IconColor);
            parts.Add($"Applies <color=#{hex}>{d.Name}</color> {action.statusStacks}");
        }

        // Multi-target
        if (action.maxTargets > 1)
            parts.Add($"Up to {action.maxTargets} targets");

        // Bonus at max range
        if (action.bonusDamageAtMaxRange)
            parts.Add($"+{action.bonusDamage} damage at max range");

        // Push/Pull range
        if (isPushPull)
            parts.Add($"Range {action.range}");

        // Status action details
        if (action.effect == CardEffect.Status)
        {
            if (action.targetSelf) parts.Add("Targets self");
            else if (action.range > 0) parts.Add($"Range {action.range}");
        }

        // PlaceWeb range
        if (action.effect == CardEffect.PlaceWeb)
            parts.Add($"Range {action.range}");

        return parts.Count > 0 ? string.Join("  \u2022  ", parts) : null;
    }

    private static Color GetEffectGradientColor(CardEffect effect)
    {
        switch (effect)
        {
            case CardEffect.Move:
            case CardEffect.Dash:
            case CardEffect.Jump:        return new Color(0.25f, 0.40f, 0.20f);
            case CardEffect.Attack:
            case CardEffect.AttackLine:
            case CardEffect.AttackAoE:   return new Color(0.45f, 0.20f, 0.18f);
            case CardEffect.Push:
            case CardEffect.Pull:
            case CardEffect.PushAoE:     return new Color(0.45f, 0.30f, 0.15f);
            default:                     return new Color(0.22f, 0.30f, 0.38f);
        }
    }

    // ── Sprite generation ───────────────────────────────────────────────

    private static Sprite CreateGradientSprite(Color baseColor)
    {
        const int size = 64;
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        Color dark = baseColor * 0.4f; dark.a = 1f;
        Color light = baseColor * 1.2f; light.a = 1f;
        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size * 0.5f) / (size * 0.5f);
                float dy = (y - size * 0.5f) / (size * 0.5f);
                float vig = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 0.2f);
                tex.SetPixel(x, y, Color.Lerp(dark, light, t * vig));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    private static Sprite _circleSprite;
    private static Sprite CreateCircleSprite(int size)
    {
        if (_circleSprite != null) return _circleSprite;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f));
                float a = Mathf.Clamp01(r - dist);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        return _circleSprite;
    }
}
