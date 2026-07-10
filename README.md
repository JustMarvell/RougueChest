# RougueChest (working title)

A chess-based strategy game where capturing a piece transitions into an HSR-style turn-based team combat encounter.

---

## 0. Implementation Status

**Chess Core** (`Assets/Scripts/Chess/Core`) — done: `Board`, `Piece`, `Square`, `Move`, `MoveGenerator` (pseudo-legal + legal move gen, attack detection), `GameState` (turn tracking, check/checkmate/stalemate, pending-capture handoff to combat via `OnCaptureTriggered`/`ResolveCapture`, and now the match-persistent `WhiteSP`/`BlackSP` skill point pools, each starting at 2).

**Chess View** (`Assets/Scripts/Chess/View`) — done: `BoardView` (builds board/pieces, TMP turn + selection UI, redraw on move; now also wires up `CombatView`, `CombatStageController`, and `CombatCameraController` and drives the full capture → combat → resolution flow via `ResolveActiveSelection`), `BoardInputHandler` (click-to-select/move, plus click-to-pick/confirm routing during `GameStatus.AwaitingCombat`).

**Combat Core** (`Assets/Scripts/Chess/Core` combat classes, namespace `Combat.Core`) — done:
- `CombatUnit`: HP/ATK/SPD plus Energy/MaxEnergy/`UltimateReady`, an assigned `PieceCombatKit`, and an `IsFrozen` flag (placeholder for the future Stalemate reaction).
- `TurnOrderService`: discrete Action Value queue (`AV = BaseActionValue / SPD`), a priority queue keyed on "AV remaining until next action." Deterministic/serializable by construction. Supports speed-change rescaling and percent-based "advance turn," plus non-mutating turn-order preview.
- `CombatState`: now fully data-driven — `ResolveAction` reads the acting `AbilityDefinition`'s `Kind` (Basic/Skill/Ultimate) to handle SP generation/spending and Energy gain, then runs every `AbilityEffect` the ability carries. Fires events for unit turn start, damage, heal, defeat, and combat end.
- `AbilityDefinition` / `AbilityEffect` (`Assets/Scripts/Combat/Core`): polymorphic effect list (`[SerializeReference]`) per ability. Implemented effects: `DamageEffect`, `HealEffect`, `SpeedChangeEffect` (Knight's SPD buff, plugs into `TurnOrderService.ApplySpeedChange`), `AdvanceTurnEffect` (Knight's "Advance Forward" ultimate, plugs into `TurnOrderService.AdvanceTurn`).
- `PieceCombatKit`: Basic/Skill/Ultimate triplet + per-piece `MaxEnergy`.
- `SkillPointPool`, `CombatTargeting`, `CombatAction`, `ICombatDecisionProvider` / `PlayerDecisionProvider` (event-driven: raises `OnDecisionNeeded`, waits for `SubmitAction`).

**Combat Integration** (`Assets/Scripts/Combat/Integration`) — done, and now wired to manual team selection:
- `PieceCombatFactory`: placeholder baseline HP/ATK/SPD per piece type (Queen strongest, Pawn weakest), now also assigns each unit its `PieceCombatKit` from `DefaultCombatKits`.
- `DefaultCombatKits`: placeholder-but-functional Basic/Skill/Ultimate abilities for all six piece types (numbers not balanced), including the Knight's turn-manipulation skill/ultimate and Rook/King heal-style skills as stand-ins for a future Shield/Taunt effect.
- `CaptureCombatResolver`: **now consumes a `CaptureTeamSelection`** — builds real N-vs-N teams from the player's picks (via `selection.GetTeams()`) instead of headless 1v1, and pulls each side's persistent `SkillPointPool` from `GameState`.

**Combat Selection** (`Assets/Scripts/Combat/Selection`) — done **and connected**:
- `CaptureTeamSelection`: computes the 3x3 eligible-ally squares around attacker/defender, phases through `AttackerPicking → DefenderPicking → Ready`, manual toggle-pick up to 5 per team.
- `BoardInputHandler` routes clicks during `AwaitingCombat` into `TogglePick`/`ConfirmCurrentPhase`, and `BoardView.ResolveActiveSelection` fires once selection is `Ready`, building the combat encounter end-to-end.

