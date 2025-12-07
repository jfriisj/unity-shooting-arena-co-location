# Shooting Arena - Data Flow Documentation

**Last Updated:** December 2024  
**Current Implementation:** Simplified FusionBootstrap + FusionBBEvents pattern

---

## 📋 Implementation Overview

This document describes the data flow for Host and Client startup in the Shooting Arena game. The networking layer uses a **simplified approach** based on Meta's official MRMotifs samples:

### Key Components

| Component | Pattern | Responsibility |
|-----------|---------|----------------|
| `FusionBootstrap` | Building Block | Session creation/joining via `StartSharedClient()` |
| `FusionBBEvents` | Callbacks | Connection events (OnConnectedToServer, OnConnectFailed, etc.) |
| `SessionDiscoveryManager` | Wrapper | Simplified API around FusionBootstrap |
| `GameStartupManagerMotif` | Orchestrator | Polls IsConnecting/IsConnected, manages flow |

### Session Discovery API (Simplified)

```csharp
// Host Flow
m_sessionDiscovery.StartAsHost();           // Generates unique name, calls StartSharedClient()

// Client Flow  
m_sessionDiscovery.JoinSession(sessionName); // Sets DefaultRoomName, calls StartSharedClient()

// Properties (polled by GameStartupManagerMotif)
m_sessionDiscovery.IsConnecting  // True while connecting
m_sessionDiscovery.IsConnected   // True when connected
m_sessionDiscovery.Runner        // Reference to NetworkRunner
```

### FusionBBEvents Callbacks

The SessionDiscoveryManager subscribes to these events for connection status:

```csharp
FusionBBEvents.OnConnectedToServer    // Connection successful
FusionBBEvents.OnDisconnectedFromServer // Disconnected
FusionBBEvents.OnConnectFailed        // Connection failed
FusionBBEvents.OnSceneLoadDone        // Scene loaded
FusionBBEvents.OnPlayerJoined         // Player joined session
FusionBBEvents.OnPlayerLeft           // Player left session
FusionBBEvents.OnShutdown             // Runner shutdown
```

---

# 🎮 Host Data Flow Analysis - ShootingGame Scene

## Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    HOST STARTUP FLOW                                     │
│                          (Simplified FusionBootstrap Approach)                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│   1. ROLE        │     │   2. ROOM SCAN   │     │   3. NETWORK     │     │   4. COLOCATION  │
│   SELECTION      │────▶│                  │────▶│   SESSION        │────▶│   ANCHOR         │
│                  │     │                  │     │                  │     │                  │
│ RoleSelectionUI  │     │ MRUK             │     │ FusionBootstrap  │     │ SSA Manager      │
│                  │     │ OVRScene         │     │ FusionBBEvents   │     │ ColocationMgr    │
└──────────────────┘     └──────────────────┘     └──────────────────┘     └──────────────────┘
                                                                                   │
                                                                                   ▼
┌──────────────────┐     ┌──────────────────┐                          ┌──────────────────┐
│   6. READY       │     │   5. ROOM        │                          │                  │
│                  │◀────│   SHARING        │◀─────────────────────────│  Room Sharing    │
│ Avatar Spawning  │     │                  │                          │  Motif           │
│ Game Start       │     │ RoomSharingMotif │                          │                  │
└──────────────────┘     │ MRUK ShareRooms  │                          └──────────────────┘
                         └──────────────────┘
```

## Detailed Step-by-Step Flow

### Step 1: Role Selection
**Components:** `GameStartupManagerMotif`, `RoleSelectionModalUI`

```
User Action: Press "HOST" button
     │
     ▼
RoleSelectionModalUI.OnHostSelected()
     │
     ▼
GameStartupManagerMotif.OnHostRoleSelected()
     ├── m_isHost = true
     ├── m_roleSelected = true
     ├── Hide RoleSelectionUI
     ├── Show StartupModalUI
     └── StartCoroutine(HostFlowWithRoleSelectionCoroutine())
```

### Step 2: Room Scan (BEFORE Network)
**Components:** `MRUK`, `OVRScene`

```
GameStartupManagerMotif.HostFlowWithRoleSelectionCoroutine()
     │
     ▼
SetState(CheckingRoomScan)
     │
     ▼
