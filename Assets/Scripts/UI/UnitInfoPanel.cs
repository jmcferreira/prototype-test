using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space side panel showing a unit's portrait, name, HP, and status effects.
/// Player panel anchors to the left, enemy panels stack on the right.
/// Border colour brightens when it is that unit's turn.
/// </summary>
public class UnitInfoPanel : MonoBehaviour
{
    private Unit _unit;
    private TurnManager _turnManager;

    private Image _borderImage;
    private Text _nameText;
    private Text _hpText;
    private readonly Dictionary<StatusEffectType, Text> _statusTexts = new();
    private GameObject _deceasedOverlay;

    private Color _borderActiveColor;
    private Color _borderInactiveColor;
    private static readonly Color DeceasedBorderColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);

    public void Init(Unit unit, TurnManager turnManager, bool isLeft, int stackIndex = 0)
    {
        _unit = unit;
        _turnManager = turnManager;

        Color teamColor = unit.Team == Team.Player
            ? new Color(0.3f, 0.55f, 1f)
            : new Color(1f, 0.3f, 0.3f);
        _borderActiveColor = teamColor;
        _borderInactiveColor = new Color(teamColor.r * 0.25f, teamColor.g * 0.25f, teamColor.b * 0.25f, 0.85f);

        BuildPanel(isLeft, teamColor, stackIndex);
        Refresh();

        _unit.OnChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (_unit != null) _unit.OnChanged -= Refresh;
    }

    // ── Build ──────────────────────────────────────────────────────────

    private void BuildPanel(bool isLeft, Color teamColor, int stackIndex)
    {
        var rect = gameObject.AddComponent<RectTransform>();

        if (isLeft)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(12f, 80f);
            rect.sizeDelta = new Vector2(180f, 260f);
        }
        else
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            // Stack multiple enemy panels vertically
            float y = -100f + stackIndex * 200f;
            rect.anchoredPosition = new Vector2(-12f, y);
            rect.sizeDelta = new Vector2(170f, 190f);
        }

        // Coloured border (bright = active turn, dim = inactive)
        _borderImage = gameObject.AddComponent<Image>();
        _borderImage.color = _borderInactiveColor;
        _borderImage.raycastTarget = false;

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

        // Status icons row
        BuildStatusRow(innerGo.transform);

        // Deceased overlay (hidden until unit dies)
        BuildDeceasedOverlay();
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
        txt.fontSize = size > 80 ? 48 : 30;
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

    private void BuildStatusRow(Transform parent)
    {
        var go = new GameObject("StatusRow");
        go.transform.SetParent(parent, false);
        var hl = go.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 4;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 24;

        foreach (var kvp in StatusEffectDefs.All)
        {
            var type = kvp.Key;
            var def = kvp.Value;

            var stGo = new GameObject($"Status_{def.Name}");
            stGo.transform.SetParent(go.transform, false);
            var stText = stGo.AddComponent<Text>();
            stText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stText.fontSize = 18;
            stText.alignment = TextAnchor.MiddleCenter;
            stText.color = def.IconColor;
            stText.raycastTarget = false;
            var stLe = stGo.AddComponent<LayoutElement>();
            stLe.preferredWidth = 42;
            stLe.preferredHeight = 24;
            stGo.SetActive(false);
            _statusTexts[type] = stText;
        }
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

            // Hide all status icons
            foreach (var kvp in _statusTexts)
                kvp.Value.gameObject.SetActive(false);
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
                kvp.Value.text = $"{def.Icon}{stacks}";
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
