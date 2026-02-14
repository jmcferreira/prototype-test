using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen modal offering 3 tier-weighted relics to pick from.
/// Shown between scenarios after victory. Player picks one, it's stored in RunState.
/// </summary>
public class RewardScreenUI : MonoBehaviour
{
    private Action _onPicked;

    public void Init() { gameObject.SetActive(false); }

    /// <summary>
    /// Show the reward screen with 3 random relics. Calls onPicked after selection.
    /// </summary>
    public void Show(Action onPicked)
    {
        _onPicked = onPicked;
        gameObject.SetActive(true);
        BuildScreen();
    }

    private void BuildScreen()
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Full-screen overlay
        var overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;

        // Central container
        var containerGo = new GameObject("Container");
        containerGo.transform.SetParent(transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(600f, 340f);

        var containerLayout = containerGo.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 16;
        containerLayout.childAlignment = TextAnchor.MiddleCenter;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(containerGo.transform, false);
        var titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 30;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1f, 0.85f, 0.3f);
        titleTxt.text = "Choose a Reward";
        titleTxt.raycastTarget = false;
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 40f;

        // Show current relics count
        int relicCount = RunState.Current?.Relics.Count ?? 0;
        if (relicCount > 0)
        {
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(containerGo.transform, false);
            var subTxt = subGo.AddComponent<Text>();
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 14;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.6f, 0.6f, 0.6f);
            subTxt.text = $"Relics: {relicCount}";
            subTxt.raycastTarget = false;
            var subLe = subGo.AddComponent<LayoutElement>();
            subLe.preferredHeight = 20f;
        }

        // Cards row
        var rowGo = new GameObject("CardsRow");
        rowGo.transform.SetParent(containerGo.transform, false);
        var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 220f;

        // Generate 3 relics
        var relics = RelicLibrary.GetRandomRelics(3);
        foreach (var relic in relics)
            BuildRelicCard(rowGo, relic);
    }

    private void BuildRelicCard(GameObject parent, RelicDef relic)
    {
        // Card background
        var cardGo = new GameObject($"Relic_{relic.id}");
        cardGo.transform.SetParent(parent.transform, false);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var cardLe = cardGo.AddComponent<LayoutElement>();
        cardLe.preferredWidth = 170f;
        cardLe.preferredHeight = 220f;

        // Make it clickable
        var btn = cardGo.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        var captured = relic; // closure capture
        btn.onClick.AddListener(() => OnRelicPicked(captured));

        // Border colored by tier
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(cardGo.transform, false);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = GetTierColor(relic.tier);
        borderImg.raycastTarget = false;
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        // Inner panel
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(borderGo.transform, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.1f, 0.1f, 0.13f, 0.97f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        // Vertical layout for content
        var layout = innerGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 12, 12);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Tier badge
        string tierLabel = relic.tier.ToString().ToUpper();
        AddCardText(innerGo, "Tier", tierLabel, GetTierColor(relic.tier), 11, FontStyle.Bold, 16f);

        // Relic name
        AddCardText(innerGo, "Name", relic.relicName, Color.white, 18, FontStyle.Bold, 30f);

        // Description
        AddCardText(innerGo, "Desc", relic.description, new Color(0.75f, 0.75f, 0.75f), 14, FontStyle.Normal, 80f);

        // Effect icon
        string icon = GetEffectIcon(relic.effect);
        AddCardText(innerGo, "Icon", icon, GetTierColor(relic.tier), 28, FontStyle.Normal, 40f);
    }

    private void AddCardText(GameObject parent, string name, string text, Color color,
                             int fontSize, FontStyle style, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.text = text;
        txt.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void OnRelicPicked(RelicDef relic)
    {
        RunState.Current?.AddRelic(relic);
        gameObject.SetActive(false);
        _onPicked?.Invoke();
    }

    private static Color GetTierColor(ItemTier tier)
    {
        return tier switch
        {
            ItemTier.Common => new Color(0.6f, 0.6f, 0.6f),
            ItemTier.Uncommon => new Color(0.3f, 0.75f, 0.35f),
            ItemTier.Rare => new Color(1f, 0.75f, 0.2f),
            _ => Color.white,
        };
    }

    private static string GetEffectIcon(RelicEffect effect)
    {
        return effect switch
        {
            RelicEffect.StartingBlock => "\u26DB",   // ⛛
            RelicEffect.MaxHPBonus => "\u2665",       // ♥
            RelicEffect.HealBonus => "\u2726",        // ✦
            RelicEffect.GoldBonus => "\u2B50",        // ⭐ (fallback coin)
            RelicEffect.DamageBonus => "\u2694",      // ⚔
            _ => "?",
        };
    }
}
