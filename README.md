# Shooting Arena - Co-location Multiplayer Game

A Meta Quest mixed reality arena shooter where 2+ players in the same physical room can see and shoot each other. Built with Unity 6, Photon Fusion 2, and Meta XR SDK.

---

## 🎯 Project Goal

**A co-located arena shooting game where players in the same physical space share a coordinate system via spatial anchors and compete in fast-paced combat.**

### Design Philosophy
- **Open Play Area Mode**: Players spawn at their physical headset position (no teleportation needed)
- **Co-location First**: Shared spatial anchors ensure all players see the same virtual content in the same physical space
- **Minimal Scope**: Focus on core shooting mechanics before adding voice chat, power-ups, etc.

---

## ✅ Current Status: READY FOR RESEARCH DATA COLLECTION

The project is fully functional with all core systems and metrics collection verified on Quest devices.

| Area | Status |
|------|--------|
| **Compilation** | ✅ No errors |
| **Core Scripts** | ✅ All implemented |
| **Scene Setup** | ✅ Building blocks configured |
| **Networking** | ✅ Photon Fusion working |
| **Platform** | ✅ Oculus Platform initialized |
| **Device Testing** | ✅ Co-location verified |
| **Metrics Collection** | ✅ All 10 metrics validated (real data, no mocks) |
| **Calibration Tracking** | ✅ Fixed - both Host and Client track drift correctly |

---

## 📊 Metrics Collection System

### Verified Working (December 6, 2025)

All metrics are collected from real system/network APIs with no mocks or constants:

| Metric | Column | Data Source | Status |
|--------|--------|-------------|--------|
| Session ID | `session_id` | `DateTime.Now` timestamp | ✅ Real |
| Headset ID | `headset_id` | `SystemInfo.deviceUniqueIdentifier` hash | ✅ Real |
| Participants | `participant_count` | `NetworkRunner.SessionInfo.PlayerCount` | ✅ Real |
| Timestamp | `timestamp_sec` | `Time.time` since session start | ✅ Real |
| Frame Rate | `frame_rate_fps` | `1/Time.deltaTime` (0.5s average) | ✅ Real |
| Network Latency | `network_latency_ms` | `NetworkRunner.GetPlayerRtt()` | ✅ Real |
| Calibration Error | `calibration_error_mm` | `ColocationManager.ValidateCalibration()` | ✅ Real |
| Battery Temp | `battery_temp_c` | Android Intent API | ✅ Real |
| Battery Level | `battery_level` | `SystemInfo.batteryLevel` | ✅ Real |
| Scene State | `scene_state` | `NetworkRunner.IsSharedModeMasterClient` | ✅ Real |

### CSV Format
```
session_id,headset_id,participant_count,timestamp_sec,frame_rate_fps,network_latency_ms,calibration_error_mm,battery_temp_c,battery_level,scene_state
```

### File Location
- Path on device: `/sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/`
- Filename format: `session_YYYYMMDD_HHMMSS_<headset_id>.csv`

### Research Thresholds (from Literature)
- **Network Latency**: ≤75ms (Van Damme et al.)
- **Calibration Error**: <10mm (Reimer et al.)
- **Frame Rate**: ≥72 FPS (Quest 3 native)

### Quick Reference: ADB Commands

```bash
# ADB path (Windows)
ADB="/c/Users/jonfriis/Android/Sdk/platform-tools/adb.exe"

# Test devices
H1="2G0YC1ZF8B07WD"  # H_4193 (usually Client)
H2="2G0YC5ZF9F00N1"  # H_6444 (usually Host)

# List sessions
"$ADB" -s $H1 shell "ls -la /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/"

# Read latest session
"$ADB" -s $H1 shell "cat /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/<session_file>.csv"

# Pull all metrics locally
"$ADB" -s $H1 pull /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/ ./metrics_H1/
```

See `research-paper/data/collection guide.md` for complete protocol.

---

## 📝 Development Log

### Session: December 6, 2025 - MetricsLogger Improvements

#### Problem Solved: H2 Headset Showing "NotConnected"
The MetricsLogger on H2 was incorrectly reporting `scene_state=NotConnected` while H1 correctly showed `Client`. Root cause: `FindAnyObjectByType<NetworkRunner>()` was unreliable for detecting the NetworkRunner in certain initialization scenarios.

#### Solution Implemented
Updated `UpdateNetworkState()` in `MetricsLogger.cs` to use Photon Fusion's static `NetworkRunner.Instances` list:

```csharp
private void UpdateNetworkState()
{
    if (m_networkRunner == null || !m_networkRunner.IsRunning)
    {
        // Use NetworkRunner.Instances (Fusion's internal static list)
        foreach (var runner in NetworkRunner.Instances)
        {
            if (runner != null && runner.IsRunning)
            {
                m_networkRunner = runner;
                break;
            }
        }
        // Fallback if Instances is empty
        if (m_networkRunner == null)
        {
            m_networkRunner = FindAnyObjectByType<NetworkRunner>();
        }
    }
    // ... rest of method
}
```

#### Verification Results (Latest Session)
| Headset | Serial | Headset ID | Network State | Participant Count |
|---------|--------|------------|---------------|-------------------|
| H1 | `2G0YC1ZF8B07WD` | `H_4193` | Client ✅ | 2 ✅ |
| H2 | `2G0YC5ZF9F00N1` | `H_6444` | Host ✅ | 2 ✅ |

#### MetricsLogger CSV Format (10 Columns)
```
session_id,headset_id,participant_count,timestamp_sec,frame_rate_fps,network_latency_ms,calibration_error_mm,battery_temp_c,battery_level,scene_state
```

#### Metrics File Location
- Path on device: `/sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/`
- Filename format: `session_YYYYMMDD_HHMMSS_<headset_id>.csv`

#### Test Device Configuration
| Device | Serial Number | Headset ID | Role |
|--------|---------------|------------|------|
| Quest H1 | `2G0YC1ZF8B07WD` | `H_4193` | Usually Client |
| Quest H2 | `2G0YC5ZF9F00N1` | `H_6444` | Usually Host |

**ADB Path:** `C:/Users/jonfriis/Android/Sdk/platform-tools/adb.exe`

#### ADB Commands for Metrics Collection
```bash
# Set ADB path (Windows)
ADB="C:/Users/jonfriis/Android/Sdk/platform-tools/adb.exe"

# List connected devices
$ADB devices

# List available sessions on H1
$ADB -s 2G0YC1ZF8B07WD shell ls /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/

# List available sessions on H2
$ADB -s 2G0YC5ZF9F00N1 shell ls /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/

# Read a session file
$ADB -s <serial> shell cat /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/<filename>.csv

# Pull all metrics to local folder
$ADB -s <serial> pull /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/ ./
```

#### ✅ FIXED: Calibration Error Discrepancy (December 6, 2025)
**Issue:** H2 (Host) showed 0mm calibration error while H1 (Client) showed ~659mm.

**Root Cause:** 
1. Host never called `AlignUserToAnchor()` - they create the anchor but don't align to it
2. `ValidateCalibration()` measured distance from anchor position, not drift from initial position
3. The 659mm "error" was the meaningless distance between camera rig origin and world-space anchor position

**Fix Applied:**
1. Added `RegisterHostCalibration()` to `ColocationManager.cs` - called when host creates anchor
2. Both host and client now track drift from their reference position after calibration
3. `ValidateCalibration()` now measures horizontal drift only (ignores vertical for sit/stand)
4. Initial calibration error is 0mm for both roles (host defines origin, client aligns to it)

**Files Modified:**
- `Assets/Scripts/Colocation/ColocationManager.cs` - Added host calibration registration, fixed drift tracking
- `Assets/Scripts/Colocation/SharedSpatialAnchorManager.cs` - Calls `RegisterHostCalibration()` after anchor creation

**Expected Behavior After Fix:**
| Headset | Initial Error | Drift Tracking |
|---------|---------------|----------------|
| Host | 0mm | Tracks drift from anchor creation position |
| Client | 0mm | Tracks drift from alignment position |

**Verified Working (Session 20251206_210243):**
- H1 (Client): `calibration_error_mm = 0.00mm` ✅
- H2 (Host): `calibration_error_mm = 0.00mm` ✅

---

#### FPS Performance Observations
| Headset | Role | FPS Range | Notes |
|---------|------|-----------|-------|
| H1 | Client | 50-75 FPS | More variance, stabilizes to ~72 |
| H2 | Host | 70-77 FPS | Stable throughout |

**Analysis:** Client has slightly more variance likely due to network overhead processing remote player updates. Both devices achieve target 72 FPS during stable operation.

---

## 🚀 Next Steps for Future Development

### Completed ✅
1. ~~MetricsLogger network state detection~~ - Fixed using `NetworkRunner.Instances`
2. ~~Calibration error discrepancy~~ - Fixed with `RegisterHostCalibration()`
3. ~~Metrics validation~~ - All 10 metrics verified as real data sources
4. ~~Guided Startup Flow~~ - Implemented modal system to guide Host/Client initialization

