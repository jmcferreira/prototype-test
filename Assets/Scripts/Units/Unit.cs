using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team { Player, Enemy }

/// <summary>
/// Base class for any actor that occupies a hex tile.
/// </summary>
public abstract class Unit : MonoBehaviour
{
    public Team Team { get; private set; }
    public HexCoord Coord { get; private set; }
    public string DisplayName { get; private set; }
    public int HP { get; private set; } = 3;
    public int MaxHP { get; private set; } = 3;
    public bool IsAlive => HP > 0;

    /// <summary>Fired when HP or statuses change. UI panels subscribe to this.</summary>
    public event Action OnChanged;

    private HexGrid _grid;
    private float _hexSize;

    // --- Status effects ---
    private readonly Dictionary<StatusEffectType, int> _statuses = new();

    public void Init(Team team, HexCoord startCoord, HexGrid grid, string displayName = null, int maxHP = 3, string acronym = null)
    {
        Team = team;
        DisplayName = displayName ?? team.ToString();
        _grid = grid;
        _hexSize = grid.HexSize;
        MaxHP = maxHP;
        HP = maxHP;
        Coord = startCoord;
        PlaceAt(startCoord);
        gameObject.name = DisplayName;

        Color teamBright = team == Team.Player
            ? new Color(0.25f, 0.42f, 0.92f)
            : new Color(0.88f, 0.22f, 0.22f);
        Color teamDark = team == Team.Player
            ? new Color(0.12f, 0.18f, 0.50f)
            : new Color(0.50f, 0.12f, 0.12f);

        // Poker-chip rim (outer, darker, slightly larger)
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.transform.SetParent(transform);
        rim.transform.localPosition = Vector3.zero;
        rim.transform.localScale = new Vector3(0.78f, 0.045f, 0.78f);
        rim.GetComponent<MeshRenderer>().material = CreateUnlitMat(teamDark);
        rim.name = "ChipRim";

        // Poker-chip face (inner, brighter)
        var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.transform.SetParent(transform);
        face.transform.localPosition = new Vector3(0f, 0.005f, 0f);
        face.transform.localScale = new Vector3(0.64f, 0.05f, 0.64f);
        face.GetComponent<MeshRenderer>().material = CreateUnlitMat(teamBright);
        face.name = "ChipFace";

        // Acronym label on top (world-space TextMesh, faces the top-down camera)
        string label = acronym ?? (DisplayName.Length > 0 ? DisplayName.Substring(0, 1) : "?");
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(transform);
        labelGo.transform.localPosition = new Vector3(0f, 0.07f, 0f);
        labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 64;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;
    }

    private static Material CreateUnlitMat(Color color)
    {
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        return mat;
    }

    /// <summary>
    /// Move this unit to a hex. Used by card resolvers for move/push/pull.
    /// </summary>
    public void ForceMoveTo(HexCoord target)
    {
        Coord = target;
        PlaceAt(target);
    }

    /// <summary>
    /// Take damage. Logs HP remaining. Hides visuals when defeated.
    /// </summary>
    public void TakeHit(int damage)
    {
        HP = Mathf.Max(0, HP - damage);
        Debug.Log($"{DisplayName} took {damage} damage — HP: {HP}");
        SpawnDamageNumber(damage);
        if (HP <= 0)
            HideVisuals();
        NotifyChanged();
    }

    /// <summary>
    /// Disable all child renderers so the poker chip disappears from the map.
    /// </summary>
    private void HideVisuals()
    {
        Debug.Log($"{DisplayName} has been defeated!");
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }

    /// <summary>
    /// Spawn a floating "-X" damage number above the unit that drifts up and fades out.
    /// </summary>
    private void SpawnDamageNumber(int damage)
    {
        var go = new GameObject("DmgNum");
        go.transform.position = transform.position + new Vector3(0f, 0.15f, 0.3f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = $"-{damage} \u2665";
        tm.fontSize = 64;
        tm.characterSize = 0.14f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.2f, 0.15f);
        tm.fontStyle = FontStyle.Bold;

        StartCoroutine(AnimateDamageNumber(go, tm));
    }

