using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PVP turn manager: round-based with multiple cycles per round.
/// Each cycle: P1 champion → P2 champion → P2 creatures → P1 creatures → Tower phase.
/// After all cycles in a round, triggers recruitment.
/// Victory: opponent's base tower destroyed.
/// </summary>
public class PvpTurnManager : TurnManager
{
    private PlayerUnit _p1Champion;
    private PlayerUnit _p2Champion;
    private TowerUnit _p1BaseTower;
    private TowerUnit _p2BaseTower;
    private List<TowerUnit> _allTowers = new();

    // All units that participate in turns (not towers)
    private List<Unit> _allActors = new();

    public int RoundNumber { get; private set; } = 1;
    public int CycleInRound { get; private set; } = 0;
    public int CyclesPerRound { get; private set; } = 3;

    /// <summary>Fired at the end of each round, before recruitment.</summary>
    public event Action<int> OnRoundEnd;

    /// <summary>Called by recruitment UI when recruitment phase is complete.</summary>
    public event Action OnRecruitmentComplete;

    public void InitPvp(PlayerUnit p1, PlayerUnit p2,
                        TowerUnit p1Base, TowerUnit p2Base,
                        List<TowerUnit> towers, int cyclesPerRound)
    {
        _p1Champion = p1;
        _p2Champion = p2;
        _p1BaseTower = p1Base;
        _p2BaseTower = p2Base;
        _allTowers = towers;
        CyclesPerRound = cyclesPerRound;
    }

    /// <summary>
    /// Start the PVP match. turnOrder should include champions and any initial creatures.
    /// Towers are NOT in the turn order (they act during tower phase).
    /// </summary>
    public override void Begin(List<Unit> turnOrder)
    {
        _allActors = new List<Unit>(turnOrder);

        // Build cycle turn order and add towers for GetAliveUnits
        _turnOrder = new List<Unit>();
        foreach (var t in _allTowers)
            _turnOrder.Add(t);
        _turnOrder.AddRange(_allActors);

        RoundNumber = 1;
        CycleInRound = 1;
        TurnNumber = 1;

        StartNextCycle();
    }

    public override List<Unit> GetAliveUnits()
    {
        var alive = new List<Unit>();
        // Include towers so they can be targeted by attacks
        foreach (var t in _allTowers)
            if (t.IsAlive) alive.Add(t);
        foreach (var u in _allActors)
            if (u.IsAlive) alive.Add(u);
        return alive;
    }

    /// <summary>
    /// Add a newly recruited creature to the match.
    /// </summary>
    public void AddCreature(Unit creature)
    {
        _allActors.Add(creature);
        // Also add to _turnOrder for GetAliveUnits
        if (!_turnOrder.Contains(creature))
            _turnOrder.Add(creature);
    }

    public override void EndCurrentTurn()
    {
        if (_turnOrder.Count == 0) return;

        CurrentUnit.OnTurnEnd();

        // Check for victory: base tower destroyed
        if (!_p1BaseTower.IsAlive)
        {
            Debug.Log("P2 wins! P1's base tower destroyed!");
            FireGameOver(false); // false = P1 lost (convention: P1 is "player")
            return;
        }
        if (!_p2BaseTower.IsAlive)
        {
            Debug.Log("P1 wins! P2's base tower destroyed!");
            FireGameOver(true); // true = P1 won
            return;
        }

        // Advance to next unit in cycle order
        _cycleIndex++;
        AdvanceCycle();
    }

    // ── Cycle management ──────────────────────────────────────────────

    private List<Unit> _cycleOrder = new();
    private int _cycleIndex;