Check: MRUK.GetCurrentRoom() != null?
     │
     ├── YES → Skip to Step 3
     │
     └── NO → Try LoadSceneFromDevice()
              │
              ├── Success → Room loaded! Skip to Step 3
              │
              ├── NoRoomsFound → Prompt Space Setup
              │   └── OVRScene.RequestSpaceSetup()
              │       └── After completion → LoadSceneFromDevice()
              │
              └── NoScenePermission → ERROR: Needs permissions
```

**⚠️ KEY INSIGHT:** Room scan happens BEFORE network connection. The old code tried to use `RoomScanManager` (a NetworkBehaviour) which doesn't work without network.

### Step 3: Create Network Session (Simplified)
**Components:** `SessionDiscoveryManager`, `FusionBootstrap`, `FusionBBEvents`

The simplified approach uses FusionBootstrap building block with event-based callbacks:

```
SetState(CreatingSession)
     │
     ▼
SessionDiscoveryManager.StartAsHost(customSessionName?)
     │
     ├── Generate session name: "ShootingGame_XXXX"
     │
     ├── Configure: m_fusionBootstrap.DefaultRoomName = sessionName
     │
     └── Call: m_fusionBootstrap.StartSharedClient()
          │
          └── FusionBBEvents callbacks fire automatically:
               ├── OnConnectedToServer → HandleConnectedToServer()
               │   ├── m_isConnected = true
               │   ├── m_isConnecting = false
               │   ├── m_runner = runner
               │   └── Fire: OnConnected, OnSessionCreated events
               │
               └── OnConnectFailed → HandleConnectFailed()
                   ├── m_isConnecting = false
                   └── Fire: OnConnectionFailed event
```

**GameStartupManagerMotif polls SessionDiscoveryManager:**
```csharp
// Polling loop (replaces Task.await approach)
while (m_sessionDiscovery.IsConnecting && taskElapsed < taskTimeout)
{
    yield return new WaitForSeconds(0.5f);
    taskElapsed += 0.5f;
}

if (m_sessionDiscovery.IsConnected)
{
    m_networkRunner = m_sessionDiscovery.Runner;
    // Continue to colocation step...
}
```

**Network Session Data:**
- Session Name: `ShootingGame_XXXX` (auto-generated)
- Game Mode: `Shared` (via FusionBootstrap.AutoStartAs)
- Max Players: Configured in FusionBootstrap

### Step 4: Colocation Anchor (Network Objects Spawned)
**Components:** `SharedSpatialAnchorManager` (NetworkBehaviour), `ColocationManager`

```
When NetworkRunner starts → Spawns networked prefabs
     │
     ▼
SharedSpatialAnchorManager.Spawned()
     │
     ▼
Check: Runner.IsSharedModeMasterClient?
     │
     ├── YES (Host) → AdvertiseColocationSession()
     │
     └── NO (Client) → DiscoverNearbySession()
```

**Host Colocation Flow:**
```
AdvertiseColocationSession()
     │
     ▼
OVRColocationSession.StartAdvertisementAsync()
     │
     ├── Success → m_sharedAnchorGroupId = result.Value
     │             m_colocationEstablished = true
     │             OnColocationSessionEstablished?.Invoke(groupId)
     │             │
     │             ▼
     │         CreateAndShareAlignmentAnchor()
     │             │
     │             ▼
     │         Determine anchor position (AtHostPosition mode):
     │             position = headset XZ projected to floor
     │             rotation = headset forward direction (yaw only)
     │             │
     │             ▼
     │         CreateAnchor(position, rotation)
     │             └── anchor.SaveAnchorAsync()
     │                 └── OVRSpatialAnchor.ShareAsync(anchor, groupId)
     │             │
     │             ▼
     │         ColocationManager.RegisterHostCalibration(position)
     │             └── m_isAligned = true
     │                 OnAlignmentComplete?.Invoke()
     │
     └── Failure → ERROR
```

**Colocation Data Shared:**
- `m_sharedAnchorGroupId`: GUID for the colocation group
- Anchor UUID, Position, Rotation

### Step 5: Room Sharing (Send Room Mesh to Clients)
**Components:** `RoomSharingMotif` (NetworkBehaviour), `MRUK`

```
RoomSharingMotif.Spawned()
     │
     ▼