    private static IEnumerator AnimateDamageNumber(GameObject go, TextMesh tm)
    {
        float duration = 0.9f;
        float elapsed = 0f;
        Vector3 startPos = go.transform.position;
        Color startColor = tm.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Drift upward (in world Z = screen up for top-down camera)
            go.transform.position = startPos + new Vector3(0f, 0f, t * 0.8f);

            // Fade out in the second half
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            tm.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        UnityEngine.Object.Destroy(go);
    }

    /// <summary>
    /// Recover HP, capped at MaxHP.
    /// </summary>
    public void Heal(int amount)
    {
        HP = Mathf.Min(HP + amount, MaxHP);
        Debug.Log($"{DisplayName} healed {amount} — HP: {HP}");
        NotifyChanged();
    }

    // --- Status effect methods ---

    /// <summary>
    /// Apply stacks of a status effect, capped at the type's max.
    /// </summary>
    public void ApplyStatus(StatusEffectType type, int stacks)
    {
        var def = StatusEffectDefs.Get(type);
        int current = GetStatusStacks(type);
        int newStacks = Mathf.Min(current + stacks, def.MaxStacks);
        _statuses[type] = newStacks;
        Debug.Log($"{DisplayName} gained {stacks} {def.Name} (now {newStacks}/{def.MaxStacks})");
        NotifyChanged();
    }

    /// <summary>
    /// Get the current number of stacks for a status effect.
    /// </summary>
    public int GetStatusStacks(StatusEffectType type)
    {
        return _statuses.TryGetValue(type, out int s) ? s : 0;
    }

    /// <summary>
    /// Whether the unit currently has any stacks of this status.
    /// </summary>
    public bool HasStatus(StatusEffectType type) => GetStatusStacks(type) > 0;

    /// <summary>
    /// Fire all statuses that match the given trigger. Called generically —
    /// pass OnAction after each action resolve, OnTurnEnd at end of turn.
    /// </summary>
    public void TriggerStatuses(StatusTrigger trigger)
    {
        foreach (var kvp in StatusEffectDefs.All)
        {
            var type = kvp.Key;
            var def = kvp.Value;
            int stacks = GetStatusStacks(type);
            if (stacks <= 0) continue;
            if (def.DamageTrigger != trigger) continue;

            int dmg = def.DamagePerStack * stacks;
            if (dmg > 0)
            {
                TakeHit(dmg);
                Debug.Log($"{DisplayName} took {dmg} {def.Name} damage ({stacks} stacks)");
            }
        }
    }

    /// <summary>
    /// Decay all status stacks at end of turn. Call from OnTurnEnd.
    /// </summary>
    public void DecayStatuses()
    {
        var toRemove = new List<StatusEffectType>();
        foreach (var kvp in StatusEffectDefs.All)
        {
            var type = kvp.Key;
            var def = kvp.Value;
            int stacks = GetStatusStacks(type);
            if (stacks <= 0 || def.DecayPerTurn <= 0) continue;

            int newStacks = stacks - def.DecayPerTurn;
            if (newStacks <= 0)
            {
                toRemove.Add(type);
                Debug.Log($"{DisplayName} {def.Name} wore off.");
            }
            else
            {
                _statuses[type] = newStacks;
                Debug.Log($"{DisplayName} {def.Name} decayed to {newStacks} stacks.");
            }
        }
        foreach (var t in toRemove)
            _statuses.Remove(t);

        NotifyChanged();
    }

    private void NotifyChanged() => OnChanged?.Invoke();

    public HexGrid Grid => _grid;

    private void PlaceAt(HexCoord coord)
    {
        transform.position = coord.ToWorldPosition(_hexSize) + Vector3.up * 0.1f;
    }

    /// <summary>Called by TurnManager when this unit's turn begins.</summary>
    public virtual void OnTurnStart() { }

    /// <summary>Called by TurnManager when this unit's turn ends.</summary>
    public virtual void OnTurnEnd()
    {
        // Poison damage, then decay all statuses (Poison -1 stack, Blind wears off, etc.)
        TriggerStatuses(StatusTrigger.OnTurnEnd);
        DecayStatuses();
    }
}
