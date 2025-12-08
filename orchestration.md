Research scripts for orchestration plan

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingGameManagerMotif.cs#1-1), lines 1 to 500

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingSetupMotif.cs)

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/PlayerHealthMotif.cs#1-1), lines 1 to 250

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingHUDMotif.cs#1-1), lines 1 to 200

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingGameManagerMotif.cs#500-500), lines 500 to 560

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/PlayerHealthMotif.cs#250-250), lines 250 to 556

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingHUDMotif.cs#200-200), lines 200 to 302

Searched for text `ShootingGameManagerMotif`, 20 results

Searched for text `PlayerHealthMotif`, 20 results

Searched for files matching `**/Assets/Scripts/Shooting/*.cs`, 20 matches

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingGameConfigMotif.cs#1-1), lines 1 to 100

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingPlayerMotif.cs#1-1), lines 1 to 150

Read [](file:///c%3A/github/shooting/Assets/Scripts/Shooting/ShootingAudioMotif.cs#1-1), lines 1 to 100

Read [](file:///c%3A/github/shooting/ARCHITECTURE_AND_RESPONSIBILITIES.md)

Now I have the detailed research. Let me create the plan document. I don't have permission to create files. Here is the full **Orchestration Refactoring Plan** for you to copy into a new document:

---

## Plan: Orchestration Refactoring

Refactor `ShootingGameManagerMotif` from a God Object (does everything) into a Slim Orchestrator (coordinates state changes via events). This fixes race conditions, reduces coupling, and makes debugging easier.

---

### Steps

1.  **Create `GameStateEventBus.cs`** - A new static event hub that defines all game-related events (`OnGameStateChanged`, `OnRoundStarted`, `OnCountdownTick`, `OnRestartRequested`).
2.  **Create `GameStateUIHandler.cs`** - Move all `TextMeshProUGUI` status/countdown UI references out of `ShootingGameManagerMotif` into this new script that subscribes to the Event Bus.
3.  **Create `ScoreboardManagerMotif.cs`** - Extract scoreboard panel, score entry prefabs, and winner announcement logic into a dedicated component.
4.  **Create `GameInputHandler.cs`** - Move the restart gesture detection (dual-grip hold) out of the Game Manager's `Update()` loop into this dedicated input script.
5.  **Wire `ShootingAudioMotif` to Events** - Instead of the Game Manager calling audio RPCs directly, have `ShootingAudioMotif` subscribe to `OnRoundStarted`/`OnRoundEnded` events.
6.  **Slim down `ShootingGameManagerMotif`** - Remove all UI, Audio, and Input references. It should only contain the Networked state machine (`GameState`, `RemainingTime`) and fire events.

---

### Target Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    ORCHESTRATION LAYER                          │
├─────────────────────────────────────────────────────────────────┤
│  ShootingGameManagerMotif (Slim Orchestrator)                   │
│  ✅ Game state machine (Waiting, Countdown, Playing, RoundEnd)  │
│  ✅ Round timer (Networked)                                     │
│  ✅ Win condition checking                                      │
│  ✅ Fires events via GameStateEventBus                          │
│  ❌ NO direct UI manipulation                                   │
│  ❌ NO direct audio calls                                       │
│  ❌ NO input handling                                           │
└────────────────────────┬────────────────────────────────────────┘
                         │ Events (not direct calls)
         ┌───────────────┼───────────────┬───────────────┐
         ▼               ▼               ▼               ▼
┌─────────────┐  ┌───────────────┐  ┌──────────────┐  ┌────────────┐
│GameStateUI  │  │ScoreboardMgr  │  │GameInputHdlr │  │AudioMotif  │
│Handler      │  │               │  │              │  │            │
│- status text│  │- score entries│  │- grip restart│  │- round sfx │
│- countdown  │  │- winner text  │  │- fire events │  │            │
└─────────────┘  └───────────────┘  └──────────────┘  └────────────┘
```

---

### New Files to Create

| File | Responsibility |
|------|----------------|
| `GameStateEventBus.cs` | Static event hub. Defines: `OnGameStateChanged`, `OnRoundStarted`, `OnRoundEnded`, `OnCountdownTick`, `OnTimerUpdated`, `OnRestartRequested`. |
| `GameStateUIHandler.cs` | Subscribes to Event Bus. Updates status text and countdown display. |
| `ScoreboardManagerMotif.cs` | Subscribes to score events. Manages scoreboard panel and player entries. |
| `GameInputHandler.cs` | Detects restart gesture (dual-grip). Fires `OnRestartRequested`. |

---

### Task Checklist

**Phase 1: Event Bus (Foundation)**
- [ ] Create `GameStateEventBus.cs`
- [ ] Define all events

**Phase 2: Extract UI**
- [ ] Create `GameStateUIHandler.cs`
- [ ] Move status/countdown UI code from `ShootingGameManagerMotif`
- [ ] Subscribe to events

**Phase 3: Extract Scoreboard**
- [ ] Create `ScoreboardManagerMotif.cs`
- [ ] Move scoreboard panel logic

**Phase 4: Extract Input**
- [ ] Create `GameInputHandler.cs`
- [ ] Move restart gesture detection
- [ ] Game Manager subscribes to `OnRestartRequested`

**Phase 5: Wire Audio**
- [ ] Modify `ShootingAudioMotif` to subscribe to Event Bus
- [ ] Remove direct audio RPCs from Game Manager

**Phase 6: Final Cleanup**
- [ ] Remove all UI/Audio/Input fields from `ShootingGameManagerMotif`
- [ ] Verify Game Manager is under 200 lines
- [ ] Update ARCHITECTURE_AND_RESPONSIBILITIES.md

---

### Expected Benefits

1.  **No More Race Conditions:** Components subscribe to events; initialization order doesn't matter.
2.  **Easier Debugging:** If UI is broken, check `GameStateUIHandler`. If audio is broken, check `ShootingAudioMotif`.
3.  **Safer Changes:** Adding a new game state doesn't require touching 5 scripts.
4.  **Fewer Bugs:** Decoupled components mean changes in one area don't break others.

---

### Estimated Effort

| Phase | Time |
|-------|------|
| Phase 1: Event Bus | 1-2 hours |
| Phase 2: UI Handler | 2-3 hours |
| Phase 3: Scoreboard | 2-3 hours |
| Phase 4: Input Handler | 1 hour |
| Phase 5: Audio Wiring | 30 min |
| Phase 6: Cleanup | 1-2 hours |
| **Total** | **~8-12 hours** |

---
