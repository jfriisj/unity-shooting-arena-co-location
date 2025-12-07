---
agent: agent
---
# Guided Startup Flow Implementation

## Task Overview
Implement a **Guided Startup Modal System** that orchestrates the game initialization flow, clearly differentiating between Host and Client roles. The system should gate avatar spawning until all prerequisites are complete and provide clear visual feedback to users.

## Problem Statement
Currently, the game has race conditions where:
- Avatars spawn before network objects are ready
- SpawnManagerMotif may be null when AvatarSpawnerHandlerMotif tries to use it
- Clients may try to spawn before anchor alignment is complete
- Room scan status is not clearly communicated to users

## Architecture

### Flow Diagram
```
┌─────────────────────────────────────────────────────────────────┐
│                    STARTUP FLOW                                  │
├─────────────────────────────────────────────────────────────────┤
│  APP START                                                       │
│      ▼                                                           │
│  Platform Init (Oculus Platform)                                 │
│      ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ MODAL: "Initializing..."                                    ││
│  │ - Check for invite/deep link                                ││
│  │ - If invite → CLIENT flow                                   ││
│  │ - If no invite → HOST flow (first in room)                  ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌──────────────────────┐    ┌──────────────────────┐           │
│  │      HOST FLOW       │    │     CLIENT FLOW      │           │
│  ├──────────────────────┤    ├──────────────────────┤           │
│  │ 1. Check Room Scan   │    │ 1. Join Session      │           │
│  │    → Prompt if needed│    │    → "Connecting..." │           │
│  │ 2. Create/Join       │    │ 2. Wait for Anchor   │           │
│  │    Network Session   │    │    → "Waiting..."    │           │
│  │ 3. Create Anchor     │    │ 3. Localize Anchor   │           │
│  │    → Share with group│    │    → "Aligning..."   │           │
│  │ 4. Share Room Mesh   │    │ 4. Load Room Mesh    │           │
│  │    → via RoomSharing │    │    → "Loading room"  │           │
│  │ 5. READY             │    │ 5. READY             │           │
│  │    → Spawn avatar    │    │    → Spawn avatar    │           │
│  └──────────────────────┘    └──────────────────────┘           │
└─────────────────────────────────────────────────────────────────┘
```

## Components to Create

### 1. `GameStartupManagerMotif.cs`
**Location:** `Assets/Scripts/Startup/GameStartupManagerMotif.cs`

**Responsibilities:**
- Orchestrates the entire startup flow
- Determines Host vs Client role
- Gates avatar spawning until all prerequisites complete
- Fires events for UI updates
- Handles timeout and error recovery

**Key Properties:**
```csharp
public enum StartupState {
    Initializing,
    CheckingRoomScan,
    PromptingRoomScan,
    CreatingSession,
    JoiningSession,
    WaitingForAnchor,
    LocalizingAnchor,
    SharingRoom,
    LoadingRoom,
    Ready,
    Error
}

public bool IsHost { get; }
public StartupState CurrentState { get; }
public string StatusMessage { get; }
public float Progress { get; } // 0-1
public event Action<StartupState> OnStateChanged;
public event Action OnStartupComplete;
public event Action<string> OnError;
```

**Key Methods:**
```csharp
public void StartHostFlow();
public void StartClientFlow();
public void RetryCurrentStep();
public void RequestRoomScan();
```

### 2. `StartupModalUI.cs`
**Location:** `Assets/Scripts/Startup/StartupModalUI.cs`

**Responsibilities:**
- Displays modal panel during startup
- Shows current state and progress
- Displays error messages with retry options
- Hides when startup is complete

**UI Elements:**
- Title text (e.g., "Setting Up Game...")
- Status text (e.g., "Checking room scan...")
- Progress bar or spinner
- Step indicators (checkmarks for completed steps)
- Error panel with retry button
- Room scan button (for host if needed)

### 3. `StartupFlowConfig.cs` (ScriptableObject)
**Location:** `Assets/Scripts/Startup/StartupFlowConfig.cs`

**Configurable Settings:**
```csharp
[CreateAssetMenu(fileName = "StartupFlowConfig", menuName = "MRMotifs/Startup Flow Config")]
public class StartupFlowConfig : ScriptableObject
{
    public float stepTimeout = 30f;
    public float anchorWaitTimeout = 60f;
    public float roomLoadTimeout = 30f;
    public bool autoPromptRoomScan = true;
    public bool showDebugLogs = true;
}
```