### Ready for Research
- **Data Collection**: System is ready to collect research metrics
- **Guide**: See `research-paper/data/collection guide.md` for full protocol

### Future Enhancements (Optional)
1. **Drift Monitoring**: The calibration_error_mm will now track actual drift over time - useful for long session analysis
2. **Thermal Correlation**: Battery temperature data can be correlated with FPS drops
3. **Network Analysis**: Latency spikes can be analyzed against participant count changes

---

## 🏗️ DEFINITIVE: Co-located MR Architecture

This project uses Meta's co-location APIs combined with Photon Fusion 2. Understanding the correct building blocks is essential.

### Building Block Options (Choose ONE Approach)

Meta provides TWO independent approaches for co-located multiplayer. **DO NOT MIX THEM:**

#### Option A: Colocation Session + Space Sharing (THIS PROJECT)
Uses Meta's native Bluetooth/WiFi discovery - NO lobby/matchmaking needed.

| Step | Component | API | Responsibility |
|------|-----------|-----|----------------|
| 1 | **Platform Init** | `OVRPlatform.Initialize()` | Initialize Meta Platform SDK |
| 2 | **Room Scan (Host only)** | `MRUK.LoadSceneFromDevice()` | Load room mesh from device |
| 3 | **Advertise Session (Host)** | `OVRColocationSession.StartAdvertisementAsync(metadata)` | Bluetooth broadcast, returns `groupUuid` |
| 4 | **Start Photon Session** | `NetworkRunner.StartGame()` | Start networking AFTER colocation discovered |
| 5 | **Share Room (Host)** | `room.ShareRoomAsync(groupUuid)` | Share MRUK room via Space Sharing API |
| 6 | **Discover Session (Client)** | `OVRColocationSession.StartDiscoveryAsync()` | Listen for nearby hosts |
| 7 | **Load Shared Room (Client)** | `MRUK.LoadSceneFromSharedRooms(null, groupUuid, alignmentData)` | Load host's room mesh with alignment |

**Key Flow:**
```
Host: Platform Init → MRUK Scan → Advertise (get groupUuid) → Share Room → Start Photon
Client: Platform Init → Discover → Get groupUuid → Join Photon → Load Shared Room → Align
```

#### Option B: Photon Lobby + Shared Spatial Anchor (Alternative)
Uses Photon for matchmaking, single anchor for alignment.

| Step | Component | API | Responsibility |
|------|-----------|-----|----------------|
| 1 | **Platform Init** | `OVRPlatform.Initialize()` | Initialize Meta Platform SDK |
| 2 | **Start Photon with Lobby** | `StartGameArgs { CustomLobbyName = "X" }` | Publish session to lobby |
| 3 | **Create Anchor (Host)** | `OVRSpatialAnchor`, `anchor.SaveAnchorAsync()` | Create and save spatial anchor |
| 4 | **Share Anchor (Host)** | `anchor.ShareAsync(groupUuid)` | Share to group via Meta Cloud |
| 5 | **Load Anchor (Client)** | `OVRSpatialAnchor.LoadUnboundAnchorsAsync()` | Load shared anchor |
| 6 | **Align Camera Rig** | Transform math on OVRCameraRig | Align client to anchor position |

### ⚠️ CRITICAL: What NOT to Do

1. **DON'T use Colocation Discovery for matchmaking AND Photon Lobby together** - Pick ONE matchmaking method
2. **DON'T call `MRUK.LoadSceneFromDevice()` on Client** - Client loads from Space Sharing, not device
3. **DON'T skip the groupUuid** - This is what links Host and Client for anchor/room sharing
4. **DON'T expect room mesh without Space Sharing** - Without it, only Host has room collision

### Networked Variables Required for Space Sharing

```csharp
// Host sets these, Client reads them
[Networked] public NetworkString<_512> NetworkedRoomUuid { get; set; }
[Networked] public NetworkString<_256> NetworkedFloorPose { get; set; }
```

### Physics Collision Setup

For room mesh collision to work on ALL devices:
1. **Host:** MRUK spawns `EffectMesh` with `MeshCollider` from device scan
2. **Client:** `LoadSceneFromSharedRooms()` spawns same mesh with colliders from shared data
3. **Both devices:** Now have identical physics geometry

### Meta Documentation Links