    private void StartNextCycle()
    {
        // Build cycle order: P1 → P2 → P2 creatures → P1 creatures
        _cycleOrder = new List<Unit>();

        if (_p1Champion.IsAlive) _cycleOrder.Add(_p1Champion);
        if (_p2Champion.IsAlive) _cycleOrder.Add(_p2Champion);

        // P2 creatures (alliance 1)
        foreach (var u in _allActors)
        {
            if (u == _p1Champion || u == _p2Champion) continue;
            if (!u.IsAlive) continue;
            if (u.Alliance == 1) _cycleOrder.Add(u);
        }

        // P1 creatures (alliance 0)
        foreach (var u in _allActors)
        {
            if (u == _p1Champion || u == _p2Champion) continue;
            if (!u.IsAlive) continue;
            if (u.Alliance == 0) _cycleOrder.Add(u);
        }

        _cycleIndex = 0;

        if (_cycleOrder.Count == 0)
        {
            // No units can act — skip to tower phase
            StartCoroutine(TowerPhase());
            return;
        }

        StartCycleUnit();
    }

    private void AdvanceCycle()
    {
        // Skip dead units
        while (_cycleIndex < _cycleOrder.Count && !_cycleOrder[_cycleIndex].IsAlive)
            _cycleIndex++;

        if (_cycleIndex >= _cycleOrder.Count)
        {
            // Cycle complete — tower phase
            StartCoroutine(TowerPhase());
            return;
        }

        StartCycleUnit();
    }

    private void StartCycleUnit()
    {
        var unit = _cycleOrder[_cycleIndex];

        // Update TurnManager's _currentIndex to point at this unit
        _currentIndex = _turnOrder.IndexOf(unit);
        TurnNumber++;

        Debug.Log($"=== R{RoundNumber}C{CycleInRound} — {unit.DisplayName}'s turn ===");
        BattleLog.AddTurnHeader(unit.DisplayName, TurnNumber);
        FireTurnChanged(unit);
        unit.OnTurnStart();
    }

    // ── Tower phase ───────────────────────────────────────────────────

    private IEnumerator TowerPhase()
    {
        Debug.Log($"--- Tower Phase (R{RoundNumber}C{CycleInRound}) ---");
        BattleLog.AddTurnHeader("Tower Phase", TurnNumber);

        yield return new WaitForSeconds(0.5f);

        var allUnits = GetAliveUnits();
        foreach (var tower in _allTowers)
        {
            if (tower is TowerUnit t && t.IsAlive && t.TowerType == TowerType.Guard)
            {
                t.FireAtEnemies(allUnits);
                yield return new WaitForSeconds(0.3f);
            }
        }

        // Check victory after tower damage
        if (!_p1BaseTower.IsAlive)
        {
            Debug.Log("P2 wins! P1's base tower destroyed!");
            FireGameOver(false);
            yield break;
        }
        if (!_p2BaseTower.IsAlive)
        {
            Debug.Log("P1 wins! P2's base tower destroyed!");
            FireGameOver(true);
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        // End of cycle — advance or start recruitment
        CycleInRound++;
        if (CycleInRound > CyclesPerRound)
        {
            // Round complete — guard tower gold income
            GrantGuardTowerGold();

            Debug.Log($"=== Round {RoundNumber} complete! Recruitment phase ===");
            OnRoundEnd?.Invoke(RoundNumber);
            // Recruitment UI will call ResumeAfterRecruitment() when done
        }
        else
        {
            StartNextCycle();
        }
    }

    private void GrantGuardTowerGold()
    {
        var match = PvpMatchState.Current;
        if (match == null) return;

        int goldPerTower = match.Arena.guardTowerGoldPerRound;
        foreach (var tower in _allTowers)
        {
            if (tower is TowerUnit t && t.IsAlive && t.TowerType == TowerType.Guard)
            {
                match.AddGold(t.OwnerAlliance, goldPerTower);
                Debug.Log($"{t.DisplayName} generates {goldPerTower} Gold for P{t.OwnerAlliance + 1}");
            }
        }
    }

    /// <summary>
    /// Called after recruitment is done to start the next round.
    /// </summary>
    public void ResumeAfterRecruitment()
    {
        RoundNumber++;
        CycleInRound = 1;
        OnRecruitmentComplete?.Invoke();
        StartNextCycle();
    }
}
