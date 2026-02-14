# PVP Mode Implementation Plan

## Overview
Add a PVP game mode alongside the existing Campaign. Two players (hot-seat) each
control a Champion and recruit faction creatures. Victory = destroy opponent's Base Tower.

### Key Design Decisions (from user answers):
- **Turn structure**: Full cycle x3 per round. Sequence: P1→P2→P2creatures→P1creatures→TowerDmg, repeated 3 times.
- **Guard towers**: Deal damage to enemies within Range 2 during Tower phase.
- **Economy**: Start 5g. Creature costs: cheap 2g, medium 3g, elite 5g. Guard towers give 2g/round.
- **Champion decks**: 2 new simple decks (4 cards each), faction-themed.

---

## Phase 1: Alliance System (foundation)

**Goal**: Add alliance-based friend/foe logic without breaking Campaign.

### Changes:
**Unit.cs**: Add `int Alliance` (default -1), `SetAlliance(int)`, `IsAlly(Unit)`, `IsEnemy(Unit)`.
When Alliance >= 0 on both units, use alliance for friend/foe. Otherwise fall through to `Team == Team`.

**7 resolvers** (Attack, AttackAoE, AttackLine, Push, Pull, Status): Change `unit.Team == caster.Team` → `caster.IsAlly(unit)`.

**EnemyUnit.cs**: In `FindPrimaryTarget`, change `u.Team == Team` → `IsAlly(u)`.