- **[Multiplayer Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-multiplayer-blocks)** - Official guide for Auto/Custom/Local Matchmaking + Colocation blocks
- **[Colocation Discovery](https://developers.meta.com/horizon/documentation/unity/unity-colocation-discovery)** - `OVRColocationSession` API
- **[Space Sharing (MRUK)](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-space-sharing)** - Room mesh sharing
- **[Shared Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-shared-spatial-anchors)** - Group-based anchor sharing

---

## 🔄 Guided Startup Flow (Host vs Client)

The game implements a **Guided Startup Modal System** that ensures proper initialization order and prevents race conditions between networking, colocation, and avatar spawning.

### Why This Flow Exists

Previous issues included:
- `SpawnManagerMotif is null` - Avatar spawned before network objects were ready
- `Avatar was destroyed before setup completed` - Race conditions in initialization
- Calibration errors - Client attempted alignment before anchor was shared

The Guided Startup Flow solves these by **gating each step** and showing clear progress to the user.

### Startup States

```
Initializing → RoomScan → Networking → Colocation → RoomSharing → Ready
```

Each state must complete before advancing. If any step fails, an error is shown with retry option.

### Host Flow (First Player to Join)

```
┌────────────────────────────────────────────────────────────────┐
│                       HOST FLOW                                │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  1. INITIALIZING                                               │
│     └─ "Initializing platform..."                              │
│     └─ Wait for Oculus Platform SDK init                       │
│                                                                │
│  2. ROOM SCAN                                                  │
│     └─ "Checking room scan..."                                 │
│     ├─ If room exists → "Room found!" → Continue               │
│     └─ If no room → "Please scan your room" → Wait for scan    │
│                                                                │
│  3. NETWORKING                                                 │
│     └─ "Creating session..."                                   │
│     └─ Wait for NetworkRunner to spawn                         │
│     └─ "Session created!"                                      │
│                                                                │
│  4. COLOCATION                                                 │
│     └─ "Creating spatial anchor..."                            │
│     └─ SharedSpatialAnchorManager creates anchor               │
│     └─ ColocationManager.RegisterHostCalibration() called      │
│     └─ "Anchor ready!"                                         │
│                                                                │
│  5. ROOM SHARING (Optional)                                    │
│     └─ "Sharing room mesh..."                                  │
│     └─ RoomSharingMotif.ShareRoomAsync() if enabled            │
│     └─ "Room shared!"                                          │
│                                                                │
│  6. READY                                                      │
│     └─ "Ready to play!"                                        │
│     └─ Modal hides                                             │
│     └─ Avatar spawning is now permitted                        │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Client Flow (Joining Existing Session)

```
┌────────────────────────────────────────────────────────────────┐
│                       CLIENT FLOW                              │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  1. INITIALIZING                                               │
│     └─ "Initializing platform..."                              │
│     └─ Wait for Oculus Platform SDK init                       │
│                                                                │
│  2. ROOM SCAN                                                  │
│     └─ "Checking room scan..."                                 │
│     └─ Client needs their own room scan for collisions         │
│     └─ "Room found!"                                           │
│                                                                │
│  3. NETWORKING                                                 │
│     └─ "Joining session..."                                    │
│     └─ Wait for NetworkRunner to connect                       │
│     └─ "Connected to session!"                                 │
│                                                                │
│  4. COLOCATION                                                 │
│     └─ "Waiting for host anchor..."                            │
│     └─ SharedSpatialAnchorManager discovers session via BT     │
│     └─ "Anchor received!"                                      │
│     └─ "Aligning to host's space..."                           │
│     └─ ColocationManager.AlignUserToAnchor() called            │
│     └─ "Aligned!"                                              │
│                                                                │
│  5. ROOM SHARING (Optional)                                    │
│     └─ "Loading host's room mesh..."                           │
│     └─ RoomSharingMotif loads shared room                      │
│     └─ "Room loaded!"                                          │
│                                                                │
│  6. READY                                                      │
│     └─ "Ready to play!"                                        │
│     └─ Modal hides                                             │
│     └─ Avatar spawning is now permitted                        │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Key Differences: Host vs Client

| Step | Host | Client |
|------|------|--------|
| Room Scan | **Required** - Must scan room before creating anchor | **Required** - Needs local scan for physics/collisions |
| Networking | Creates session (IsSharedModeMasterClient = true) | Joins existing session |
| Colocation | Creates anchor, calls `RegisterHostCalibration()` | Discovers anchor, calls `AlignUserToAnchor()` |
| Room Sharing | Shares room mesh via MRUK | Loads shared room mesh from host |
| Timing | Must complete before client can join | Must wait for host at each step |

### Implementation Components

| Component | File | Responsibility |
|-----------|------|----------------|
| `GameStartupManagerMotif` | `Scripts/Startup/GameStartupManagerMotif.cs` | Orchestrates entire startup flow |
| `StartupModalUI` | `Scripts/Startup/StartupModalUI.cs` | UI display (status, progress, errors) |
| `StartupFlowConfig` | `Scripts/Startup/StartupFlowConfig.cs` | ScriptableObject for timeout/config |
| `StartupState` enum | `Scripts/Startup/GameStartupManagerMotif.cs` | State machine states |

### Events and Integration Points

The startup system integrates with existing components via events:

```csharp
// ColocationManager
public event Action OnAlignmentComplete;      // Fired when client aligns to anchor
public bool IsAligned { get; }                // True after alignment

// RoomSharingMotif  
public event Action OnRoomShared;             // Fired when host shares room
public event Action OnRoomLoaded;             // Fired when client loads room
public event Action<string> OnRoomSharingFailed;  // Fired on error

// SharedSpatialAnchorManager
public event Action<Guid> OnColocationSessionEstablished;  // Fired when anchor is ready
public bool IsColocationEstablished { get; }               // True after anchor setup
```

### Avatar Spawning Gate

`AvatarSpawnerHandlerMotif` now waits for startup to complete:

```csharp
// Before spawning avatar
if (m_startupManager != null)
{
    while (!m_startupManager.IsStartupComplete)
    {
        if (m_startupManager.CurrentState == StartupState.Error)
        {
            yield break; // Abort spawn on error
        }
        yield return new WaitForSeconds(0.5f);
    }
}
// Now safe to spawn avatar
```

### Timeout Configuration

Default timeouts (configurable via `StartupFlowConfig`):

| Step | Default Timeout | Notes |
|------|-----------------|-------|
| Room Scan | 30s | User may need to scan |
| Networking | 15s | Session creation/join |
| Colocation | 45s | Anchor discovery via Bluetooth |
| Room Sharing | 30s | MRUK room mesh transfer |

### Error Handling

When a step fails:
1. Modal shows error message in red
2. "Retry" button appears
3. User can retry the failed step
4. If skip is allowed (room sharing), continues without that feature

### Testing the Flow

1. **Host Test**: Start app on first device, watch modal progress through all steps
2. **Client Test**: Start app on second device, verify it waits for host anchor
3. **Timeout Test**: Disable WiFi/Bluetooth and verify timeout messages appear
4. **Retry Test**: Force a failure and verify retry button works

---

## 🔌 Networking Architecture (Simplified FusionBootstrap Approach)

**Updated December 2024** - The networking layer uses a simplified approach based on Meta's official MRMotifs samples.

### Key Pattern: FusionBootstrap + FusionBBEvents

Instead of manually creating NetworkRunner and calling `StartGame()`, we use:

1. **FusionBootstrap** (Building Block) - Handles session creation/joining
2. **FusionBBEvents** (Static Callbacks) - Provides connection state notifications
3. **SessionDiscoveryManager** (Wrapper) - Simplified API for game code

### Session Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    SIMPLIFIED SESSION CREATION                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Host:                                                           │
│    SessionDiscoveryManager.StartAsHost()                        │
│      └─ FusionBootstrap.DefaultRoomName = "ShootingGame_XXXX"  │
│      └─ FusionBootstrap.StartSharedClient()                     │
│      └─ FusionBBEvents.OnConnectedToServer fires               │
│      └─ IsConnected = true                                       │
│                                                                  │
│  Client:                                                         │
│    SessionDiscoveryManager.JoinSession("ShootingGame_XXXX")     │
│      └─ FusionBootstrap.DefaultRoomName = sessionName           │
│      └─ FusionBootstrap.StartSharedClient()                     │
│      └─ FusionBBEvents.OnConnectedToServer fires               │
│      └─ IsConnected = true                                       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### SessionDiscoveryManager Public API

```csharp
// Start as Host (generates unique session name)
public void StartAsHost(string customSessionName = null);

// Join existing session by name
public void JoinSession(string sessionName);

// Disconnect from current session
public async void Disconnect();

// Properties (polled by GameStartupManagerMotif)
public bool IsConnecting { get; }      // True while connecting
public bool IsConnected { get; }       // True when connected
public bool IsHost { get; }            // True if session host
public string CurrentSessionName { get; }
public NetworkRunner Runner { get; }

// Events
public event Action<NetworkRunner> OnConnected;
public event Action<string> OnConnectionFailed;
public event Action OnDisconnected;
```

### FusionBBEvents Subscriptions

The SessionDiscoveryManager subscribes to these FusionBBEvents:

| Event | Handler | Updates |
|-------|---------|---------|
| `OnConnectedToServer` | `HandleConnectedToServer` | Sets IsConnected=true, stores Runner |
| `OnDisconnectedFromServer` | `HandleDisconnectedFromServer` | Resets state |
| `OnConnectFailed` | `HandleConnectFailed` | Fires OnConnectionFailed event |
| `OnPlayerJoined` | `HandlePlayerJoined` | Logs player join |
| `OnPlayerLeft` | `HandlePlayerLeft` | Logs player leave |
| `OnShutdown` | `HandleShutdown` | Resets state |

### GameStartupManagerMotif Polling

Instead of awaiting async Tasks, the startup manager polls the SessionDiscoveryManager:

```csharp
// Wait for connection with timeout
while (m_sessionDiscovery.IsConnecting && taskElapsed < taskTimeout)
{
    yield return new WaitForSeconds(0.5f);
    taskElapsed += 0.5f;
    m_statusMessage = $"Creating game session... ({taskElapsed:F0}s)";
    UpdateUI();
}

if (m_sessionDiscovery.IsConnected)
{
    m_networkRunner = m_sessionDiscovery.Runner;
    // Continue to next step...
}
```

### Why This Approach?

| Aspect | Old Approach | New Approach |
|--------|--------------|--------------|
| Session Creation | Manual `NetworkRunner.StartGame()` | `FusionBootstrap.StartSharedClient()` |
| Connection Status | Awaiting async Task | Polling `IsConnecting`/`IsConnected` |
| Callbacks | `INetworkRunnerCallbacks` interface | `FusionBBEvents` static events |
| Error Handling | Task exception handling | Event-based (`OnConnectFailed`) |
| Complexity | ~360 lines | ~150 lines |
| Reliability | Timeout issues on device | Matches Meta's tested patterns |

### Session Discovery Note

The simplified approach uses **manual session name entry** instead of automatic lobby refresh. Clients must know the session name from the host (shared verbally or via text).

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Avatar/                 # Avatar handling
│   │   ├── AvatarMovementHandlerMotif.cs    # Position sync via object-of-interest
│   │   ├── AvatarNameTagHandlerMotif.cs     # Player name tags above heads
│   │   └── AvatarSpawnerHandlerMotif.cs     # Avatar spawn handling (integrates with startup)
│   ├── Shooting/               # Core shooting mechanics
│   │   ├── BoundaryDisablerMotif.cs         # Guardian suppression for free movement
│   │   ├── BulletMotif.cs                   # Networked projectile with physics
│   │   ├── CoverSpawnerMotif.cs             # Spawns cover objects in play area
│   │   ├── NetworkedCoverMotif.cs           # Networked cover object behavior
│   │   ├── PlayerHealthMotif.cs             # Health, damage, death, respawn
│   │   ├── PracticeModeMotif.cs             # Single-player practice with AI targets
│   │   ├── ShootingAudioMotif.cs            # Game audio (round start, end, countdown)
│   │   ├── ShootingDebugVisualizerMotif.cs  # Debug visualization (spawn points, boundaries)
│   │   ├── ShootingGameConfigMotif.cs       # Centralized game configuration
│   │   ├── ShootingGameManagerMotif.cs      # Game state machine, scoring
│   │   ├── ShootingHUDMotif.cs              # Health bar, kills, death panel
│   │   ├── ShootingPlayerMotif.cs           # Trigger input, bullet spawning
│   │   └── ShootingSetupMotif.cs            # Attaches shooting to avatars
│   ├── Spawning/               # Spawn system
│   │   └── SpawnManagerMotif.cs             # Open play area spawning
│   ├── Startup/                # Guided startup flow system
│   │   ├── GameStartupManagerMotif.cs       # Orchestrates Host/Client initialization
│   │   ├── RoleSelectionModalUI.cs          # Host/Client role selection UI
│   │   ├── SessionDiscoveryManager.cs       # Simplified FusionBootstrap wrapper
│   │   ├── StartupModalUI.cs                # Modal UI (status, progress, errors)
│   │   ├── StartupFlowConfig.cs             # ScriptableObject for timeout config
│   │   └── Editor/
│   │       └── StartupFlowSetup.cs          # Editor utility for UI setup
│   ├── Colocation/             # Co-location system
│   │   ├── ColocationManager.cs             # Camera rig alignment + calibration tracking
│   │   ├── RoomSharingMotif.cs              # Room mesh sharing (experimental, disabled)
│   │   ├── RoomScanManager.cs               # Room scan validation
│   │   └── SharedSpatialAnchorManager.cs    # Anchor creation/sharing (3 modes)
│   ├── Network/                # Networking utilities
│   │   └── HostMigrationHandlerMotif.cs     # Seamless host migration (disabled)
│   ├── Platform/               # Quest platform integration
│   │   ├── GroupPresenceAndInviteHandlerMotif.cs  # Group presence
│   │   └── InvitationAcceptanceHandlerMotif.cs    # Deep link invite handling
│   └── Shared/                 # Shared utilities
│       ├── Metrics/            # Research metrics collection
│       │   ├── CalibrationAccuracyTracker.cs # Spatial drift monitoring
│       │   ├── MetricsLogger.cs             # CSV logging (10 metrics @ 1Hz)
│       │   └── NetworkLatencyTracker.cs     # Network latency tracking
│       └── HandleAnimationMotif.cs
├── Prefabs/
│   ├── Shooting/
│   │   └── BulletMotif.prefab               # Networked bullet prefab
│   ├── Colocation/                          # Colocation-related prefabs
│   ├── NetworkedRigMotif.prefab             # Networked camera rig
│   ├── FusionAvatarSdk28PlusNoLegs.prefab   # Meta Avatar prefab
│   └── ScoreEntryMotif.prefab               # Scoreboard entry UI
├── Scenes/
│   └── ShootingGame.unity                   # Main game scene
└── Resources/
    └── OculusPlatformSettings.asset         # Platform configuration

research-paper/
├── data/
│   ├── collection guide.md                  # Metrics collection protocol
│   └── sessions/                            # Extracted session data
└── scripts/
    └── analyze_metrics.py                   # Python analysis scripts
```

---

## 🎮 Scene Structure (`ShootingGame.unity`)

### Active GameObjects
| GameObject | Purpose | Status |
|------------|---------|--------|
| `Directional Light` | Scene lighting | ✅ Active |
| `[BuildingBlock] Camera Rig` | OVRCameraRig with tracking | ✅ Active |
| `  └─ [MR Motif] Arena` | Reference point for avatar sync (child of Camera Rig) | ✅ Active |
| `[BuildingBlock] Passthrough` | MR passthrough layer | ✅ Active |
| `[BuildingBlock] Network Manager` | Fusion NetworkRunner (Shared Mode) | ✅ Active |
| `[BuildingBlock] Auto Matchmaking` | Auto session join for same-room play | ✅ Active |
| `[BuildingBlock] Platform Init` | Oculus Platform initialization | ✅ Active |
| `[BuildingBlock] Networked Avatar` | Meta Avatar spawning | ✅ Active |
| `[BuildingBlock] MR Utility Kit` | MRUK room scanning | ✅ Active |
| `[BuildingBlock] Colocation` | Colocation building block | ✅ Active |
| `[MR Motif] Game Startup Manager` | Guided startup flow orchestration | ✅ Active |
| `[MR Motif] Shooting Game Manager` | Game state, rounds, scoring | ✅ Active |
| `[MR Motif] Spawn Manager` | Open play area spawning logic | ✅ Active |
| `[MR Motif] Avatar Spawner Handler` | Avatar spawn event handling | ✅ Active |
| `[MR Motif] Shooting Setup` | Attaches shooting components to avatars | ✅ Active |
| `[MR Motif] Group Presence` | Meta Platform group presence | ✅ Active |
| `[MR Motif] Shooting HUD Canvas` | Player HUD (health, kills, death panel) | ✅ Active |
| `[MR Motifs] Colocation Manager` | Anchor alignment + calibration tracking | ✅ Active |
| `[MR Motifs] SSA Manager` | Shared Spatial Anchor management | ✅ Active |
| `[MR Motif] Practice Mode` | Single-player practice with AI targets | ✅ Active |
| `[MR Motif] Cover Spawner` | Spawns cover objects in play area | ✅ Active |
| `[MR Motif] Metrics Logger` | Research metrics CSV logging @ 1Hz | ✅ Active |
| `[MR Motif] Network Metrics` | Network performance metrics | ✅ Active |

### Disabled GameObjects
| GameObject | Purpose | Reason Disabled |
|------------|---------|-----------------|
| `[BuildingBlock] Scene Mesh` | Room mesh visualization | Conflicts with colocation flow |
| `[BuildingBlock] Scene Debugger` | Debug UI tool | Development only |
| `VoiceLogger` (x2) | Voice chat logging | Voice not in MVP |
| `[MR Motif] Room Sharing` | Room mesh sharing | Experimental feature |

---

## 🏗️ Detailed Scene Object & Component Responsibilities

This section documents every significant GameObject and its components to prevent duplicate responsibilities and configuration errors.

> ⚠️ **CRITICAL RULES TO PREVENT DUPLICATION:**
> 1. **Never add a component if it already exists elsewhere in the scene**
> 2. **NetworkBehaviour components REQUIRE a NetworkObject on the same GameObject**
> 3. **Building Blocks are self-contained - don't add custom NetworkBehaviours to them**
> 4. **Each responsibility should live in ONE place only**

---

### Meta Building Blocks (Do NOT Modify)

These are Meta SDK provided building blocks. They are self-contained systems and should not have custom components added to them.

#### `[BuildingBlock] Camera Rig`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `OVRCameraRig` | Unity | Main XR camera rig, tracking space origin |
| `OVRManager` | Unity | XR system settings (tracking, passthrough, etc.) |
| `OVRHeadsetEmulator` | Unity | Editor testing without headset |
| `BoundaryDisablerMotif` | MRMotifs | Suppresses Guardian boundaries for free movement |

**Child: `[MR Motif] Arena`** - Empty transform used as "object of interest" for avatar position sync.

---

#### `[BuildingBlock] Passthrough`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `OVRPassthroughLayer` | Unity | Enables MR passthrough rendering |

**Single Responsibility:** Passthrough rendering ONLY. No game logic.

---

#### `[BuildingBlock] Network Manager`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `NetworkRunner` | Fusion | Core networking (Shared Mode) |
| `NetworkEvents` | Fusion | Network event callbacks |
| `FusionBBEvents` | Meta.XR.MultiplayerBlocks.Fusion | Building block event integration |
| `CustomNetworkObjectProvider` | Meta.XR.MultiplayerBlocks.Fusion | Network object instantiation |
| `FusionVoiceClient` | Photon.Voice.Fusion | **[DISABLED]** Voice chat (not MVP) |
| `RunnerEnableVisibility` | Fusion | Runner visibility management |
| `HostMigrationHandlerMotif` | MRMotifs | **[DISABLED]** Seamless host takeover |
| `NetworkLatencyTracker` | MRMotifs | **[DISABLED]** Performance metrics |

**Single Responsibility:** Network session management. No game state logic.

---

#### `[BuildingBlock] Auto Matchmaking`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `FusionBootstrap` | Fusion | Auto-start network session |
| `FusionBootstrapDebugGUI` | Fusion | Debug UI in editor |

**Single Responsibility:** Session creation/joining ONLY.

---

#### `[BuildingBlock] Platform Init`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `PlatformInit_` | Meta.XR.MultiplayerBlocks.Shared | Initializes Oculus Platform SDK |

**Single Responsibility:** Platform SDK initialization ONLY.

---

#### `[BuildingBlock] Networked Avatar`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `AvatarSpawnerFusion` | Meta.XR.MultiplayerBlocks.Fusion | Spawns Meta Avatars for networked players |

**Child: `AvatarSDK`** - Contains `OvrAvatarManager`, `AvatarLODManager`, `GpuSkinningConfiguration`, `SampleInputManager`
**Child: `LipSyncInput`** - Contains audio/lip sync components

**Single Responsibility:** Avatar instantiation ONLY. No game logic on avatars.

---

#### `[BuildingBlock] MR Utility Kit`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `MRUK` | Meta.XR.MRUtilityKit | Room scanning, spatial awareness, scene mesh |

**Single Responsibility:** Room/environment awareness ONLY.

---

#### `[BuildingBlock] Colocation` ⚠️ CRITICAL
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `ColocationController` | Meta.XR.MultiplayerBlocks.Shared | Meta SDK colocation orchestration |
| `ColocationSessionEventHandler` | Meta.XR.MultiplayerBlocks.Shared | Colocation event callbacks |
| `FusionMessenger` | Meta.XR.MultiplayerBlocks.Colocation.Fusion | Fusion message passing for colocation |
| `FusionNetworkData` | Meta.XR.MultiplayerBlocks.Colocation.Fusion | Network data for colocation |
| `SharedSpatialAnchorCore` | Meta.XR.BuildingBlocks | Core spatial anchor functionality |
| `NetworkObject` | Fusion | **REQUIRED** for FusionMessenger/FusionNetworkData |
| `RoomSharingMotif` | MRMotifs | **[DISABLED]** Room mesh sharing (experimental) |

> ⚠️ **WARNING:** This building block MUST have a `NetworkObject` because it contains `FusionMessenger` and `FusionNetworkData` which are `NetworkBehaviour` classes. Without NetworkObject, Fusion will not spawn these correctly and colocation will fail silently.

> ⚠️ **DO NOT** add custom NetworkBehaviours here. Use separate MR Motif GameObjects.

---

#### `[BuildingBlock] Scene Mesh` (Currently Disabled)
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `RoomMeshController` | Meta.XR.BuildingBlocks | Controls scene mesh loading/display |
| `RoomMeshEvent` | Meta.XR.BuildingBlocks | Scene mesh load events |

**Status:** **DISABLED** - Causes issues with colocation. Re-enable only after colocation works.

---

### MR Motif GameObjects (Custom Game Logic)

These are custom game components. Each should have ONE clear responsibility.

#### `[MR Motif] Game Startup Manager`
| Component | Namespace | Responsibility | NetworkObject? |
|-----------|-----------|----------------|----------------|
| `GameStartupManagerMotif` | MRMotifs.SharedActivities.Startup | Orchestrates Host/Client initialization flow | MonoBehaviour |

**Single Responsibility:** Guided startup flow orchestration.

**Responsibilities:**
- ✅ Gate each initialization step (Room Scan → Networking → Colocation → Room Sharing)
- ✅ Show modal UI with progress and status updates
- ✅ Differentiate Host vs Client flows
- ✅ Provide `IsStartupComplete` flag for other systems to wait on
- ✅ Handle errors with retry capability
- ❌ NOT responsible for: actual initialization (delegates to RoomScanManager, ColocationManager, etc.)

**Key Properties:**
- `IsStartupComplete` - True when all steps are done and avatar can spawn
- `CurrentState` - Current `StartupState` enum value
- `IsHost` - True if this player is the session host

**Configuration:**
- Uses `StartupFlowConfig` ScriptableObject for timeouts
- `m_enableRoomSharing` - Toggle room sharing step on/off

---

#### `[MR Motif] Shooting Game Manager`
| Component | Namespace | Responsibility | NetworkObject? |
|-----------|-----------|----------------|----------------|
| `NetworkObject` | Fusion | Enables networking | ✅ REQUIRED |
| `ShootingGameManagerMotif` | MRMotifs | Game state machine (Waiting/Countdown/Playing/RoundEnd), scoring, win conditions | NetworkBehaviour |
| `ShootingAudioMotif` | MRMotifs | Game audio (round start, round end, countdown) | MonoBehaviour |
| `ShootingGameConfigMotif` | MRMotifs | Centralized game configuration (rounds, scoring) | MonoBehaviour |
| `ShootingDebugVisualizerMotif` | MRMotifs | Debug visualization (spawn points, boundaries) | MonoBehaviour |

**Responsibilities:**
- ✅ Game state management
- ✅ Round timing and scoring
- ✅ Win condition detection
- ✅ Audio feedback for game events
- ❌ NOT responsible for: player health, bullet spawning, avatar management

---

#### `[MR Motif] Spawn Manager`
| Component | Namespace | Responsibility | NetworkObject? |
|-----------|-----------|----------------|----------------|
| `NetworkObject` | Fusion | Enables networking | ✅ REQUIRED |
| `SpawnManagerMotif` | MRMotifs | Open play area spawning (headset position = spawn position) | NetworkBehaviour |

**Single Responsibility:** Spawn location calculation ONLY.

**Responsibilities:**
- ✅ Determine spawn locations
- ✅ Respawn positioning
- ❌ NOT responsible for: avatar instantiation, game state

---

#### `[MR Motif] Avatar Spawner Handler`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `AvatarSpawnerHandlerMotif` | MRMotifs | Handles avatar spawn events, bridges avatar system to game |

**Single Responsibility:** Avatar spawn event handling.

**Responsibilities:**
- ✅ React to avatar spawn events
- ✅ Coordinate with game systems when avatar spawns
- ❌ NOT responsible for: avatar instantiation (handled by Building Block)

---

#### `[MR Motif] Shooting Setup`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `ShootingSetupMotif` | MRMotifs | Attaches shooting components (`ShootingPlayerMotif`, `PlayerHealthMotif`) to spawned avatars |

**Single Responsibility:** Component attachment to avatars.

**Responsibilities:**
- ✅ Listen for avatar spawn via `AvatarEntity.OnSpawned`
- ✅ Add `ShootingPlayerMotif` for weapon/bullet spawning
- ✅ Add `PlayerHealthMotif` for health management
- ✅ Wire up prefab references (bullet, weapon)
- ❌ NOT responsible for: game state, HUD, scoring

---

#### `[MR Motif] Group Presence`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `GroupPresenceAndInviteHandlerMotif` | MRMotifs | Meta Platform group presence, invite handling |

**Single Responsibility:** Social/invite features.

**Responsibilities:**
- ✅ Group presence API
- ✅ Invite sending/receiving
- ❌ NOT responsible for: networking, game state

---

#### `[MR Motif] Shooting HUD Canvas`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `Canvas` | Unity | UI canvas for HUD |
| `OVROverlayCanvas` | Unity | Quest overlay rendering |
| `ShootingHUDMotif` | MRMotifs | Health display, kill counter, death panel, hit markers |

**Single Responsibility:** Player HUD visualization.

**Responsibilities:**
- ✅ Display player health
- ✅ Display kills/deaths
- ✅ Death panel with respawn countdown
- ✅ Hit markers and damage indicators
- ❌ NOT responsible for: health calculation, game state

---

#### `[MR Motifs] Colocation Manager`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `ColocationManager` | MRMotifs | Aligns camera rig to shared spatial anchor, tracks calibration drift |

**Single Responsibility:** Camera rig alignment and calibration tracking.

**Public API:**
- `RegisterHostCalibration(Vector3 anchorPosition)` - Called by host when creating anchor
- `AlignUserToAnchor(OVRSpatialAnchor anchor)` - Called by client to align to shared anchor
- `ValidateCalibration()` - Returns current drift in mm (horizontal only)
- `GetCurrentCalibrationError()` - Returns cached calibration error
- `IsCalibrated()` - Returns true if calibration has been performed
- `IsHost()` - Returns true if this user created the anchor

**Responsibilities:**
- ✅ `RegisterHostCalibration()` - stores reference position for host
- ✅ `AlignUserToAnchor()` - positions camera rig relative to anchor
- ✅ `ValidateCalibration()` - measures horizontal drift from reference position
- ✅ Track calibration state for both Host and Client roles
- ❌ NOT responsible for: anchor creation, anchor discovery, anchor sharing

> ⚠️ **NOTE:** This is a MonoBehaviour, NOT a NetworkBehaviour. It does not need a NetworkObject.

---

#### `[MR Motifs] SSA Manager` (Shared Spatial Anchor Manager)
| Component | Namespace | Responsibility | NetworkObject? |
|-----------|-----------|----------------|----------------|
| `NetworkObject` | Fusion | Enables networking | ✅ REQUIRED |
| `SharedSpatialAnchorManager` | MRMotifs | Anchor creation, advertisement, discovery, sharing | NetworkBehaviour |
| `NetworkObjectPrefabData` | Fusion | Prefab data for network spawning | |

**Single Responsibility:** Spatial anchor lifecycle management.

**Anchor Placement Modes:**
- `AtOrigin` - Anchor at Vector3.zero (original behavior)
- `AtHostPosition` - Anchor at host's current headset position (default)
- `ManualPlacement` - Host presses trigger to confirm anchor location

**Responsibilities:**
- ✅ **Host:** Create spatial anchor at configured position
- ✅ **Host:** Call `ColocationManager.RegisterHostCalibration()` after anchor creation
- ✅ **Host:** Advertise colocation session via `OVRColocationSession`
- ✅ **Client:** Discover nearby sessions via Bluetooth
- ✅ **Client:** Load and localize to shared anchor
- ✅ **Client:** Call `ColocationManager.AlignUserToAnchor()` after localization
- ❌ NOT responsible for: room mesh sharing, camera rig manipulation

---

#### `[MR Motif] Practice Mode`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `PracticeModeMotif` | MRMotifs | Single-player practice with AI targets |

**Status:** ✅ ENABLED - Available for solo practice sessions.

**Responsibilities:**
- ✅ Spawn AI target dummies for practice
- ✅ Provide solo warm-up mode
- ❌ NOT responsible for: multiplayer game state, scoring

---

#### `[MR Motif] Cover Spawner`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `CoverSpawnerMotif` | MRMotifs | Spawns cover objects in play area |

**Status:** ✅ ENABLED - Provides dynamic cover during gameplay.

**Responsibilities:**
- ✅ Create cover objects for players to hide behind
- ✅ Works with `NetworkedCoverMotif` for networked cover
- ❌ NOT responsible for: bullet physics, game state

---

#### `[MR Motif] Network Metrics`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `NetworkLatencyTracker` | MRMotifs | Tracks network performance metrics |

**Status:** ✅ ENABLED - Monitors network health for research.

**Responsibilities:**
- ✅ Track RTT (round-trip time) per player
- ✅ Monitor packet loss
- ✅ Feed data to MetricsLogger
- ❌ NOT responsible for: game logic, display

---

#### `[MR Motif] Metrics Logger`
| Component | Namespace | Responsibility |
|-----------|-----------|----------------|
| `MetricsLogger` | MRMotifs | Research metrics collection (CSV logging @ 1Hz) |
| `CalibrationAccuracyTracker` | MRMotifs | Calibration drift monitoring via ColocationManager |

**Status:** ✅ ENABLED - Collecting research metrics for co-location study.

**Metrics Collected (all real data, no mocks):**
| Metric | Source |
|--------|--------|
| `session_id` | Auto-generated timestamp |
| `headset_id` | Device hash (H_XXXX format) |
| `participant_count` | `NetworkRunner.SessionInfo.PlayerCount` |
| `timestamp_sec` | Unity `Time.time` since session start |
| `frame_rate_fps` | `1/Time.deltaTime` (0.5s rolling average) |
| `network_latency_ms` | `NetworkRunner.GetPlayerRtt()` |
| `calibration_error_mm` | `ColocationManager.ValidateCalibration()` |
| `battery_temp_c` | Android Intent API |
| `battery_level` | `SystemInfo.batteryLevel` |
| `scene_state` | Host/Client/NotConnected |

**Key Implementation Details:**
- Uses `NetworkRunner.Instances` for reliable runner detection (not `FindAnyObjectByType`)
- Auto-saves every 60 seconds + on app pause/quit
- Data persists even if app crashes (up to last 60s)

**CSV Output:** `/sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/`

---

### Component Responsibility Matrix

| Responsibility | Owner Component | Location |
|----------------|-----------------|----------|
| **Networking** | | |
| Session management | `NetworkRunner` | [BuildingBlock] Network Manager |
| Session creation (FusionBootstrap) | `FusionBootstrap` | [BuildingBlock] Auto Matchmaking |
| Session state wrapper | `SessionDiscoveryManager` | [MR Motif] Game Startup Manager |
| Connection callbacks | `FusionBBEvents` | Static events (subscribed by SessionDiscoveryManager) |
| Host migration | `HostMigrationHandlerMotif` | [BuildingBlock] Network Manager *(disabled)* |
| **Colocation** | | |
| Colocation orchestration | `ColocationController` | [BuildingBlock] Colocation |
| Anchor creation/discovery | `SharedSpatialAnchorManager` | [MR Motifs] SSA Manager |
| Camera rig alignment | `ColocationManager` | [MR Motifs] Colocation Manager |
| Room mesh sharing | `RoomSharingMotif` | [MR Motif] Room Sharing *(disabled)* |
| **Game State** | | |
| State machine | `ShootingGameManagerMotif` | [MR Motif] Shooting Game Manager |
| Scoring | `ShootingGameManagerMotif` | [MR Motif] Shooting Game Manager |
| Round timing | `ShootingGameManagerMotif` | [MR Motif] Shooting Game Manager |
| Game audio | `ShootingAudioMotif` | [MR Motif] Shooting Game Manager |
| Game configuration | `ShootingGameConfigMotif` | [MR Motif] Shooting Game Manager |
| Debug visualization | `ShootingDebugVisualizerMotif` | [MR Motif] Shooting Game Manager |
| **Avatars** | | |
| Avatar instantiation | `AvatarSpawnerFusion` | [BuildingBlock] Networked Avatar |
| Avatar spawn handling | `AvatarSpawnerHandlerMotif` | [MR Motif] Avatar Spawner Handler |
| Avatar position sync | `AvatarMovementHandlerMotif` | On spawned avatar prefab |
| Avatar name tags | `AvatarNameTagHandlerMotif` | On spawned avatar prefab |
| **Combat** | | |
| Component attachment | `ShootingSetupMotif` | [MR Motif] Shooting Setup |
| Weapon input/bullets | `ShootingPlayerMotif` | Attached to avatar at runtime |
| Health/damage | `PlayerHealthMotif` | Attached to avatar at runtime |
| Bullet physics | `BulletMotif` | BulletMotif.prefab |
| Cover objects | `CoverSpawnerMotif` | [MR Motif] Cover Spawner |
| Networked cover | `NetworkedCoverMotif` | On spawned cover prefab |
| **UI** | | |
| HUD display | `ShootingHUDMotif` | [MR Motif] Shooting HUD Canvas |
| **Spawning** | | |
| Spawn locations | `SpawnManagerMotif` | [MR Motif] Spawn Manager |
| **Platform** | | |
| Platform init | `PlatformInit_` | [BuildingBlock] Platform Init |
| Group presence | `GroupPresenceAndInviteHandlerMotif` | [MR Motif] Group Presence |
| Boundary suppression | `BoundaryDisablerMotif` | [BuildingBlock] Camera Rig |
| **Practice** | | |
| Solo practice mode | `PracticeModeMotif` | [MR Motif] Practice Mode |
| **Metrics/Research** | | |
| CSV metrics logging | `MetricsLogger` | [MR Motif] Metrics Logger |
| Calibration drift tracking | `CalibrationAccuracyTracker` | [MR Motif] Metrics Logger |
| Network latency tracking | `NetworkLatencyTracker` | [MR Motif] Network Metrics |

---

### Common Mistakes to Avoid

| ❌ Mistake | ✅ Correct Approach |
|-----------|---------------------|
| Adding `NetworkBehaviour` to building block without `NetworkObject` | Add custom NetworkBehaviours to separate MR Motif GameObjects with their own `NetworkObject` |
| Adding duplicate `SharedSpatialAnchorManager` | Only ONE instance in scene on `[MR Motifs] SSA Manager` |
| Adding duplicate `ColocationManager` | Only ONE instance in scene on `[MR Motifs] Colocation Manager` |
| Adding game logic to `[BuildingBlock] Colocation` | Use `[MR Motif]` GameObjects for custom game logic |
| Having multiple `NetworkRunner` instances | Only ONE `NetworkRunner` in `[BuildingBlock] Network Manager` |
| Putting health/shooting on building blocks | Attach to avatar prefab at runtime via `ShootingSetupMotif` |
| Using manual `NetworkRunner.StartGame()` | Use `FusionBootstrap.StartSharedClient()` with `FusionBBEvents` callbacks |
| Awaiting async Tasks for connection | Poll `IsConnecting`/`IsConnected` via coroutines |

---

## ✅ Implemented Features

### Core Gameplay
| Feature | Script | Status |
|---------|--------|--------|
| Game states (Waiting/Countdown/Playing/RoundEnd) | `ShootingGameManagerMotif.cs` | ✅ |
| Round timer (3 min default) | `ShootingGameManagerMotif.cs` | ✅ |
| Win condition (10 kills default) | `ShootingGameManagerMotif.cs` | ✅ |
| Score tracking per player | `ShootingGameManagerMotif.cs` | ✅ |
| Manual round restart (hold both grips) | `ShootingGameManagerMotif.cs` | ✅ |
| Auto-restart after round end | `ShootingGameManagerMotif.cs` | ✅ |

### Health & Combat
| Feature | Script | Status |
|---------|--------|--------|
| Networked health (100 HP default) | `PlayerHealthMotif.cs` | ✅ |
| Take damage RPC | `PlayerHealthMotif.cs` | ✅ |
| Death and respawn (3 sec delay) | `PlayerHealthMotif.cs` | ✅ |
| Invulnerability after respawn (2 sec) | `PlayerHealthMotif.cs` | ✅ |
| Kill/death tracking per player | `PlayerHealthMotif.cs` | ✅ |
| Visual hit feedback | `PlayerHealthMotif.cs` | ✅ |

### Weapons & Bullets
| Feature | Script | Status |
|---------|--------|--------|
| Trigger input detection (both hands) | `ShootingPlayerMotif.cs` | ✅ |
| Dual-wield weapon support | `ShootingPlayerMotif.cs` | ✅ |
| Networked bullet spawning | `ShootingPlayerMotif.cs` | ✅ |
| Configurable fire rate (0.2 sec default) | `ShootingPlayerMotif.cs` | ✅ |
| Bullet physics with velocity | `BulletMotif.cs` | ✅ |
| Hit detection (collision + trigger) | `BulletMotif.cs` | ✅ |
| Owner tracking (no self-damage) | `BulletMotif.cs` | ✅ |
| Auto-despawn after lifetime | `BulletMotif.cs` | ✅ |
| Trail renderer effect | `BulletMotif.cs` | ✅ |

### HUD & UI
| Feature | Script | Status |
|---------|--------|--------|
| Health slider with text | `ShootingHUDMotif.cs` | ✅ |
| Kills/deaths display | `ShootingHUDMotif.cs` | ✅ |
| Death panel with respawn countdown | `ShootingHUDMotif.cs` | ✅ |
| Hit markers on successful hits | `ShootingHUDMotif.cs` | ✅ |
| Damage indicator when hit | `ShootingHUDMotif.cs` | ✅ |
| Scoreboard panel | `ShootingGameManagerMotif.cs` | ✅ |

### Networking & Co-location
| Feature | Script | Status |
|---------|--------|--------|
| Photon Fusion 2 Shared Mode | Building Block | ✅ |
| FusionBootstrap session creation | `FusionBootstrap` (Building Block) | ✅ |
| FusionBBEvents callbacks | `SessionDiscoveryManager.cs` | ✅ |
| Simplified session API | `SessionDiscoveryManager.cs` | ✅ |
| Host migration support | `HostMigrationHandlerMotif.cs` | ✅ (disabled) |
| Anchor creation (3 modes) | `SharedSpatialAnchorManager.cs` | ✅ |
| Anchor advertisement/discovery | `SharedSpatialAnchorManager.cs` | ✅ |
| Camera rig alignment to anchor | `ColocationManager.cs` | ✅ |
| Calibration error tracking | `ColocationManager.cs` | ✅ |
| Open play area spawning | `SpawnManagerMotif.cs` | ✅ |

### Avatars
| Feature | Script | Status |
|---------|--------|--------|
| Meta Avatars with networking | Building Block | ✅ |
| Avatar position sync via object-of-interest | `AvatarMovementHandlerMotif.cs` | ✅ |
| Player name tags | `AvatarNameTagHandlerMotif.cs` | ✅ |

### Platform Features
| Feature | Script | Status |
|---------|--------|--------|
| Guardian boundary suppression | `BoundaryDisablerMotif.cs` | ✅ |
| Group presence | `GroupPresenceAndInviteHandlerMotif.cs` | ✅ |
| Deep link invite handling | `InvitationAcceptanceHandlerMotif.cs` | ✅ |

---

## 🔧 Configuration

### Game Settings
| Setting | Default | Location |
|---------|---------|----------|
| Round Duration | 180 sec | `ShootingGameManagerMotif.m_roundDuration` |
| Kills to Win | 10 | `ShootingGameManagerMotif.m_killsToWin` |
| Min Players | 2 | `ShootingGameManagerMotif.m_minPlayersToStart` |
| Auto Restart | true | `ShootingGameManagerMotif.m_autoRestart` |
| Auto Restart Delay | 10 sec | `ShootingGameManagerMotif.m_autoRestartDelay` |

### Player Settings
| Setting | Default | Location |
|---------|---------|----------|
| Max Health | 100 | `PlayerHealthMotif.m_maxHealth` |
| Respawn Delay | 3 sec | `PlayerHealthMotif.m_respawnDelay` |
| Invulnerability | 2 sec | `PlayerHealthMotif.m_invulnerabilityDuration` |

### Weapon Settings
| Setting | Default | Location |
|---------|---------|----------|
| Fire Force | 15 | `ShootingPlayerMotif.m_fireForce` |
| Fire Rate | 0.2 sec | `ShootingPlayerMotif.m_fireRate` |
| Bullet Lifetime | 5 sec | `ShootingPlayerMotif.m_bulletLifetime` |
| Bullet Damage | 10 | `BulletMotif.m_damage` |

### Co-location Settings
| Setting | Default | Location |
|---------|---------|----------|
| Anchor Placement Mode | AtHostPosition | `SharedSpatialAnchorManager.m_anchorPlacementMode` |
| Manual Placement Button | Primary Index Trigger | `SharedSpatialAnchorManager.m_placementButton` |

---

## 📋 Setup Checklist

### Phase 0: Scene Integrity Verification (NEW - Do This First!)
Before testing, verify the scene has no duplicate components or missing dependencies:

- [ ] **NetworkObject on Colocation:** `[BuildingBlock] Colocation` has `NetworkObject` component
- [ ] **Single SSA Manager:** Only ONE `SharedSpatialAnchorManager` in scene (on `[MR Motifs] SSA Manager`)
- [ ] **Single Colocation Manager:** Only ONE `ColocationManager` in scene (on `[MR Motifs] Colocation Manager`)
- [ ] **Scene Mesh Disabled:** `[BuildingBlock] Scene Mesh` is disabled (can re-enable later)
- [ ] **No NetworkBehaviours without NetworkObject:** Every NetworkBehaviour has a NetworkObject on same GameObject

### Phase 1: Configuration (Required)
- [ ] **Photon App ID**: Go to **Fusion > Fusion Hub** → Enter your App ID from [Photon Dashboard](https://dashboard.photonengine.com)
- [ ] **Oculus App ID**: Go to **Oculus > Platform > Edit Settings** → Enter App ID from [Meta Developer Dashboard](https://developer.oculus.com)
- [ ] Verify `BulletMotif.prefab` is assigned to `ShootingSetupMotif` in scene
- [ ] (Optional) Assign weapon prefab to `ShootingSetupMotif` for visual weapons

### Phase 2: Editor Testing
- [ ] Enter Play mode
- [ ] Verify no console errors
- [ ] Check game state shows "Waiting for Players"

### Phase 3: Single Device Testing
- [ ] Build for Android (Quest)
- [ ] Install on Quest device
- [ ] Verify passthrough works
- [ ] Verify avatar spawns at headset position
- [ ] Verify trigger fires bullets (check visually)

### Phase 4: Two-Device Testing
- [ ] Both devices on same WiFi network
- [ ] Both devices with completed Space Setup
- [ ] Deploy to both devices
- [ ] Verify both players see each other
- [ ] Verify spatial alignment (players in correct positions)
- [ ] Verify game starts when 2 players present

### Phase 5: Combat Testing
- [ ] Bullets visible to both players
- [ ] Hit detection works (damage applied)
- [ ] Death and respawn cycle works
- [ ] Scoreboard updates correctly
- [ ] Win condition triggers round end

---

## 🚀 Quick Start

### Prerequisites
- Unity 6 (6000.0.x)
- Meta XR SDK packages installed
- Photon Fusion 2 SDK
- Valid Photon App ID (get from [Photon Dashboard](https://dashboard.photonengine.com))
- Valid Oculus Platform App ID (get from [Meta Developer Dashboard](https://developer.oculus.com))
- Two Meta Quest devices for co-location testing

### Setup
1. Open `Assets/Scenes/ShootingGame.unity`
2. Configure Photon: **Fusion > Fusion Hub** → Set App ID
3. Configure Oculus: **Oculus > Platform > Edit Settings** → Set App ID
4. Build for Android (Quest)

### Testing Co-location
1. Both Quest headsets must have completed **Space Setup** in the same room
2. Deploy app to both devices
3. Launch on both devices (same WiFi network)
4. Host starts game → creates spatial anchor
5. Client discovers session → aligns to shared anchor
6. Both players now share the same coordinate system!

---

## 🔮 Future Implementation Plans

### Phase 1: Polish & Bug Fixes
| Feature | Priority | Notes |
|---------|----------|-------|
| Visual weapon models on controllers | High | Assign weapon prefab to `ShootingSetupMotif` |
| Bullet impact VFX | High | Create hit effect prefab for `BulletMotif.m_hitEffectPrefab` |
| Audio feedback (shots, hits, death) | High | Add audio clips to relevant components |
| UI polish (scoreboard, round announcements) | Medium | Improve `ShootingHUDMotif` visuals |
| Calibration error display | Low | Show alignment quality to players |

### Phase 2: Gameplay Enhancements
| Feature | Priority | Notes |
|---------|----------|-------|
| Multiple weapon types | Medium | Create weapon variants (pistol, shotgun, etc.) |
| Ammunition system | Medium | Limited ammo with reload mechanic |
| Power-ups (health, speed, damage) | Medium | Spawnable pickups in play area |
| Team modes (2v2, 3v3) | Medium | Team assignment and team scoring |
| Different game modes (CTF, King of the Hill) | Low | Alternative win conditions |

### Phase 3: Advanced Features
| Feature | Priority | Notes |
|---------|----------|-------|
| Voice chat integration | Medium | Re-add Photon Voice support |
| Spectator mode | Low | Watch ongoing matches |
| Practice mode (single player) | Low | AI targets or solo warm-up |
| Room-aware obstacles | Low | Use MRUK to create cover from furniture |
| Persistent leaderboards | Low | Cloud-saved player stats |
| Custom avatar skins | Low | Unlockable cosmetics |

### Phase 4: Social & Discovery
| Feature | Priority | Notes |
|---------|----------|-------|
| Private room codes | Medium | Invite friends to specific sessions |
| Match history | Low | Track past games |
| Achievements | Low | Meta Platform achievements |
| Cross-session friends | Low | Play with same people again |

### Phase 5: Effect Mesh Arena System 🎯

Use Meta's MRUK (Mixed Reality Utility Kit) to scan real-world environments and use them as game arenas with collision detection, boundaries, and multiplayer sharing.

#### 5.1 Arena Scanning Implementation
| Feature | Priority | Notes |
|---------|----------|-------|
| `ArenaManagerMotif.cs` | High | Main arena management script |
| Listen to Scene Mesh completion | High | `RoomMeshEvent.OnRoomMeshLoadCompleted` |
| Query MRUK room bounds | High | `room.GetRoomBounds()` for playable area |
| Arena boundary visualization | Medium | Visual indicators of play area limits |
| `ArenaConfigMotif.cs` | Medium | Configurable arena settings |

#### 5.2 EffectMesh Integration
| Feature | Priority | Notes |
|---------|----------|-------|
| Custom arena material | Medium | Wireframe, selective passthrough, or solid |
| Semantic label filtering | Medium | Exclude CEILING, DOOR, WINDOW_FRAME |
| Physics colliders | High | Scene Mesh colliders for bullet physics |
| Cut holes for doors/windows | Low | `EffectMesh.CutHoles` feature |

#### 5.3 Arena Persistence (Save/Load)
| Feature | Priority | Notes |
|---------|----------|-------|
| JSON arena export | Medium | `MRUK.Instance.LoadSceneFromJson()` |
| Space Sharing for multiplayer | High | `ShareRoomsAsync()` to share with others |
| Spatial Anchor persistence | Medium | Arena position survives app restart |

#### 5.4 Shooting Game Integration
| Component | Integration Point |
|-----------|-------------------|
| `SpawnManagerMotif` | Use arena floor bounds for spawn positioning |
| `BulletMotif` | Use Scene Mesh colliders for bullet physics |
| `ShootingGameManagerMotif` | Wait for arena ready before starting match |
| `ColocationManager` | Share arena with other players via Space Sharing API |

#### Current Foundation (Already In Scene)
- ✅ `[BuildingBlock] Scene Mesh` with `RoomMeshController` + `RoomMeshEvent`
- ✅ `[BuildingBlock] MR Utility Kit` for MRUK access
- ✅ `[BuildingBlock] Colocation` for multiplayer colocation
- ✅ `[MR Motifs] SSA Manager` for Space Sharing capability

#### Key MRUK APIs
```csharp
// Load scene from device scan
MRUK.Instance.LoadSceneFromDevice();

// Get current room and bounds
var room = MRUK.Instance.GetCurrentRoom();
var bounds = room.GetRoomBounds();

// Share room with other players (Host)
await room.ShareRoomAsync(groupUuid);

// Load shared room (Guest)
await MRUK.Instance.LoadSceneFromSharedRooms(null, groupUuid, alignmentData);

// EffectMesh for visualization with colliders
effectMesh.AddColliders = true;
effectMesh.Labels = new[] { "FLOOR", "WALL", "GLOBAL_MESH" };
```

---

## 🐛 Known Issues & Troubleshooting

### ⚠️ Scene Configuration Issues (CRITICAL)

#### Duplicate Components Break Colocation
**Symptoms:** Game stuck on "Waiting for Players", colocation never completes, no errors visible.

**Cause:** Duplicate `SharedSpatialAnchorManager` or `ColocationManager` components in scene.

**Solution:**
1. Open scene in Unity
2. Search hierarchy for `SharedSpatialAnchorManager` - should be on `[MR Motifs] SSA Manager` ONLY
3. Search hierarchy for `ColocationManager` - should be on `[MR Motifs] Colocation Manager` ONLY
4. Delete any duplicates
5. Save scene

**Prevention:** See "Detailed Scene Object & Component Responsibilities" section above.

---

#### Missing NetworkObject on Building Block
**Symptoms:** Colocation fails silently, FusionMessenger/FusionNetworkData not functioning.

**Cause:** `[BuildingBlock] Colocation` has NetworkBehaviour components (`FusionMessenger`, `FusionNetworkData`) but no `NetworkObject`.

**Solution:**
1. Select `[BuildingBlock] Colocation` in hierarchy
2. Add `NetworkObject` component if missing
3. Save scene

---

#### Building Block Scene Mesh Conflicts
**Symptoms:** Colocation works but room mesh causes issues.

**Solution:** Disable `[BuildingBlock] Scene Mesh` GameObject until colocation is stable.

---

### Players Don't See Each Other
1. Verify both devices completed Space Setup in the same room
2. Check both devices are on the same WiFi network
3. Verify Photon App ID is configured correctly
4. Check console for Fusion connection errors
5. Ensure `AvatarMovementHandlerMotif` is syncing positions

### Bullets Don't Do Damage
1. Verify `BulletMotif` prefab has a collider set as trigger
2. Check avatar has collider for hit detection
3. Verify `TakeDamageRpc` is being called (add debug logs)
4. Ensure bullet's `OwnerPlayer` is set correctly (no self-damage)

### Game Doesn't Start
1. Check `ShootingGameManagerMotif.m_minPlayersToStart` (default: 2)
2. Verify players are registered via `RegisterPlayer()`
3. Check game state in Inspector while running

### Co-location Not Aligning
1. Both devices must have completed **Space Setup** (room scan)
2. Verify host creates anchor (`SharedSpatialAnchorManager` logs)
3. Verify client discovers anchor (check discovery logs)
4. Ensure `ColocationManager.AlignUserToAnchor()` is called
5. Check calibration error isn't too high

### Performance Issues
1. Reduce bullet lifetime (`ShootingPlayerMotif.m_bulletLifetime`)
2. Increase fire rate cooldown (`ShootingPlayerMotif.m_fireRate`)
3. Check for excessive debug logging

---

## 📚 Architecture Deep Dive

### Game State Machine
```
WAITING_FOR_PLAYERS
    ↓ (2+ players connected)
COUNTDOWN (3 seconds)
    ↓
PLAYING
    ↓ (player reaches kill target OR timer expires)
ROUND_END (show winner, 10 sec delay)
    ↓ (auto-restart OR hold both grips)
WAITING_FOR_PLAYERS
```

### Co-location Flow
```
1. Host creates OVRSpatialAnchor at headset position
2. Host saves anchor and gets UUID
3. Host advertises session via OVRColocationSession
4. Client discovers nearby session
5. Client loads anchor using shared UUID
6. ColocationManager aligns client's camera rig to anchor
7. All players now share identical world coordinates
```

### Networking Architecture (Fusion 2 Shared Mode)
```
NetworkRunner (Shared Mode)
├── Auto Matchmaking → Players auto-join same session
├── Host = State Authority for game state
├── Each player = Input Authority for their avatar
├── [Networked] properties → Auto-synced values
├── RPCs → For events (damage, game state changes)
└── Host Migration → Seamless takeover if host leaves
```

### Open Play Area Spawning
```
1. Player connects to Fusion session
2. SpawnManagerMotif.GetSpawnLocation() returns headset position
3. Avatar spawns at physical location (no teleportation)
4. Player is already "there" - no movement needed
```

---

## ❌ Explicitly Out of Scope (Not MVP)

- Voice chat (removed Photon Voice dependency)
- Predefined spawn points (using open play area)
- Seeing other players' health bars overhead
- Weapon pickups on the ground
- Environment destruction
- AI opponents

---

## 🔮 Future Architecture: Server-Authoritative with Global Anchors

> **Branch for implementation:** `feature/server-authoritative-anchors`

### Current Approach
- Each headset aligns to a **shared spatial anchor** via colocation
- Avatar positions are synced **relative to an "object of interest"**
- All game logic runs on headsets via Photon Fusion (Shared Mode)

### Proposed Architecture

**1. Global Anchor(s) as Reference Frame**
- More stable reference point, less drift over time
- Multiple anchors for larger play areas (>3m from single anchor causes drift)
- Better for persistent/long-running sessions
- Pre-placed physical anchor markers (QR codes or known positions)

**2. Local Server as Master Handler**
- Offloads processing from headsets (tracking sync, game state, hit detection)
- Better for longer runs - headsets thermal throttle over time
- Single source of truth - less sync conflicts
- Could run on a laptop, Raspberry Pi, or cloud server

### Architecture Diagram
```
┌─────────────────────────────────────────────────────┐
│                  Local Server                        │
│  ┌─────────────────────────────────────────────┐    │
│  │  Game State Manager (Authoritative)         │    │
│  │  - Player positions (anchor-relative)       │    │
│  │  - Hit detection                            │    │
│  │  - Score tracking                           │    │
│  │  - Round management                         │    │
│  └─────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────┐    │
│  │  Anchor Registry                            │    │
│  │  - Global anchor UUIDs                      │    │
│  │  - Calibration data                         │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
              │                    │
              ▼                    ▼
    ┌─────────────────┐  ┌─────────────────┐
    │   Headset 1     │  │   Headset 2     │
    │   - Tracking    │  │   - Tracking    │
    │   - Rendering   │  │   - Rendering   │
    │   - Input       │  │   - Input       │
    │   - Anchor loc  │  │   - Anchor loc  │
    └─────────────────┘  └─────────────────┘
```

### Key Technologies for Implementation
- **Photon Fusion Server Mode** instead of Shared Mode
- **Dedicated Server Build** running game logic
- **Pre-placed physical anchor markers** (QR codes or known positions)
- **Meta's Space Sharing API** for room-scale anchor sharing

### Trade-offs
| Aspect | Current (Shared Mode) | Future (Server Mode) |
|--------|----------------------|---------------------|
| Latency | Lower (peer-to-peer) | Higher (server round-trip) |
| Headset Load | Higher | Lower |
| Drift Handling | Per-session | Persistent calibration |
| Scalability | 2-8 players | Many players |
| Infrastructure | None | Server required |

---

## 🔗 Dependencies

| Package | Purpose |
|---------|---------|
| Meta XR Core SDK | OVRManager, OVRCameraRig, OVRPassthroughLayer |
| Meta MR Utility Kit | MRUK for room awareness |
| Meta Avatars SDK | AvatarEntity, networked avatars |
| Photon Fusion 2 | NetworkRunner, NetworkBehaviour, RPCs |
| TextMesh Pro | UI text rendering |

---

## License

Based on Meta MR Motifs sample code. See individual files for copyright notices.
