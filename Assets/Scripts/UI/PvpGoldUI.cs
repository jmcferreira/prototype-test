using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays gold for both PVP players at the top of the screen.
/// Updates every frame from PvpMatchState.
/// </summary>
public class PvpGoldUI : MonoBehaviour
{
    private Text _text;

    public void Init()
    {
        var rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -5);
        rt.sizeDelta = new Vector2(300, 30);

        _text = gameObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 18;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.color = new Color(1f, 0.85f, 0.3f);
    }

    private void Update()
    {
        var match = PvpMatchState.Current;
        if (match == null || _text == null) return;
        _text.text = $"P1: {match.Gold[0]}g  |  Round {match.RoundNumber}  |  P2: {match.Gold[1]}g";
    }
}