**HexToken.cs**: WebToken: `unit.Team == Owner.Team` → `Owner.IsAlly(unit)`. LootToken: keep `Team.Player` check (PVP won't use loot tokens).

**Fate draws** in PlayerUnit/EnemyUnit: `defender.Team == Team.Player` → `!IsEnemy(defender)` or keep as-is (fate UI is player-facing in both modes).

**UnitInfoPanel.cs** color: Use Alliance for color in PVP. Keep Team-based in Campaign.

**Verification**: Run Campaign — all combat, targeting, passives, tokens must work identically.

---

## Phase 2: Game Mode State + Main Menu

**Goal**: Menu screen at startup, branching GameSetup.

### New files:
- `Assets/Scripts/Data/GameModeState.cs` — static class with `GameMode` enum {None, Campaign, PVP} and `CurrentMode`.
- `Assets/Scripts/UI/MainMenuUI.cs` — Full-screen modal: title + 2 buttons (Campaign / PVP Match).

### Changes:
**GameSetup.cs**: `Setup()` checks `GameModeState.CurrentMode`:
  - `None` → show MainMenuUI, return without building game.
  - `Campaign` → existing campaign logic.
  - `PVP` → delegate to PvpGameSetup (placeholder until Phase 6).

**GameOverModalUI.cs**: "Restart"/"New Run" → set mode to `None` so menu shows on reload.

**Verification**: Game starts → menu appears. "Campaign" launches normal game. "PVP" shows placeholder.

---

## Phase 3: TowerUnit + PVP Arena Data

**Goal**: Tower entities and PVP map layout definitions.

### New files:
- `Assets/Scripts/Units/TowerUnit.cs` — `Unit` subclass:
  - No cards, no hand, never in turn queue.
  - `TowerType` enum {Base, Guard}.
  - Guard towers have `GoldPerRound` (default 2).
  - Overrides OnTurnStart/OnTurnEnd to no-op.
  - Visual: taller chip, distinct color (gold for Base, gray for Guard).
  - `DealTowerDamage(List<Unit> allUnits, HexGrid grid)` — finds enemies within range 2, deals 1 dmg each.

- `Assets/Scripts/Data/PvpArenaDef.cs` — Data class:
  ```
  arenaName, gridColumns(11), gridRows(11)
  p1BaseTowerPos(5,9), p2BaseTowerPos(5,1)
  p1GuardTowerPositions[(3,7),(7,7)]
  p2GuardTowerPositions[(3,3),(7,3)]
  p1ChampionSpawn(5,10), p2ChampionSpawn(5,0)
  baseTowerHP(20), guardTowerHP(10)
  ```

- `Assets/Scripts/Data/PvpArenaPool.cs` — Static provider for arena defs.

**Verification**: TowerUnit spawns on grid, has HP, takes damage, can be destroyed. No turn involvement.

---

## Phase 4: Faction System + Decks

**Goal**: Factions with creature pools, champion decks, creature decks.

### New files:
- `Assets/Scripts/Data/FactionDef.cs`:
  ```
  factionName, factionIcon
  championDef (UnitDef with deckId, passive)
  recruitPool (UnitDef[] with goldCost)
  ```

- `Assets/Scripts/Data/FactionLibrary.cs` — 2 factions:
  **Darkside**: Champion (dark mage deck), creatures: Spider(2g), Cultist(3g), Skeleton(3g), Witch(5g)
  **Brotherhood**: Champion (warrior deck), creatures: Soldier(2g), Guerrilla(3g), Sniper(5g)

### Changes:
**UnitDef.cs**: Add `int goldCost` field.

**CardLibrary.cs**: Add decks:
- `"champion_darkside"` — 4 cards: dark magic theme
- `"champion_brotherhood"` — 4 cards: martial theme
- `"skeleton"` — 2 cards (reuse spider-like movement + attack)
- `"witch"` — 2 cards (ranged + status)
- `"soldier"` — 2 cards (move + melee attack)
- `"guerrilla"` — 2 cards (dash + ranged)
- `"sniper"` — 2 cards (long-range attack)

**PassiveType.cs / PassiveFactory.cs**: Add types if needed, or reuse None for creatures.

**Verification**: FactionLibrary returns valid defs. All deckIds resolve to decks.

---

## Phase 5: PVP Turn Manager

**Goal**: Round-based cycling with tower phase and recruitment triggers.

### New file:
- `Assets/Scripts/Turns/PvpTurnManager.cs`:
  - Tracks `RoundNumber`, `CycleInRound` (0-2), current turn index.
  - Build turn order per cycle: [P1Champion, P2Champion, P2Creatures..., P1Creatures...]
  - After each unit acts → check victory (either Base Tower HP <= 0).
  - After all units acted in cycle → Tower damage phase: each Guard Tower calls DealTowerDamage.
  - After cycle 3 → end of round: award guard tower gold, trigger recruitment callback.
  - Exposes `OnTurnChanged`, `OnGameOver`, `OnRoundEnd` events.
  - `AddCreatures(EnemyUnit[])` to add recruited creatures to turn order.
  - `GetAliveUnits()` returns all (champions + creatures + towers).

### Key: Creature order is P2's first, then P1's (matching user spec: P2creatures → P1creatures).

**Verification**: Turns cycle correctly. Victory triggers on base tower death. Round counter works.

---

## Phase 6: PVP Game Setup + Faction Selection

**Goal**: Full PVP bootstrap from faction selection to game start.

### New files:
- `Assets/Scripts/UI/FactionSelectionUI.cs` — Sequential picker: P1 chooses faction, then P2.
  Shows faction cards with name, creature preview, champion description.

- `Assets/Scripts/PvpGameSetup.cs` — Called by GameSetup when mode is PVP:
  1. Show FactionSelectionUI → get both faction choices.
  2. Configure grid from PvpArenaDef.
  3. Spawn P1 Champion: `PlayerUnit, Team.Player, Alliance=0`.
  4. Spawn P2 Champion: `PlayerUnit, Team.Player, Alliance=1`.
  5. Spawn Base Towers: `TowerUnit, Alliance=0/1`.
  6. Spawn Guard Towers: `TowerUnit, Alliance=0/1`.
  7. Create PvpMatchState (tracks gold per player, round).
  8. Create shared HandUI (existing mechanism handles switching).
  9. Create PvpTurnManager.
  10. Create UnitInfoPanels (alliance-based side: 0=left, 1=right).
  11. Give starting gold (5g each).
  12. Start PvpTurnManager.

- `Assets/Scripts/Data/PvpMatchState.cs`:
  ```
  FactionDef P1Faction, P2Faction
  int P1Gold, P2Gold
  int RoundNumber
  List<EnemyUnit> P1Creatures, P2Creatures
  ```

### Changes:
**GameSetup.cs**: PVP branch delegates to `PvpGameSetup.Setup(hexGrid, uiRoot)`.

**Verification**: PVP match launches. 2 champions + 2 base + 4 guard towers on grid. Both champions play cards.

---

## Phase 7: Recruitment System

**Goal**: Between-round creature recruitment and spawning.

### New file:
- `Assets/Scripts/UI/RecruitmentUI.cs` — Modal between rounds:
  - Shows current player's faction creature roster (icon, name, stats, cost).
  - Gold balance display.
  - Up to 3 slots to fill (click creature to add, click slot to remove).
  - "Done" button.
  - Sequential: P1 recruits → P2 recruits → spawn all → resume.

  After both confirm:
  - Spawn creatures at random empty hexes adjacent to owner's Base Tower.
  - Create as EnemyUnit with correct Alliance.
  - Add to PvpTurnManager turn order.
  - Create UnitInfoPanels for new creatures.

### Changes:
**PvpTurnManager.cs**: `OnRoundEnd` event triggers RecruitmentUI. After recruitment completes, rebuild turn order with new creatures and start next round.

**Verification**: Recruitment UI appears between rounds. Creatures spawn correctly. Creatures act in subsequent cycles with correct targeting.

---

## Phase 8: PVP Game Over + Polish

**Goal**: Complete PVP experience with proper endings.

### Changes:
**GameOverModalUI.cs**: PVP branch:
  - "Player 1 Wins!" / "Player 2 Wins!" with faction branding.
  - Match stats: rounds played, creatures recruited, towers destroyed.
  - "Rematch" button → reload scene with PVP mode, skip menu.
  - "Main Menu" button → set mode to None, reload.

**PvpTurnManager.cs**: Gold award at end of round (alive guard towers give 2g to owner).

**TurnBannerUI.cs**: Alliance-aware turn display (show "Player 1 — Ranger" etc.).

**Unit.cs chip colors**: Alliance-based coloring for PVP (blue=alliance 0, red=alliance 1).

**Verification**: Full PVP match playable from menu → faction select → combat → recruitment → victory/defeat → rematch/menu.

---

## File Summary

### New files (13):
- Data: GameModeState, PvpArenaDef, PvpArenaPool, PvpMatchState, FactionDef, FactionLibrary
- Units: TowerUnit
- Turns: PvpTurnManager
- UI: MainMenuUI, FactionSelectionUI, RecruitmentUI
- Setup: PvpGameSetup
- (CardLibrary additions are edits, not new files)

### Modified files (14):
- Unit.cs (Alliance system)
- 6 resolvers (IsAlly refactor)
- EnemyUnit.cs (IsAlly targeting)
- HexToken.cs (IsAlly for WebToken)
- GameSetup.cs (mode branching)
- GameOverModalUI.cs (PVP ending + menu return)
- UnitInfoPanel.cs (alliance-based side)
- TurnBannerUI.cs (PVP turn display)
- CardLibrary.cs (new decks), UnitDef.cs (goldCost), PassiveType.cs, PassiveFactory.cs