Check: Runner.IsSharedModeMasterClient?
     │
     ├── YES (Host) → StartHostRoomSharing()
     │
     └── NO (Client) → StartGuestRoomLoading()
```

**Host Room Sharing Flow:**
```
StartHostRoomSharing()
     │
     ▼
Check: m_ssaManager.IsColocationEstablished?
     │
     ├── YES → ShareRoomAsync(groupId)
     │
     └── NO → Subscribe to OnColocationSessionEstablished
              └── When fired → ShareRoomAsync(groupId)
```

```
ShareRoomAsync(groupId)
     │
     ▼
Get MRUK.Instance
     │
     ▼
Check: m_mruk.GetCurrentRoom() != null?
     │
     ├── NO → LoadSceneFromDevice() first
     │
     └── YES → Continue
              │
              ▼
          Get room = m_mruk.GetCurrentRoom()
              │
              ▼
          Get floorAnchor = room.FloorAnchor
              │
              ▼
          Get roomUuid = room.Anchor.Uuid
              │
              ▼
          MRUK.ShareRoomsAsync([room], groupId)
              │
              ├── Success → Store in Networked properties:
              │             SharedGroupIdString = groupId.ToString()
              │             SharedRoomUuidString = roomUuid.ToString()
              │             HostFloorPosition = floor.position
              │             HostFloorRotation = floor.rotation
              │             RoomShared = true
              │             │
              │             ▼
              │         OnRoomShared?.Invoke()
              │         EnableGlobalMeshColliders()
              │
              └── Failure → ERROR logged
```

**Room Data Shared (via Fusion Networked Properties):**
- `SharedGroupIdString`: Group GUID as string
- `SharedRoomUuidString`: Room GUID as string
- `HostFloorPosition`: Vector3 floor position
- `HostFloorRotation`: Quaternion floor rotation
- `RoomShared`: NetworkBool flag

### Step 6: Ready → Avatar Spawn
**Components:** `GameStartupManagerMotif`, Avatar system

```
GameStartupManagerMotif waits for:
     │
     ├── m_ssaManager.IsColocationEstablished == true
     │
     ├── m_colocationManager.IsAligned == true (for host, always true after RegisterHostCalibration)
     │
     └── m_roomSharing.IsRoomShared == true (if EnableRoomSharing is enabled)
          │
          ▼
     CompleteStartup()
          │
          ├── SetState(Ready)
          ├── m_startupComplete = true
          ├── OnStartupComplete?.Invoke()
          │
          └── Avatar spawning can proceed
