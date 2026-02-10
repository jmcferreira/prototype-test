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

    private HexGrid _grid;
    private float _hexSize;
    private TextMesh _nameText;
    private TextMesh _hpText;
    private readonly Dictionary<StatusEffectType, TextMesh> _statusIcons = new();
    private Transform _statusRow;

    // --- Status effects ---
    private readonly Dictionary<StatusEffectType, int> _statuses = new();

    public void Init(Team team, HexCoord startCoord, HexGrid grid, string displayName = null)
    {
        Team = team;
        DisplayName = displayName ?? team.ToString();
        _grid = grid;
        _hexSize = grid.HexSize;
        Coord = startCoord;
        PlaceAt(startCoord);
        gameObject.name = DisplayName;

        // Simple colored cube so we can tell them apart
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = team == Team.Player ? Color.blue : Color.red;
        cube.GetComponent<MeshRenderer>().material = mat;

        BuildInfoPanel();
        UpdateLabel();
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

    // --- Info panel (name + HP box beneath the unit) ---

    private void BuildInfoPanel()
    {
        // Panel sits below the unit on screen (−Z in world = down on screen)
        var panelGo = new GameObject("InfoPanel");
        panelGo.transform.SetParent(transform);
        panelGo.transform.localPosition = new Vector3(0f, 0.15f, -0.7f);
        // Lie flat on XZ plane, readable from the top-down camera
        panelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Background — thin cube so it renders from any angle
        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.transform.SetParent(panelGo.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        bg.transform.localScale = new Vector3(1.4f, 0.6f, 0.02f);
        Object.Destroy(bg.GetComponent<BoxCollider>());
        var bgMat = new Material(Shader.Find("Unlit/Color"));
        bgMat.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        bg.GetComponent<MeshRenderer>().material = bgMat;

        // Horizontal divider
        var divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
        divider.transform.SetParent(panelGo.transform, false);
        divider.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        divider.transform.localScale = new Vector3(1.3f, 0.012f, 0.02f);
        Object.Destroy(divider.GetComponent<BoxCollider>());
        var divMat = new Material(Shader.Find("Unlit/Color"));
        divMat.color = new Color(0.35f, 0.35f, 0.4f, 1f);
        divider.GetComponent<MeshRenderer>().material = divMat;

        // --- Top section: Name ---
        var nameGo = new GameObject("NameLabel");
        nameGo.transform.SetParent(panelGo.transform, false);
        nameGo.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        _nameText = nameGo.AddComponent<TextMesh>();
        _nameText.characterSize = 0.08f;
        _nameText.fontSize = 48;
        _nameText.fontStyle = FontStyle.Bold;
        _nameText.anchor = TextAnchor.MiddleCenter;
        _nameText.alignment = TextAlignment.Center;
        _nameText.color = Color.white;
        _nameText.text = DisplayName;

        // --- Bottom section: HP hearts ---
        var hpGo = new GameObject("HPLabel");
        hpGo.transform.SetParent(panelGo.transform, false);
        hpGo.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        _hpText = hpGo.AddComponent<TextMesh>();
        _hpText.characterSize = 0.1f;
        _hpText.fontSize = 44;
        _hpText.anchor = TextAnchor.MiddleCenter;
        _hpText.alignment = TextAlignment.Center;
        _hpText.color = new Color(0.95f, 0.25f, 0.25f);

        // --- Bottom section: Status icon row (below HP) ---
        var statusRowGo = new GameObject("StatusRow");
        statusRowGo.transform.SetParent(panelGo.transform, false);
        statusRowGo.transform.localPosition = new Vector3(0f, -0.22f, 0f);
        _statusRow = statusRowGo.transform;

        // Pre-create one TextMesh per status type, each with its own color
        foreach (var kvp in StatusEffectDefs.All)
        {
            var type = kvp.Key;
            var def = kvp.Value;

            var iconGo = new GameObject($"Status_{def.Name}");
            iconGo.transform.SetParent(_statusRow, false);
            var tm = iconGo.AddComponent<TextMesh>();
            tm.characterSize = 0.08f;
            tm.fontSize = 40;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = def.IconColor;
            iconGo.SetActive(false);
            _statusIcons[type] = tm;
        }
    }

    private void UpdateLabel()
    {
        if (_nameText != null)
            _nameText.text = DisplayName;
        if (_hpText != null)
        {
            string hearts = new string('\u2665', Mathf.Max(0, HP));
            _hpText.text = hearts;
        }
        UpdateStatusIcons();
    }

    private void UpdateStatusIcons()
    {
        // Collect active statuses to lay them out centered
        var active = new List<StatusEffectType>();
        foreach (var kvp in _statusIcons)
        {
            int stacks = GetStatusStacks(kvp.Key);
            if (stacks > 0)
                active.Add(kvp.Key);
            kvp.Value.gameObject.SetActive(stacks > 0);
        }

        // Position active icons in a centered row
        float spacing = 0.35f;
        float totalWidth = (active.Count - 1) * spacing;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < active.Count; i++)
        {
            var type = active[i];
            var def = StatusEffectDefs.Get(type);
            int stacks = GetStatusStacks(type);
            var tm = _statusIcons[type];
            tm.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
            tm.text = $"{def.Icon}{stacks}";
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