**Combat View (UI)** (`Assets/Scripts/Combat/View`) — new, done for the core loop:
- `CombatView`: turn-order rail (`PreviewUpcoming`), team HP bars, and action buttons (Basic/Skill/Ultimate) with full targeting flow (SingleEnemy/SingleAlly/AllEnemies/AllAllies/Self), SP-affordability and ultimate-readiness checks, and a prompt label.
- `CombatUnitBarEntry`: reusable HP bar/portrait/click entry shared by the rail and team bars; dumb by design, all click semantics live in `CombatView`.

**Combat Stage (3D)** (`Assets/Scripts/Combat/View`) — new, done for the core loop:
- `CombatFormation`: attacker/defender slot layout — manual `Transform` markers with an auto-generated-row fallback.
- `CombatStageController`: spawns/despawns per-unit `CombatUnitActor`s from `PieceModelSet`, drives acting-highlight/attack/hit/defeat animations off `CombatState` events, and hands off to `CombatCameraController`.
- `CombatCameraController`: queued cinematic camera — immediate enter/exit between board and combat views, plus per-turn "focus on actor" and "focus on clash" cuts that play back-to-back without racing.
- **Note:** the combat stage is not an additively-loaded scene — it's a second staging area inside the same scene (offset in world space, see `ChessCombatStage` in `TestScene`), and the camera cuts to/from it. This differs from the original additive-scene plan in Section 2 below.

**Dev/Debug** — `SceneHelper` (camera POV teleports, reload, quit) wired to temp UI buttons in `TestScene`; TMP turn indicator and selected-piece label.

**Not started:** crit, element/aura/reaction system, cards/loadout system, multiplayer/server-authoritative layer, AI/opponent team selection (both sides currently pick manually via `PlayerDecisionProvider`), combat kit balancing (current numbers are placeholders).

---

## 1. Core Concept

- Played on a standard chess board (3D environment).
- Standard chess movement/rules apply for piece movement and legality (check/checkmate based on legal-move threat, not guaranteed capture).
- When a capture move is attempted, the game transitions to a separate combat scene (like enemy encounters in Honkai: Star Rail) instead of instantly resolving the capture.
- Capture outcome is **non-deterministic** — any piece can defeat any piece (a pawn can beat a queen), decided by the combat encounter.
- Long-term goal: multiplayer support (server-authoritative, since both chess and combat are turn-based).

---

## 2. Architecture Principles

- **Chess logic**, **combat logic**, and **rendering/scene transition** are separate systems communicating through a shared game state / events. This enables:
  - Unit testing rules without touching 3D scenes.
  - Clean multiplayer integration later (server holds canonical state; clients send move/skill requests, server validates + broadcasts).
- Combat currently plays out on a second staging area inside the same scene (offset in world space), with `CombatCameraController` cutting between the board view and the combat view. *(Originally planned as an additively-loaded scene — see Section 0. Revisit if load times or scene-size become a problem.)*
- Turn order in combat uses a discrete **Action Value (AV) queue** — `AV = BaseActionValue / SPD`, a priority queue rather than a fixed turn list or a continuously-filling gauge. Same effect as HSR's gauge (higher SPD acts more often) but deterministic/replayable, which matters for turn-order manipulation effects (e.g., Knight's SPD buff) and for server authority later. Implemented in `TurnOrderService`.

---

## 3. Capture / Combat Trigger Flow

1. Player selects a capture move on the board.
2. Board pauses; eligible allied pieces within a 3x3 area (centered on the attacker) are highlighted.
3. Player manually selects up to 4 additional pieces to join (max team size = 5, including the attacker).
4. Same process happens for the defender (their 3x3 area, their pick).
5. Combat scene runs (see Section 5).
6. Result:
   - Attacker wins → defender's piece is captured/removed from the board.
   - Defender wins → attacker's piece is captured/removed from the board.
   - Only the original targeted piece is ever removed — team members return to their original board squares regardless of outcome.
   - If the King is the piece that loses combat, the game ends immediately.

