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
    public int HP { get; private set; } = 3;
    public int MaxHP { get; private set; } = 3;
    public bool IsAlive => HP > 0;

    private HexGrid _grid;
    private float _hexSize;
    private TextMesh _hpText;

    // --- Status effects ---
    private readonly Dictionary<StatusEffectType, int> _statuses = new();

    public void Init(Team team, HexCoord startCoord, HexGrid grid)
    {
        Team = team;
        _grid = grid;
        _hexSize = grid.HexSize;
        Coord = startCoord;
        PlaceAt(startCoord);
        gameObject.name = $"{team} Unit";

        // Simple colored cube so we can tell them apart
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = team == Team.Player ? Color.blue : Color.red;
        cube.GetComponent<MeshRenderer>().material = mat;

        // HP label floating above the unit
        var textGo = new GameObject("HP Label");
        textGo.transform.SetParent(transform);
        textGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        // Rotate to face the top-down camera (text on XZ plane, readable from Y+)
        textGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _hpText = textGo.AddComponent<TextMesh>();
        _hpText.characterSize = 0.2f;
        _hpText.fontSize = 48;
        _hpText.anchor = TextAnchor.MiddleCenter;
        _hpText.alignment = TextAlignment.Center;
        _hpText.color = Color.white;
        UpdateHPLabel();
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
    /// Take damage. Logs HP remaining.
    /// </summary>
    public void TakeHit(int damage)
    {
        HP -= damage;
        UpdateHPLabel();
        Debug.Log($"{Team} took {damage} damage — HP: {HP}");
    }

    /// <summary>
    /// Recover HP, capped at MaxHP.
    /// </summary>
    public void Heal(int amount)
    {
        HP = Mathf.Min(HP + amount, MaxHP);
        UpdateHPLabel();
        Debug.Log($"{Team} healed {amount} — HP: {HP}");
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
        Debug.Log($"{Team} gained {stacks} {def.Name} (now {newStacks}/{def.MaxStacks})");
        UpdateLabel();
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
                Debug.Log($"{Team} took {dmg} {def.Name} damage ({stacks} stacks)");
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
                Debug.Log($"{Team} {def.Name} wore off.");
            }
            else
            {
                _statuses[type] = newStacks;
                Debug.Log($"{Team} {def.Name} decayed to {newStacks} stacks.");
            }
        }
        foreach (var t in toRemove)
            _statuses.Remove(t);

        UpdateLabel();
    }

    /// <summary>
    /// Build a short string of active statuses for the unit label.
    /// </summary>
    private string StatusLabel()
    {
        var parts = new List<string>();
        foreach (var kvp in _statuses)
        {
            if (kvp.Value <= 0) continue;
            var def = StatusEffectDefs.Get(kvp.Key);
            parts.Add($"{def.Icon}{kvp.Value}");
        }
        return parts.Count > 0 ? " " + string.Join(" ", parts) : "";
    }

    private void UpdateLabel()
    {
        if (_hpText != null)
        {
            string hearts = new string('\u2665', Mathf.Max(0, HP));
            _hpText.text = hearts + StatusLabel();
        }
    }

    private void UpdateHPLabel()
    {
        UpdateLabel();
    }

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