```

---

## Data Dependencies Matrix

| Component | Depends On | Provides | Network Required? |
|-----------|------------|----------|-------------------|
| `RoleSelectionModalUI` | User input | Host/Client selection + session name | ❌ No |
| `MRUK.LoadSceneFromDevice()` | Device scene data | Room mesh | ❌ No |
| `OVRScene.RequestSpaceSetup()` | User scan | New scene data | ❌ No |
| `SessionDiscoveryManager` | FusionBootstrap | Session state (IsConnecting/IsConnected) | ✅ Creates it |
| `FusionBootstrap` | Photon settings | NetworkRunner via StartSharedClient() | ✅ Yes |
| `FusionBBEvents` | FusionBootstrap | Connection callbacks | ✅ Yes |
| `SharedSpatialAnchorManager` | NetworkRunner.Spawned() | Colocation GroupID, Anchor | ✅ Yes |
| `ColocationManager` | SSA anchor | Camera rig alignment | ❌ No (uses anchor data) |
| `RoomSharingMotif` | NetworkRunner.Spawned() | Shared room mesh data | ✅ Yes |

---

## Potential Issues Identified

### ✅ Fixed Issue: Room Scan Before Network
The old code tried to use `RoomScanManager.RequestRoomScan()` before network was connected. Since `RoomScanManager` is a `NetworkBehaviour`, its `Spawned()` never ran, causing `IsSceneLoaded` to always be `false`.

**Solution Applied:** Now uses `MRUK.LoadSceneFromDevice()` and `OVRScene.RequestSpaceSetup()` directly.

### ✅ Fixed Issue: Simplified Networking Pattern
Previous implementation used manual `NetworkRunner.StartGame()` calls which caused timeout issues.

**Solution Applied (December 2024):** 
- Uses `FusionBootstrap.StartSharedClient()` (Meta's recommended pattern)
- Uses `FusionBBEvents` for connection callbacks
- `GameStartupManagerMotif` polls `IsConnecting`/`IsConnected` instead of awaiting Tasks
- No `INetworkRunnerCallbacks` implementation needed

### ⚠️ Note: Manual Session Name Entry
The simplified approach requires clients to manually enter the session name (shared by host verbally or via text). This trades automatic lobby refresh for implementation simplicity and reliability.

### ⚠️ Potential Issue: Duplicate Room Loading
Both places attempt to load room:
1. `GameStartupManagerMotif` (before network) - **This is correct**
2. `RoomSharingMotif.ShareRoomAsync()` has fallback `LoadSceneFromDevice()` if room is null

This is actually safe - the second load will just return the already-loaded room.

---

## Timing Sequence

```
T=0.0s   App Start
T=0.5s   Platform Init
T=1.0s   Show Role Selection UI
T=1.5s   User presses HOST
T=2.0s   Check existing room scan
T=2.5s   Load room from device (or prompt Space Setup)
T=3.0s   Room loaded
T=3.5s   Create network session
T=4.0s   Session created, NetworkObjects spawn
T=4.5s   SharedSpatialAnchorManager.Spawned() → AdvertiseColocationSession()
T=5.0s   Colocation advertised, create anchor
T=5.5s   Anchor created, saved, shared
T=5.5s   ColocationManager.RegisterHostCalibration()
T=6.0s   RoomSharingMotif.Spawned() → StartHostRoomSharing()
T=6.5s   MRUK.ShareRoomsAsync() completes
T=7.0s   RoomShared = true
T=7.5s   CompleteStartup() → READY
T=8.0s   Avatar can spawn
```

---

## Recommendations

1. **Current flow is correct** - Room scan happens before network, then network spawns trigger colocation and room sharing.

2. **No duplicate scan prompts** - We disabled auto-prompt in `RoomScanManager`.

3. **Clear separation of concerns:**
   - `GameStartupManagerMotif` - Orchestrates flow
   - `MRUK` - Room data provider
   - `SessionDiscoveryManager` - Network session management
   - `SharedSpatialAnchorManager` - Colocation anchors
   - `RoomSharingMotif` - Room mesh sharing

---

# 🎮 Client Data Flow Analysis - ShootingGame Scene

## Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                   CLIENT STARTUP FLOW                                    │
│                          (Simplified FusionBootstrap Approach)                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│   1. ROLE        │     │   2. ENTER       │     │   3. JOIN        │     │   4. DISCOVER    │
│   SELECTION      │────▶│   SESSION NAME   │────▶│   SESSION        │────▶│   COLOCATION     │
│                  │     │                  │     │                  │     │                  │
│ RoleSelectionUI  │     │ Manual Entry     │     │ FusionBootstrap  │     │ SSA Manager      │
│                  │     │ (from Host)      │     │ FusionBBEvents   │     │ OVRColocation    │
└──────────────────┘     └──────────────────┘     └──────────────────┘     └──────────────────┘
                                                                                   │
                                                                                   ▼
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│   7. READY       │     │   6. LOAD        │     │   5. LOCALIZE    │     │                  │
│                  │◀────│   SHARED ROOM    │◀────│   ANCHOR         │◀────│  Wait for Host   │
│ Avatar Spawning  │     │                  │     │                  │     │  Anchor Share    │
│ Game Start       │     │ RoomSharingMotif │     │ ColocationMgr    │     │                  │
└──────────────────┘     │ MRUK LoadShared  │     │ AlignToAnchor    │     └──────────────────┘
                         └──────────────────┘     └──────────────────┘
```

## Detailed Step-by-Step Flow

### Step 1: Role Selection
**Components:** `GameStartupManagerMotif`, `RoleSelectionModalUI`

```
User Action: Views session list, selects a session
     │
     ▼
RoleSelectionModalUI shows available sessions
     │ (refreshed via SessionDiscoveryManager)
     │
     ▼
User taps on session entry (e.g., "ShootingGame_A1B2")
     │
     ▼
RoleSelectionModalUI.OnSessionSelected(sessionName)
     │
     ▼
GameStartupManagerMotif.OnSessionSelected(sessionName)
     ├── m_isHost = false
     ├── m_roleSelected = true
     ├── m_selectedSessionName = sessionName
     ├── Hide RoleSelectionUI
     ├── Show StartupModalUI
     └── StartCoroutine(ClientFlowWithRoleSelectionCoroutine(sessionName))
```