> **Current state:** Steps 1–6 are wired end-to-end — `CaptureTeamSelection` drives the highlight/pick UI via `BoardInputHandler`, and `BoardView.ResolveActiveSelection` hands the finished picks to `CaptureCombatResolver`/`CombatState`, which runs the full N-vs-N encounter with the 3D stage, camera, and UI, then calls back into `GameState.ResolveCapture`. Both attacker and defender team selection are currently manual (no AI opponent yet).

**Open questions (deferred):**
- Enemy AI / opposing player team selection — manual like the player, or auto-picked for now until AI exists?
- Does joining a team as support cost that piece its next board turn?

---

## 4. Piece Roles & Combat Kit

Every piece has: **basic attack + one skill + one ultimate**. Effects differ by role:

| Role | Basic Attack | Skill | Ultimate |
|---|---|---|---|
| Attack | Yes | Attack skill (single/AoE) | Attack ultimate |
| Support/Buff | Yes | Buff/heal (single/AoE) | Support ultimate |
| Defense | Yes | Shield/taunt (adjustable later) | Defensive ultimate |

> **Current state:** All six piece types have a working Basic/Skill/Ultimate kit via `DefaultCombatKits`, resolved through the generic `AbilityDefinition`/`AbilityEffect` system in `CombatState`. Numbers are placeholders, not balanced.

### Knight — turn manipulation
- Skill ("Spur"): increases a single ally's SPD via `SpeedChangeEffect`, which rescales their remaining AV through `TurnOrderService.ApplySpeedChange`.
- Ultimate ("Chavalry Charge"): advances all allies' turns via `AdvanceTurnEffect` → `TurnOrderService.AdvanceTurn(percent: 1.0)`, i.e. full HSR-style "Advance Forward."

### Other pieces (placeholder kits, to be rebalanced later)
- **Pawn**: cheap/spammy — AoE ultimate ("Promotion Strike"), low costs across the board.
- **Bishop**: AoE skill ("Diagonal Ray"), high-multiplier single-target ultimate ("Prism Beam").
- **Rook**: tanky — Skill/Ultimate are self/AoE heal placeholders ("Brace"/"Bulwark") standing in for a future Shield/Taunt effect.
- **Queen**: strongest all-rounder, highest-multiplier AoE ultimate ("Sovereign's Wrath").
- **King**: defensive/support-leaning (ally heal skill/ultimate); losing = game over, handled at the `GameState` level, not here.

---

## 5. Team Combat Rules

- Both attacker and defender bring a team (1–5 pieces) into the encounter.
- Team selection: **manual**, player picks from the highlighted 3x3-area allies.
- Combat plays out HSR-style (action-gauge turn order, basic attack/skill/ultimate per piece).
- Only the original attacker/defender piece can be captured as a result; team members are never removed from the board.

> **Current state:** Fully implemented — `CaptureTeamSelection` → `CaptureCombatResolver` → `CombatState.Setup(attackerTeam, defenderTeam, ...)` runs real N-vs-N encounters (1–5 per side), rendered via `CombatStageController`/`CombatView`, with each side's `SkillPointPool` persisting on `GameState` across the whole match.

---

## 6. Loadout System — "Cards"

Inspired by Genshin Impact's artifact system.

- Each piece has **5 fixed slots**, each tied to a specific suit:

| Slot | Suit | Main Stat |
|---|---|---|
| 1 | Heart | HP |
| 2 | Diamond | Elemental dmg% OR general stat% (atk/def/heal/buff) — rolled from a pool |
| 3 | Spade | Attack |
| 4 | Club | Stat% (atk/def/heal/buff) — rolled from a pool |
| 5 | Joker | Crit Rate / Crit DMG |

- Piece capacity limits how many slots are unlocked:
  - **Pawn**: 2 slots unlocked — fixed to Slot 1 (Heart) and Slot 3 (Spade). Cannot complete a 4pc set; 2pc max.
  - **Queen**: all 5 slots unlocked.
  - Other pieces: TBD when needed.