### 4. Scene GameObject: `[MR Motif] Game Startup`
**Components:**
- `GameStartupManagerMotif`
- Reference to `StartupFlowConfig` asset

### 5. UI Prefab: `StartupModalCanvas`
**Location:** `Assets/Prefabs/UI/StartupModalCanvas.prefab`

**Structure:**
```
StartupModalCanvas (Canvas - Screen Space Overlay)
├── ModalPanel (Panel with dark background)
│   ├── TitleText ("Setting Up...")
│   ├── StatusText ("Checking room scan...")
│   ├── ProgressBar (Slider)
│   ├── StepsContainer
│   │   ├── Step1 (Icon + "Room Scan")
│   │   ├── Step2 (Icon + "Network")
│   │   ├── Step3 (Icon + "Alignment")
│   │   └── Step4 (Icon + "Ready")
│   ├── ErrorPanel (Hidden by default)
│   │   ├── ErrorText
│   │   └── RetryButton
│   └── ScanRoomButton (Hidden by default)
```

## Integration Points

### Modify Existing Components:

#### `AvatarSpawnerHandlerMotif.cs`
- Add check: Only enqueue for spawn if `GameStartupManagerMotif.CurrentState == Ready`
- Subscribe to `OnStartupComplete` event

#### `ShootingSetupMotif.cs`  
- Wait for startup complete before setting up shooting components

#### `ColocationManager` / `SharedSpatialAnchorManager`
- Expose events for anchor created/localized
- GameStartupManager subscribes to these

#### `RoomSharingMotif.cs`
- Expose events for room shared/loaded
- GameStartupManager subscribes to these

## Success Criteria

1. **No Race Conditions:** Avatar never spawns before all prerequisites are met
2. **Clear Feedback:** User always knows what's happening via modal UI
3. **Error Recovery:** Errors show clear messages with retry options
4. **Role Detection:** Correctly identifies Host vs Client
5. **Timeout Handling:** Steps timeout gracefully with error message
6. **Room Scan Flow:** Host is prompted to scan room if needed

## Implementation Order

1. Create `StartupFlowConfig` ScriptableObject
2. Create `GameStartupManagerMotif` with state machine
3. Create `StartupModalUI` and prefab
4. Add scene GameObject with components
5. Modify `AvatarSpawnerHandlerMotif` to gate on startup
6. Modify `ShootingSetupMotif` to gate on startup
7. Add events to `ColocationManager`/`RoomSharingMotif`
8. Test Host flow
9. Test Client flow
10. Test error scenarios

## Code Patterns to Follow

```csharp
// Follow project conventions:
// - m_ prefix for private fields
// - [MetaCodeSample("MRMotifs-SharedActivities")] attribute
// - #if FUSION2 directive for network code
// - Use FindAnyObjectByType<T>() for singleton discovery
// - Use async/await for async operations
// - Namespace: MRMotifs.SharedActivities.Startup
```

## Testing Scenarios

1. **Host - Fresh Start:** No room scan exists → prompts scan → creates session
2. **Host - Existing Scan:** Room scan exists → creates session immediately  
3. **Client - Via Invite:** Joins session → waits for anchor → localizes
4. **Host - Timeout:** Network creation times out → shows error
5. **Client - Host Leaves:** Host disconnects → shows error, allows rejoin

## Files to Create
- `Assets/Scripts/Startup/GameStartupManagerMotif.cs`
- `Assets/Scripts/Startup/StartupModalUI.cs`
- `Assets/Scripts/Startup/StartupFlowConfig.cs`
- `Assets/Prefabs/UI/StartupModalCanvas.prefab`
- `Assets/Resources/StartupFlowConfig.asset`

## Files to Modify
- `Assets/Scripts/Avatar/AvatarSpawnerHandlerMotif.cs`
- `Assets/Scripts/Shooting/ShootingSetupMotif.cs`
- `Assets/Scripts/Colocation/RoomSharingMotif.cs`
- `Assets/Scenes/ShootingGame.unity` (add startup GameObject)