Let me read the full client flow: 

Read [](file:///c%3A/github/shooting/Assets/Scripts/Startup/GameStartupManagerMotif.cs#500-500), lines 500 to 750

Now let me read the guest flow in RoomSharingMotif: 

Read [](file:///c%3A/github/shooting/Assets/Scripts/Colocation/RoomSharingMotif.cs#350-350), lines 350 to 472

Now I have the complete picture for the client flow. Let me document it:

---

## Client Detailed Step-by-Step Flow

### Step 1: Role Selection - Enter Session Name
**Components:** `GameStartupManagerMotif`, `RoleSelectionModalUI`

In the simplified approach, clients enter the session name manually (shared by host):

```
App Start
     │
     ▼
WaitForPlatformThenShowRoleSelection()
     │
     ▼
SetState(RoleSelection)
     │
     ▼
RoleSelectionModalUI.Show()
     │
     ▼
User enters session name (e.g., "ShootingGame_A1B2")
     │ (Host shares this name with client via voice/text)
     │
     ▼
User taps "Join" button
     │
     ▼
GameStartupManagerMotif.OnSessionSelected(sessionName)
```

> **Note:** The simplified approach does not use automatic lobby refresh. Clients must know the session name from the host.

### Step 2: Join Network Session (Simplified)
**Components:** `SessionDiscoveryManager`, `FusionBootstrap`, `FusionBBEvents`

```
ClientFlowWithRoleSelectionCoroutine(sessionName)
     │
     ▼
SetState(JoiningSession)
m_statusMessage = "Joining {sessionName}..."
     │
     ▼
SessionDiscoveryManager.JoinSession(sessionName)
     │
     ├── Configure: m_fusionBootstrap.DefaultRoomName = sessionName
     │
     └── Call: m_fusionBootstrap.StartSharedClient()
          │
          └── FusionBBEvents callbacks fire automatically:
               ├── OnConnectedToServer → HandleConnectedToServer()
               │   ├── m_isConnected = true
               │   ├── m_isConnecting = false
               │   ├── m_runner = runner
               │   ├── m_isHost = runner.IsSharedModeMasterClient
               │   └── Fire: OnConnected, OnSessionJoined events
               │
               └── OnConnectFailed → HandleConnectFailed()
                   └── Fire: OnConnectionFailed event
```

**GameStartupManagerMotif polls SessionDiscoveryManager:**
```csharp
while (m_sessionDiscovery.IsConnecting && taskElapsed < taskTimeout)
{
    yield return new WaitForSeconds(0.5f);
    taskElapsed += 0.5f;
    m_statusMessage = $"Joining {sessionName}... ({taskElapsed:F0}s)";
}

if (m_sessionDiscovery.IsConnected)
{
    m_networkRunner = m_sessionDiscovery.Runner;
    // Continue to colocation step...
}
```

**⚠️ KEY DIFFERENCE FROM HOST:** Client does NOT do room scan before joining. They join existing session and receive room data from host.

### Step 3: Wait for Colocation Discovery
**Components:** `SharedSpatialAnchorManager` (spawned by network)

```
SetState(WaitingForAnchor)
m_statusMessage = "Waiting for host anchor..."
     │
     ▼
Wait for SharedSpatialAnchorManager to be spawned
     │
     ▼
SharedSpatialAnchorManager.Spawned() [CLIENT PATH]
     │
     ▼
Check: Runner.IsSharedModeMasterClient?
     │
     └── NO (Client) → DiscoverNearbySession()
```

```
DiscoverNearbySession()
     │
     ▼
await Task.Delay(2000)  // Wait for host to advertise
     │
     ▼
Subscribe: OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered
     │
     ▼
OVRColocationSession.StartDiscoveryAsync()
     │
     ├── Success → "Discovery started successfully"
     │             Wait for callback...
     │
     └── Failure → ERROR
```

```
OnColocationSessionDiscovered(session)
     │
     ▼
Unsubscribe from event
     │
     ▼
m_sharedAnchorGroupId = session.AdvertisementUuid
m_colocationEstablished = true
     │
     ▼
OnColocationSessionEstablished?.Invoke(groupId)
     │
     ▼
LoadAndAlignToAnchor(groupId)
```

**Colocation Discovery Data Received:**
- `session.AdvertisementUuid`: The host's colocation group GUID

### Step 4: Localize Anchor (Align to Host's Space)
**Components:** `SharedSpatialAnchorManager`, `ColocationManager`

```
LoadAndAlignToAnchor(groupUuid)
     │
     ▼
OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupUuid, unboundAnchors)
     │
     ├── Success + anchors found → Process anchors
     │
     └── Failure or 0 anchors → ERROR
```

```
For each unboundAnchor:
     │
     ▼
unboundAnchor.LocalizeAsync()
     │
     ├── Success → Anchor is now localized in client's space!
     │   │
     │   ▼
     │   Create GameObject with OVRSpatialAnchor
     │   unboundAnchor.BindTo(spatialAnchor)
     │   │
     │   ▼
     │   ColocationManager.AlignUserToAnchor(spatialAnchor)
     │
     └── Failure → Try next anchor
```

```
ColocationManager.AlignUserToAnchor(anchor)
     │
     ▼
Validate: anchor != null && anchor.Localized
     │
     ▼
Store pre-alignment state:
     m_preAlignmentPosition = cameraRig.position
     m_preAlignmentRotation = cameraRig.rotation
     │
     ▼
CRITICAL ALIGNMENT TRANSFORM:
     cameraRig.position = anchor.InverseTransformPoint(Vector3.zero)
     cameraRig.eulerAngles = new Vector3(0, -anchor.eulerAngles.y, 0)
     │
     ▼
m_isAligned = true
OnAlignmentComplete?.Invoke()
```

**What This Alignment Does:**
- Moves the camera rig so that the client's physical position corresponds to where they would be in the host's coordinate system
- Both players now share the same virtual origin point (the anchor)

### Step 5: Wait for Host's Room Data (Networked Properties)
**Components:** `RoomSharingMotif` (NetworkBehaviour)

```
SetState(LocalizingAnchor)
m_statusMessage = "Aligning to shared space..."
     │
     ▼
Wait for ColocationManager.IsAligned == true
     │
     ▼
SetState(LoadingRoom)
m_statusMessage = "Loading shared room..."
```

```
RoomSharingMotif.Spawned() [CLIENT PATH]
     │
     ▼
Check: Runner.IsSharedModeMasterClient?
     │
     └── NO (Client) → StartGuestRoomLoading()
```

```
StartGuestRoomLoading()
     │
     ▼
Check: m_ssaManager.IsColocationEstablished?
     │
     ├── YES → WaitForSharedRoomAsync(groupId)
     │
     └── NO → Subscribe to OnColocationSessionEstablished
              └── When fired → WaitForSharedRoomAsync(groupId)
```

```
WaitForSharedRoomAsync(groupId)
     │
     ▼
POLL NETWORKED PROPERTIES (from Fusion):
     Wait until:
     - RoomShared == true
     - SharedRoomUuidString is not empty
     │
     ├── Timeout (30s) → "Using local room data" (fallback)
     │
     └── Data received → Parse alignment data
```

**Networked Properties Received from Host:**
| Property | Type | Example Value |
|----------|------|---------------|
| `SharedGroupIdString` | NetworkString<_64> | "a1b2c3d4-..." |
| `SharedRoomUuidString` | NetworkString<_64> | "e5f6g7h8-..." |
| `HostFloorPosition` | Vector3 | (0.5, 0.0, 1.2) |
| `HostFloorRotation` | Quaternion | (0, 0.707, 0, 0.707) |
| `RoomShared` | NetworkBool | true |

### Step 6: Load Shared Room Mesh
**Components:** `RoomSharingMotif`, `MRUK`

```
Parse received data:
     groupId = Guid.Parse(SharedGroupIdString)
     roomUuid = Guid.Parse(SharedRoomUuidString)
     floorPose = new Pose(HostFloorPosition, HostFloorRotation)
     │
     ▼
Enable WorldLock for automatic alignment:
     m_mruk.EnableWorldLock = true
     │
     ▼
Clear any existing scene:
     m_mruk.ClearScene()
     │
     ▼
Create alignment data tuple:
     alignmentData = (
         alignmentRoomUuid: roomUuid,
         floorWorldPoseOnHost: floorPose
     )
     │
     ▼
MRUK.LoadSceneFromSharedRooms(
     roomUuids: [roomUuid],
     groupUuid: groupId,
     alignmentData: alignmentData,
     removeMissingRooms: true
)
     │
     ├── Success → m_roomLoaded = true
     │             OnRoomLoaded?.Invoke()
     │             EnableGlobalMeshColliders()
     │
     └── Failure → OnRoomSharingFailed?.Invoke()
                   EnableGlobalMeshColliders() // Fallback
```

**What LoadSceneFromSharedRooms Does:**
1. Fetches room geometry from OVR Cloud (shared by host)
2. Creates MRUK room anchors in client's scene
3. Uses `alignmentData` to transform room to match host's coordinate frame
4. Result: Both players see room mesh in same virtual positions

### Step 7: Enable Colliders & Complete Startup
**Components:** `RoomSharingMotif`, `GameStartupManagerMotif`

```
EnableGlobalMeshColliders()
     │
     ▼
Get room = MRUK.GetCurrentRoom()
     │
     ▼
For GlobalMeshAnchor:
     │
     ├── Find all MeshFilters
     │
     └── Add/Enable MeshCollider for each
     │
     ▼
For all room.Anchors (walls, floor, furniture):
     │
     └── Enable all Collider components
```

```
GameStartupManagerMotif waits for:
     │
     └── m_roomSharing.IsRoomLoaded == true
          │
          ▼
     CompleteStartup()
          │
          ├── SetState(Ready)
          ├── m_startupComplete = true
          ├── m_progress = 1.0f
          ├── m_statusMessage = "Ready!"
          │
          └── Avatar spawning can proceed
```

---

## Client Data Dependencies Matrix

| Step | Component | Waits For | Provides | Network? |
|------|-----------|-----------|----------|----------|
| 1 | RoleSelectionUI | User input | Session name entry | ❌ |
| 2 | SessionDiscoveryManager | Session name | IsConnecting → IsConnected | ✅ Uses FusionBootstrap |
| 3 | FusionBBEvents | StartSharedClient() | OnConnectedToServer callback | ✅ Yes |
| 4 | SharedSpatialAnchorManager | Network spawn | Colocation GroupID | ✅ Yes |
| 5 | ColocationManager | Anchor localized | Camera alignment | ❌ |
| 6 | RoomSharingMotif | Networked props | Room UUID, Floor pose | ✅ Yes |
| 7 | MRUK | Shared room data | Room mesh, colliders | ✅ Cloud |
| 8 | GameStartupManagerMotif | All above | Avatar spawn gate | ❌ |

---

## Client Timing Sequence

```
T=0.0s    App Start
T=0.5s    Platform Init
T=1.0s    Show Role Selection UI
T=1.5s    Session list refreshed (sees "ShootingGame_A1B2")
T=2.0s    User taps session
T=2.5s    Join network session
T=3.0s    NetworkRunner connected, objects spawn
T=3.5s    SharedSpatialAnchorManager.Spawned() → StartDiscovery()
T=5.5s    (2s delay) → OVRColocationSession.StartDiscoveryAsync()
T=6.0s    Colocation session discovered
T=6.5s    Load unbound shared anchors
T=7.0s    Anchor localized
T=7.5s    ColocationManager.AlignUserToAnchor()
T=8.0s    RoomSharingMotif receives networked properties
T=8.5s    MRUK.LoadSceneFromSharedRooms()
T=10.0s   Room mesh loaded and aligned
T=10.5s   GlobalMesh colliders enabled
T=11.0s   CompleteStartup() → READY
T=11.5s   Avatar can spawn
```

---

## Potential Issues for Client

### ⚠️ Issue 1: 2-Second Delay Before Discovery
In `DiscoverNearbySession()`:
```csharp
await Task.Delay(2000); // Wait for host to advertise
```
This is intentional but could cause issues if host is slow to advertise.

**Risk Level:** Low - Host typically advertises before client joins.

### ⚠️ Issue 2: No Room Scan for Client
Client does NOT scan their own room. They receive the HOST's room mesh.

**Implication:** 
- Client sees host's room geometry, not their own
- This is correct for colocation (both see same virtual space)
- Physical mismatches are expected (real walls won't match virtual walls)

### ⚠️ Issue 3: Fallback on Timeout
If room sharing times out (30s), client uses local room data:
```csharp
if (elapsed > m_colocationWaitTimeout)
{
    Debug.LogWarning("[RoomSharing] Timeout waiting for shared room. Using local room data.");
    EnableGlobalMeshColliders();
}
```

**Risk:** If client loads their OWN room instead of host's, coordinate frames won't match!

### ✅ Check: Client Does NOT Need Spatial Permission for Sharing
Client uses `LoadSceneFromSharedRooms()` which fetches from cloud, not local device. They don't need their own room scanned.

---

## Host vs Client Comparison

| Aspect | Host | Client |
|--------|------|--------|
| Room Scan | ✅ Scans their room BEFORE network | ❌ No scan |
| Network Session | Creates new session | Joins existing |
| Colocation | Advertises session | Discovers session |
| Anchor | Creates & shares | Loads & localizes |
| Camera Alignment | RegisterHostCalibration() (no transform) | AlignUserToAnchor() (transforms rig) |
| Room Mesh | ShareRoomsAsync() to cloud | LoadSceneFromSharedRooms() from cloud |
| Coordination Frame | Defines origin | Aligns to host's origin |

---

## End-to-End Data Flow Summary

```
HOST                                          CLIENT
────                                          ──────
1. Scan Room                                  
   └─ MRUK → Room mesh locally                
                                              
2. Create Session                             
   └─ NetworkRunner → "ShootingGame_A1B2"     
                                              
3. Advertise Colocation                       
   └─ GroupID: abc-123                        
                                              2. Join Session
                         ◄────────────────────   └─ Join "ShootingGame_A1B2"
                                              
                                              3. Discover Colocation
                                                 └─ Found GroupID: abc-123
                                              
4. Create Anchor at host position             
   └─ AnchorUUID: xyz-789                     
   └─ Share to GroupID                        
                                              4. Load & Localize Anchor
                         ◄────────────────────   └─ AnchorUUID: xyz-789
                                                 └─ AlignUserToAnchor()
                                              
5. Share Room                                 
   └─ MRUK.ShareRoomsAsync()                  
   └─ Set Networked Props:                    
      - RoomUUID                              
      - FloorPosition                         
      - FloorRotation                         
      - RoomShared = true                     
                         ────────────────────►
                                              5. Load Shared Room
                                                 └─ Read Networked Props
                                                 └─ MRUK.LoadSceneFromSharedRooms()
                                              
6. READY                                      6. READY
   └─ Avatar Spawned                             └─ Avatar Spawned
```

---

## Recommendations

1. **Simplified approach is working** - FusionBootstrap + FusionBBEvents is reliable and matches Meta's official samples.

2. **Session name sharing** - Host should display session name clearly so client can enter it manually.

3. **Polling over Tasks** - The coroutine polling approach (`while IsConnecting`) is more reliable than async Task.await patterns in Unity.

4. **Add retry logic for LoadSceneFromSharedRooms** - Currently single attempt with fallback to local (which would break colocation).

5. **Add visual feedback during anchor localization** - Currently just waiting with status text.

## Architecture Summary (Simplified)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         NETWORKING LAYER                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  FusionBootstrap (Building Block)                                       │
│  ├── StartMode = Manual                                                  │
│  ├── AutoStartAs = Shared                                                │
│  ├── DefaultRoomName = set by SessionDiscoveryManager                   │
│  └── StartSharedClient() → creates NetworkRunner                        │
│                                                                          │
│  FusionBBEvents (Static Callbacks)                                      │
│  ├── OnConnectedToServer                                                 │
│  ├── OnDisconnectedFromServer                                            │
│  ├── OnConnectFailed                                                     │
│  ├── OnPlayerJoined / OnPlayerLeft                                       │
│  └── OnShutdown                                                          │
│                                                                          │
│  SessionDiscoveryManager (Wrapper)                                       │
│  ├── StartAsHost() → generates name, calls StartSharedClient()          │
│  ├── JoinSession(name) → sets name, calls StartSharedClient()           │
│  ├── IsConnecting / IsConnected → polled by GameStartupManagerMotif    │
│  └── Subscribes to FusionBBEvents for state updates                     │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```