- **Suit vs Set are separate properties**: suit determines which slot a card fits; **Set** determines which bonus family it belongs to, independent of suit. This allows a "4pc Set" bonus using 4 cards of the same Set across 4 different suits/slots.
- Set bonuses:
  - **2pc**: moderate buff, from 2 cards of the same Set.
  - **4pc**: stronger buff, from 4 cards of the same Set (Joker slot free to be anything unless it's part of the set).
  - **2+2**: two separate 2pc bonuses can be active simultaneously from two different Sets.
- Crit formula (baseline, adjustable): `final damage = base damage + (base damage * crit damage%)`, triggered probabilistically based on crit rate%.
- Card acquisition method: **TBD**.
- Card sub-stats (secondary rolls): **TBD**.
- Actual Set bonus effects (what each Set does): **TBD**.

> Not started — no code yet. Crit is also not yet wired into `DamageEffect`.

---

## 7. Element System

Inspired by Genshin Impact's elemental reaction system. Names are chess-themed (placeholders, open to change).

| Element (internal) | Working Name |
|---|---|
| Fire | Gambit |
| Water | Tempo |
| Earth | Fortress |
| Wind | Blitz |
| Ice | Zugzwang |

### Aura / Tagging Rules
- A piece hit by an elemental attack gets tagged with that element for a fixed duration (in turns).
- Hit with a **different** element while tagged → triggers the corresponding reaction, consumes the tag.
- Hit with the **same** element while tagged → refreshes duration, no reaction.
- Untouched tag expires after its duration → clears with no reaction.
- Can a piece hold multiple simultaneous tags, and can reactions leave a residual tag behind? **TBD**.

### Reactions (working names)

| Combo | Effect | Working Name |
|---|---|---|
| Gambit + Tempo (Fire + Water) | Increased damage | Check |
| Gambit + Zugzwang (Fire + Ice) | Damage over time | Endgame |
| Blitz + any element | AoE damage of the combined element | En Passant |
| Zugzwang + Tempo (Ice + Water) | Freeze — target skips next turn | Stalemate |
| Fortress + any element | Elemental shield, applied to the next ally in turn order | Castling |

- Element assignment to pieces: randomized (mechanism TBD).
- Full complexity (all cross-element combos) can be scoped down if needed — core 5 reactions above are the baseline.

> Not started — no code yet. Note: `CombatUnit.IsFrozen` and `CombatState.AdvanceTurn`'s freeze-skip already exist as a placeholder for the **Stalemate** reaction specifically, ahead of the rest of the element system.

---

## 8. Multiplayer Considerations (future)

- Server-authoritative: server holds canonical board + combat state; clients send move/skill/team-selection requests; server validates and broadcasts results.
- Turn-based nature (both chess and combat) avoids real-time netcode complexity — no physics sync needed.
- Keep chess and combat state fully serializable/deterministic from the start to make this addition incremental rather than a rewrite.
- Team selection (manual pick) must also be a networked, server-validated action.

> The AV-queue design in `TurnOrderService` was chosen specifically to keep this door open (deterministic, replayable, no wall-clock dependency).

---

## 9. Reference / Inspiration

- **Archon: The Light and the Dark** (1983) — chess board + real-time combat on capture;
- **Honkai: Star Rail** — turn-based combat structure (action gauge, basic attack/skill/ultimate, turn advancement).
- **Genshin Impact** — artifact/set bonus system, elemental reaction system.

---

## 10. Status / Open Decisions Log

- [x] Wire `CaptureTeamSelection` into `CaptureCombatResolver`
- [x] Skills and ultimates for combat units (data-driven via `AbilityDefinition`/`AbilityEffect`)
- [ ] Combat scene/animation — currently an in-scene offset stage + camera cuts, not an additive scene; revisit if this becomes limiting
- [ ] Crit for combat units
- [ ] Team full-combatant balance (attack pieces vs support pieces contribution weighting) — current kit numbers are unbalanced placeholders
- [ ] Enemy/AI team selection logic
- [ ] Card acquisition method
- [ ] Card sub-stats
- [ ] Set bonus effects (per Set)
- [ ] Slot limits for pieces other than Pawn/Queen
- [ ] Elemental tag duration value, multi-tag handling, residual tags from reactions
- [ ] Element assignment mechanism (randomization rules)
- [ ] Piece stat baselines and progression/leveling system
- [ ] Board layout differences (if any) from standard chess

---