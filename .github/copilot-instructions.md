---
applyTo: '**'
---
# MR Motifs - Copilot Instructions

## ⚠️ CRITICAL: Simplicity First Principle

**Always prioritize simple, easy-to-understand implementations.** When developing features or solving problems:

1. **Keep it simple** - Choose the most straightforward approach that works
2. **Minimize complexity** - Avoid over-engineering; fewer moving parts = fewer bugs
3. **Use existing solutions** - Leverage built-in Unity/Meta SDK features before creating custom ones
4. **Small, focused scripts** - One responsibility per script, short methods (~10-20 lines max)
5. **Avoid premature optimization** - Make it work first, optimize only when needed
6. **Prefer composition over inheritance** - Use components and simple data flow
7. **Clear naming** - Self-documenting code reduces need for comments
8. **No unnecessary abstractions** - Only add layers when there's a proven need

**When in doubt, ask: "Is there a simpler way to do this?"**

## Project Overview
Unity 6 Mixed Reality sample project demonstrating Meta Quest MR features using URP and OpenXR. Contains 4 independent "motifs" (reusable MR patterns) under `Assets/MRMotifs/`.

## AI-Assisted Unity Development (Unity MCP)

This project integrates the **AI Game Developer (Unity-MCP)** tool for direct AI manipulation of Unity Editor. When making changes to GameObjects, scenes, prefabs, or components, prefer using Unity MCP tools over generating code files.

### Key MCP Tools Available
- **Scene/Hierarchy**: `Scene_GetHierarchy`, `GameObject_Create`, `GameObject_Find`, `GameObject_Modify`, `GameObject_AddComponent`
- **Assets**: `Assets_Find`, `Assets_Read`, `Assets_Prefab_Create`, `Assets_Prefab_Instantiate`, `Assets_Material_Create`
- **Scripts**: `Script_CreateOrUpdate`, `Script_Read`, `Script_Execute` (instant Roslyn compilation)
- **Reflection**: `Reflection_MethodFind`, `Reflection_MethodCall` - call any Unity API method directly
- **Editor**: `Editor_GetApplicationInformation`, `Editor_SetApplicationState` (play/pause)
- **Console**: `Console_GetLogs` - check for errors after changes

## Meta Quest Developer Hub MCP (HzOSDevMCP)

This project also integrates **HzOSDevMCP** for Meta Quest device management and documentation access directly from the AI assistant.

### Key HzOSDevMCP Tools Available
- **Documentation**: 
  - `get_unity_documentation_index` - Get latest Unity development docs for Meta Quest
  - `get_unified_documentation_index` - Comprehensive index for all Meta Quest platforms
  - `fetch_meta_quest_doc` - Retrieve full content of specific documentation pages
- **Device Logs**: 
  - `get_device_logcat` - Retrieve historical logcat logs from connected Quest devices
  - `stream_device_logcat` - Real-time streaming of device logs for debugging
- **3D Assets**: 
  - `meta-assets-search` - Search Meta's 3D model library for assets (FBX/GLB)
- **ADB**: 
  - `get_adb_path` - Get the preferred ADB binary path for Quest development

### HzOSDevMCP Workflow Guidelines
1. **Use latest docs**: Always fetch current Meta Quest documentation instead of relying on potentially outdated built-in knowledge
2. **Debug on-device issues**: Use `get_device_logcat` to analyze crashes, performance issues, and system errors
3. **Real-time monitoring**: Use `stream_device_logcat` during active development to catch issues as they occur
4. **Find 3D assets**: Search Meta's asset library before creating custom models

### Workflow Guidelines
1. **Keep it simple** - Use the easiest approach that solves the problem
2. **Check scene state first**: Use `Scene_GetHierarchy` to understand current setup before modifications
3. **Use MCP for GameObject operations**: Create, modify, parent, add components via MCP tools rather than writing scripts
4. **Instant code execution**: Use `Script_Execute` for one-off operations using Roslyn compilation
5. **Validate changes**: Call `Console_GetLogs` after operations to catch errors
6. **Create scripts only when needed**: For persistent behaviors, use `Script_CreateOrUpdate` with proper namespaces

### ⚠️ CRITICAL: MCP Tool Usage Limitation
**Only send ONE MCP tool request at a time.** The Unity MCP server can crash when multiple requests are sent simultaneously. Always wait for one MCP tool call to complete before invoking another. Do NOT call multiple `mcp_ai-game-devel_*` tools in parallel.

### MCP + MR Motifs Example Patterns
```csharp
// When Script_Execute is needed, follow project conventions:
public class Script
{
    public static object Main()
    {
        // Use m_ prefix for local variables matching project style
        var m_cameraRig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
        return $"Found camera rig: {m_cameraRig != null}";
    }
}
```

## Architecture

### Motif Structure
Each motif is self-contained in `Assets/MRMotifs/{MotifName}/` with subfolders:
- `Scripts/` - C# MonoBehaviours, organized by feature
- `Shaders/` - HLSL shaders and ShaderGraph assets
- `Prefabs/` - Reusable prefab configurations
- `Scenes/` - Sample scene files

### Four Core Motifs
1. **PassthroughTransitioning** - VR↔MR fading via `PassthroughFader.cs` and custom shaders
2. **SharedActivities** - Photon Fusion 2 multiplayer with Meta Avatars (requires `#if FUSION2` directive)
3. **InstantContentPlacement** - MRUK Depth API raycasting and depth shaders
4. **ColocatedExperiences** - Spatial Anchors + Colocation Discovery + Space Sharing

### Shared Assets
`Assets/MRMotifs/Shared Assets/` contains cross-motif components:
- `MenuPanel.cs` - Scene navigation and UI controls
- `ConstraintInjectorMotif.cs` - Runtime constraint injection

## Code Conventions

### Naming Patterns
- Public classes use `*Motif` suffix: `AvatarMovementHandlerMotif`, `SpawnManagerMotif`
- Shader files use `*Motif` suffix: `DepthLookingGlassMotif.shader`
- Private fields: `m_camelCase` prefix
- Shader property IDs: `s_propertyName` prefix with `Shader.PropertyToID()`

### Meta SDK Patterns
```csharp
// Passthrough readiness check
m_oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);

// Spatial anchor async operations
await anchor.SaveAnchorAsync();
await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, unboundAnchors);

// Camera rig alignment for colocation
cameraRigTransform.position = anchor.transform.InverseTransformPoint(Vector3.zero);
cameraRigTransform.eulerAngles = new Vector3(0, -anchor.transform.eulerAngles.y, 0);
```

### Multiplayer (Photon Fusion 2)
All networked code wrapped in `#if FUSION2`:
```csharp
[Networked, Capacity(8)]
private NetworkArray<Vector3> AvatarPositions => default;

[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
private void SendPositionAndRotationRpc(int index, Vector3 pos, Quaternion rot) { }
```

### Code Samples Attribute
All sample scripts use Meta's code sample attribute for documentation:
```csharp
[MetaCodeSample("MRMotifs-PassthroughTransitioning")]
public class PassthroughFader : MonoBehaviour
```

## Key Dependencies
- Meta XR Core SDK (`com.meta.xr.sdk.core`) - OVRManager, OVRPassthroughLayer
- Meta MR Utility Kit (`com.meta.xr.mrutilitykit`) - MRUK, EnvironmentRaycastManager
- Meta Avatars SDK (`com.meta.xr.sdk.avatars`) - AvatarEntity, AvatarBehaviourFusion
- Photon Fusion 2 + Voice - NetworkRunner, NetworkBehaviour
- Oculus Interaction SDK - InteractableUnityEventWrapper, RayInteractable, PokeInteractable

## 🏗️ DEFINITIVE: Co-located MR Architecture

### Building Block Options (Choose ONE Approach)

Meta provides TWO independent approaches for co-located multiplayer. **DO NOT MIX THEM:**

#### Option A: Colocation Session + Space Sharing (RECOMMENDED for this project)
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

**Key Flow:**
```
Host: Platform Init → Photon Lobby → Create Anchor → Share Anchor
Client: Platform Init → Find Lobby → Join Photon → Load Anchor → Align
```

### ⚠️ CRITICAL: What NOT to Do

1. **DON'T use Colocation Discovery for matchmaking AND Photon Lobby together** - Pick ONE matchmaking method
2. **DON'T call `MRUK.LoadSceneFromDevice()` on Client** - Client loads from Space Sharing, not device
3. **DON'T skip the groupUuid** - This is what links Host and Client for anchor/room sharing
4. **DON'T expect room mesh without Space Sharing** - Without it, only Host has room collision

### Project-Specific Architecture (Option A)

This project uses **Colocation Session + Space Sharing** because:
- Players are physically co-located (same room)
- Need room mesh collision on ALL devices
- Bluetooth discovery is simpler than lobby management

```
┌─────────────────────────────────────────────────────────────────┐
│                        HOST FLOW                                 │
├─────────────────────────────────────────────────────────────────┤
│ 1. OVRPlatform.Initialize() → Platform ready                    │
│ 2. MRUK.LoadSceneFromDevice() → Room mesh loaded                │
│ 3. OVRColocationSession.StartAdvertisementAsync() → groupUuid   │
│ 4. room.ShareRoomAsync(groupUuid) → Room shared                 │
│ 5. NetworkRunner.StartGame() → Photon session started           │
│ 6. Broadcast floor pose via Networked variable                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                       CLIENT FLOW                                │
├─────────────────────────────────────────────────────────────────┤
│ 1. OVRPlatform.Initialize() → Platform ready                    │
│ 2. OVRColocationSession.StartDiscoveryAsync() → Wait for host   │
│ 3. OnSessionDiscovered → Get groupUuid + metadata               │
│ 4. NetworkRunner.JoinGame() → Join Photon session               │
│ 5. Wait for Networked roomUuid + floorPose from Host            │
│ 6. MRUK.LoadSceneFromSharedRooms(groupUuid, alignmentData)      │
│    → Room mesh + automatic alignment                            │
└─────────────────────────────────────────────────────────────────┘
```

### Networked Variables Required

```csharp
// Host sets these, Client reads them
[Networked] public NetworkString<_512> NetworkedRoomUuid { get; set; }
[Networked] public NetworkString<_256> NetworkedFloorPose { get; set; }
```

### Physics Collision Setup

For room mesh collision to work on ALL devices:
1. Host: MRUK spawns `EffectMesh` with `MeshCollider` from device scan
2. Client: `LoadSceneFromSharedRooms()` spawns same mesh with colliders from shared data
3. Both devices now have identical physics geometry

## Meta Quest Documentation Resources

### Colocation & Multiplayer
- **[Multiplayer Building Blocks](https://developers.meta.com/horizon/documentation/unity/bb-multiplayer-blocks)** - Official guide for Auto/Custom/Local Matchmaking + Colocation blocks
- **[MR Motifs: Colocated Experiences](https://developers.meta.com/horizon/documentation/unity/unity-mrmotifs-colocated-experiences)** - Complete guide to building colocated MR experiences
- **[Colocation Discovery](https://developers.meta.com/horizon/documentation/unity/unity-colocation-discovery)** - `OVRColocationSession` API for Bluetooth-based nearby user discovery
- **[Space Sharing (MRUK)](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-space-sharing)** - Share room mesh between devices via `ShareRoomsAsync`/`LoadSceneFromSharedRooms`
- **[Space Sharing Overview](https://developers.meta.com/horizon/documentation/unity/space-sharing-overview)** - High-level concepts for sharing physical space data

### Spatial Anchors
- **[Shared Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-shared-spatial-anchors)** - Group-based vs User-based anchor sharing
- **[Spatial Anchors: Persist Content](https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-persist-content)** - Save/load spatial anchors for persistent AR content

## Build & Test
- Target: Meta Quest devices (Android)
- Render Pipeline: URP (Universal Render Pipeline Asset in Assets/)
- Entry point: `Assets/MRMotifs/MRMotifsHome.unity`
- Multiplayer testing: Use ParrelSync for multiple editor instances

## Common Patterns to Follow
1. **Simplicity first** - Always choose the simplest solution that works
2. Use `FindAnyObjectByType<T>()` for singleton discovery (Unity 6 pattern)
3. Await spatial anchor operations before proceeding
4. Parent remote avatars to "object of interest" for synchronized movement
5. Use UnityEvents for fade callbacks and interaction hooks
6. Shader render queue `Transparent-1 (2999)` for passthrough spheres with `Cull Off`
7. **Avoid over-engineering** - Don't add complexity unless absolutely necessary
8. **Use groupUuid from Colocation Discovery** - Don't generate your own UUIDs for anchor sharing
9. **Client never scans room** - Always use Space Sharing to load host's room on client

---

# 📚 Example Code Reference Guide

The `example code/MRMotifs/` folder contains Meta's official MR Motif samples. This section provides detailed analysis of each script to help you correctly implement features in this project.

## 🎯 Category 1: Networked Projectiles (For Shooting Arena)

### `BallSpawnerMotif.cs` - Template for Bullet System
**Location:** `example code/MRMotifs/ColocatedExperiences/Scripts/BouncingBall/BallManagerMotif.cs`

**Purpose:** Spawns networked projectiles that all players can see and interact with.

**Key Patterns:**
```csharp
#if FUSION2
public class BallSpawnerMotif : NetworkBehaviour
{
    [SerializeField] private NetworkObject projectilePrefab;  // Must have NetworkObject component
    [SerializeField] private float fireForce = 1.0f;
    [SerializeField] private float lifeTime = 5.0f;
    
    private Transform m_firePoint;
    
    public override void Spawned()
    {
        // Get spawn point from camera (headset direction)
        if (Camera.main != null)
            m_firePoint = Camera.main.transform;
    }
    
    private void Update()
    {
        // Check for trigger press
        if (!OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            return;
            
        // Request authority if needed, then spawn
        if (!Object.HasStateAuthority)
            Object.RequestStateAuthority();
        SpawnProjectile();
    }
    
    private void SpawnProjectile()
    {
        // Runner.Spawn() creates networked object visible to all clients
        var ball = Runner.Spawn(projectilePrefab, m_firePoint.position, m_firePoint.rotation);
        
        // Apply physics impulse
        var rb = ball.GetComponent<Rigidbody>();
        if (rb)
            rb.AddForce(m_firePoint.forward * fireForce, ForceMode.Impulse);
            
        // Auto-despawn after lifetime
        StartCoroutine(DespawnAfterDelay(ball, lifeTime));
    }
    
    private IEnumerator DespawnAfterDelay(NetworkObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj && Runner.IsRunning && obj.HasStateAuthority)
            Runner.Despawn(obj);
    }
}
#endif
```

**How to Adapt for Bullets:**
1. Change `projectilePrefab` to your bullet prefab (must have `NetworkObject` + `Rigidbody`)
2. Increase `fireForce` for faster bullets
3. Decrease `lifeTime` (bullets are faster than balls)
4. Add hit detection via `OnCollisionEnter` in a separate script on the bullet prefab

### `BouncingBallMotif.cs` - Projectile Behavior Script
**Purpose:** Attached to the projectile prefab to handle collision sounds.

```csharp
public class BouncingBallMotif : MonoBehaviour
{
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip ballSound;
    private AudioSource m_audioSource;

    private void Awake()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(spawnSound);  // Play on spawn
    }

    private void OnCollisionEnter(Collision other)
    {
        var impactStrength = other.relativeVelocity.magnitude;
        if (impactStrength > 5.0f)
            m_audioSource.PlayOneShot(ballSound);  // Play on hard impact
    }
}
```

---

## 🎯 Category 2: Colocation & Alignment

### `ColocationManager.cs` - Camera Rig Alignment
**Location:** `example code/MRMotifs/ColocatedExperiences/Scripts/Colocation/ColocationManager.cs`

**Purpose:** Aligns the player's OVRCameraRig to a shared spatial anchor so all players share the same coordinate system.

**Key Patterns:**
```csharp
public class ColocationManager : MonoBehaviour
{
    private Transform m_cameraRigTransform;
    
    private void Awake()
    {
        m_cameraRigTransform = FindAnyObjectByType<OVRCameraRig>().transform;
    }
    
    /// <summary>
    /// Call this when a shared anchor is loaded to align the player.
    /// </summary>
    public void AlignUserToAnchor(OVRSpatialAnchor anchor)
    {
        if (!anchor || !anchor.Localized)
        {
            Debug.LogError("Invalid or un-localized anchor");
            return;
        }
        
        var anchorTransform = anchor.transform;
        
        // The magic formula for camera rig alignment:
        m_cameraRigTransform.position = anchorTransform.InverseTransformPoint(Vector3.zero);
        m_cameraRigTransform.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);
    }
}
```

**When to Use:** After `OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync()` succeeds and anchor is localized.

### `SharedSpatialAnchorManager.cs` - Full Anchor Sharing Flow
**Location:** `example code/MRMotifs/ColocatedExperiences/Scripts/Colocation/SharedSpatialAnchorManager.cs`

**Purpose:** Complete implementation of anchor-based colocation using Bluetooth discovery.

**Host Flow:**
```csharp
// 1. Advertise session via Bluetooth
var result = await OVRColocationSession.StartAdvertisementAsync(null);
m_sharedAnchorGroupId = result.Value;  // Get groupUuid

// 2. Create anchor at host position
var anchor = await CreateAnchor(position, rotation);

// 3. Save anchor locally
await anchor.SaveAnchorAsync();

// 4. Share anchor to group
await OVRSpatialAnchor.ShareAsync(new List<OVRSpatialAnchor> { anchor }, m_sharedAnchorGroupId);
```

**Client Flow:**
```csharp
// 1. Discover nearby session
OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;
await OVRColocationSession.StartDiscoveryAsync();

private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
{
    m_sharedAnchorGroupId = session.AdvertisementUuid;  // Get groupUuid from host
    LoadAndAlignToAnchor(m_sharedAnchorGroupId);
}

// 2. Load shared anchors
var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupUuid, unboundAnchors);

// 3. Localize and bind anchor
foreach (var unbound in unboundAnchors)
{
    if (await unbound.LocalizeAsync())
    {
        var anchorGO = new GameObject("Anchor");
        var spatialAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
        unbound.BindTo(spatialAnchor);
        
        // 4. Align camera rig
        m_colocationManager.AlignUserToAnchor(spatialAnchor);
    }
}
```

**Anchor Placement Modes:**
```csharp
public enum AnchorPlacementMode
{
    AtOrigin,           // Anchor at Vector3.zero
    AtHostPosition,     // Anchor at host's headset position (projected to floor)
    ManualPlacement     // Host presses trigger to confirm anchor location
}
```

### `SpaceSharingManager.cs` - Room Mesh Sharing
**Location:** `example code/MRMotifs/ColocatedExperiences/Scripts/Space Sharing/SpaceSharingManager.cs`

**Purpose:** Shares the host's MRUK room mesh so clients have physics collision.

**Key Patterns:**
```csharp
public class SpaceSharingManager : NetworkBehaviour
{
    // Networked strings to share room data with clients
    [Networked] private NetworkString<_512> NetworkedRoomUuids { get; set; }
    [Networked] private NetworkString<_256> NetworkedRemoteFloorPose { get; set; }
    
    // Host: Share rooms after colocation advertisement
    private async void ShareMrukRooms()
    {
        var rooms = MRUK.Instance.Rooms;
        var result = await MRUK.Instance.ShareRoomsAsync(rooms, m_sharedAnchorGroupId);
        
        // Store UUIDs for clients to load
        NetworkedRoomUuids = string.Join(",", rooms.Select(r => r.Anchor.Uuid));
        
        // Store floor pose for alignment
        var pose = rooms[0].FloorAnchor.transform;
        NetworkedRemoteFloorPose = $"{pose.position.x},{pose.position.y},{pose.position.z},...";
    }
    
    // Client: Load shared rooms
    private async void LoadSharedRoom(Guid groupUuid)
    {
        var roomUuids = NetworkedRoomUuids.ToString().Split(',').Select(Guid.Parse).ToArray();
        var remoteFloorPose = ParsePose(NetworkedRemoteFloorPose.ToString());
        
        // This loads room mesh AND aligns client automatically
        var result = await MRUK.Instance.LoadSceneFromSharedRooms(
            roomUuids, 
            groupUuid, 
            (roomUuids[0], remoteFloorPose)
        );
    }
}
```

**Why Use Space Sharing:**
- Without it, only host has room collision
- `LoadSceneFromSharedRooms()` creates `MeshCollider` on client
- Both players can now collide with walls/floor

---

## 🎯 Category 3: Avatar & Object Synchronization

### `AvatarMovementHandlerMotif.cs` - Networked Avatar Positions
**Location:** `example code/MRMotifs/SharedActivities/Scripts/Avatars/AvatarMovementHandlerMotif.cs`

**Purpose:** Syncs avatar positions relative to an "object of interest" so moving the object moves all avatars.

**Key Patterns:**
```csharp
public class AvatarMovementHandlerMotif : NetworkBehaviour
{
    // Networked arrays for up to 8 players
    [Networked, Capacity(8)]
    private NetworkArray<Vector3> AvatarPositions => default;
    
    [Networked, Capacity(8)]
    private NetworkArray<Quaternion> AvatarRotations => default;
    
    private GameObject m_objectOfInterest;  // Parent object (e.g., chess board)
    private AvatarBehaviourFusion m_localAvatar;
    private readonly List<AvatarBehaviourFusion> m_remoteAvatars = new();
    
    // Send local avatar position as offset from object
    private void SendAvatarOffset()
    {
        var relativePosition = m_objectOfInterest.transform.InverseTransformPoint(m_localAvatar.transform.position);
        var relativeRotation = Quaternion.Inverse(m_objectOfInterest.transform.rotation) * m_localAvatar.transform.rotation;
        
        SendPositionAndRotationRpc(avatarIndex, relativePosition, relativeRotation);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void SendPositionAndRotationRpc(int avatarIndex, Vector3 pos, Quaternion rot)
    {
        AvatarPositions.Set(avatarIndex, pos);
        AvatarRotations.Set(avatarIndex, rot);
    }
    
    // Update remote avatars from networked data
    private void UpdateRemoteAvatars()
    {
        foreach (var remoteAvatar in m_remoteAvatars)
        {
            var index = GetAvatarIndex(remoteAvatar);
            var worldPosition = m_objectOfInterest.transform.TransformPoint(AvatarPositions.Get(index));
            var worldRotation = m_objectOfInterest.transform.rotation * AvatarRotations.Get(index);
            
            remoteAvatar.transform.position = worldPosition;
            remoteAvatar.transform.rotation = worldRotation;
        }
    }
}
```

**Key Insight:** Remote avatars are parented to the object of interest, so moving the object moves all avatars together.

### `ChessBoardHandlerMotif.cs` - Networked Object State
**Location:** `example code/MRMotifs/SharedActivities/Scripts/Chess Sample/ChessBoardHandlerMotif.cs`

**Purpose:** Syncs positions/rotations of multiple grabbable objects (chess pieces).

**Key Patterns:**
```csharp
public class ChessBoardHandlerMotif : NetworkBehaviour, IStateAuthorityChanged
{
    [Networked, Capacity(32)]
    private NetworkArray<Vector3> ChessPiecePositions => default;
    
    [Networked, Capacity(32)]
    private NetworkArray<Quaternion> ChessPieceRotations => default;
    
    private List<InteractableUnityEventWrapper> m_chessPieceInteractables = new();
    
    // When player grabs a piece, request authority
    private void TogglePieceMoved(bool isBeingMoved)
    {
        if (!Object.HasStateAuthority)
        {
            SetChessPieceRigidbodyState(false);  // Disable physics
            Object.RequestStateAuthority();      // Request network authority
            return;
        }
        // ...
    }
    
    // Authority holder updates networked state
    private void SendChessPieceOffset()
    {
        for (var i = 0; i < m_chessPieceInteractables.Count; i++)
        {
            ChessPiecePositions.Set(i, chessPiece.transform.localPosition);
            ChessPieceRotations.Set(i, chessPiece.transform.localRotation);
        }
    }
    
    // Non-authority clients lerp to networked positions
    private void UpdateRemoteChessPieces()
    {
        for (var i = 0; i < m_chessPieceInteractables.Count; i++)
        {
            if (!HasStateAuthority)
            {
                transform.localPosition = Vector3.Lerp(current, target, Time.deltaTime * 10f);
            }
        }
    }
}
```

---

## 🎯 Category 4: Passthrough & VR Transitions

### `PassthroughFader.cs` - VR↔MR Fading
**Location:** `example code/MRMotifs/PassthroughTransitioning/Scripts/PassthroughFader.cs`

**Purpose:** Smooth transition between full VR (skybox) and MR (passthrough).

**Key Patterns:**
```csharp
public class PassthroughFader : MonoBehaviour
{
    private enum PassthroughViewingMode { Underlay, Selective }
    private enum FadeDirection { Normal, RightToLeft, TopToBottom, InsideOut }
    
    [SerializeField] private PassthroughViewingMode passthroughViewingMode;
    [SerializeField] private float selectiveDistance = 5f;  // Sphere radius for Selective mode
    [SerializeField] private float fadeSpeed = 1f;
    
    private OVRPassthroughLayer m_oVRPassthroughLayer;
    private Material m_material;  // Fader sphere material
    private float m_targetAlpha;
    
    private static readonly int s_invertedAlpha = Shader.PropertyToID("_InvertedAlpha");
    
    private void Awake()
    {
        // Disable premultiplied alpha for proper blending
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
        
        m_oVRPassthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
        m_oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);
    }
    
    public void TogglePassthrough()
    {
        if (State == FaderState.MR)
        {
            m_targetAlpha = 0;  // Fade to VR
            onStartFadeOut?.Invoke();
        }
        else if (State == FaderState.VR)
        {
            m_oVRPassthroughLayer.enabled = true;
            onStartFadeIn?.Invoke();
        }
        StartCoroutine(FadeToTarget());
    }
    
    private IEnumerator FadeToTarget()
    {
        var currentAlpha = m_material.GetFloat(s_invertedAlpha);
        while (Mathf.Abs(currentAlpha - m_targetAlpha) > 0.001f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, m_targetAlpha, fadeSpeed * Time.deltaTime);
            m_material.SetFloat(s_invertedAlpha, currentAlpha);
            yield return null;
        }
        
        if (m_targetAlpha == 0)
            m_oVRPassthroughLayer.enabled = false;  // Disable when fully VR
    }
}
```

**Two Viewing Modes:**
- **Underlay:** Entire view fades (camera background changes)
- **Selective:** Sphere around player shows passthrough, outside is VR

---

## 🎯 Category 5: Surface Detection & Placement

### `SurfacePlacementMotif.cs` - Snap to Real Surfaces
**Location:** `example code/MRMotifs/InstantContentPlacement/Scripts/Instant Content Placement/SurfacePlacementMotif.cs`

**Purpose:** Places virtual objects on detected real-world surfaces using Depth API.

**Key Patterns:**
```csharp
public class SurfacePlacementMotif : MonoBehaviour
{
    [SerializeField] private InteractableUnityEventWrapper interactableUnityEventWrapper;
    [SerializeField] private float placementDistance = 0.4f;
    [SerializeField] private float hoverDistance = 0.1f;
    
    private EnvironmentRaycastManager m_raycastManager;  // From MRUK
    
    private void Awake()
    {
        m_raycastManager = FindAnyObjectByType<EnvironmentRaycastManager>();
        
        interactableUnityEventWrapper.WhenSelect.AddListener(OnSelect);
        interactableUnityEventWrapper.WhenUnselect.AddListener(OnUnselect);
    }
    
    private void OnUnselect()
    {
        // When released, try to snap to surface
        if (!PerformRaycastAndSnap())
        {
            // No surface found, stay where dropped
            m_targetPosition = transform.position;
        }
    }
    
    private bool PerformRaycastAndSnap()
    {
        // Raycast downward to find surface
        if (!m_raycastManager.Raycast(new Ray(transform.position, Vector3.down), out var hitInfo))
            return false;
            
        var hitPoint = hitInfo.point;
        
        // Check if within snap distance
        if (Vector3.Distance(transform.position, hitPoint) >= placementDistance)
            return false;
            
        // Set target position (slightly above surface)
        m_targetPosition = new Vector3(hitPoint.x, hitPoint.y + hoverDistance, hitPoint.z);
        m_targetRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        
        StartCoroutine(SmoothMoveToTarget());
        return true;
    }
}
```

**Requires:** `EnvironmentRaycastManager` component in scene (from MRUK).

---

## 🎯 Category 6: Networked Drawing (RPC Pattern)

### `WhiteboardManagerMotif.cs` - RPC for Real-time Sync
**Location:** `example code/MRMotifs/ColocatedExperiences/Scripts/Whiteboard/WhiteboardManagerMotif.cs`

**Purpose:** Demonstrates RPC pattern for immediate visual feedback across all clients.

**Key Patterns:**
```csharp
public class WhiteboardManagerMotif : NetworkBehaviour
{
    public Texture2D whiteboardTexture;
    
    public static WhiteboardManagerMotif Instance { get; private set; }
    
    public override void Spawned()
    {
        Instance = this;
        
        // Create texture
        whiteboardTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // New clients request snapshot of current state
        if (!Object.HasStateAuthority)
            RPC_RequestSnapshot();
    }
    
    /// <summary>
    /// RPC to ALL clients for immediate drawing feedback.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_DrawLine(Vector2 startUV, Vector2 endUV, Color color, int brushRadius)
    {
        DrawLine(startUV, endUV, color, brushRadius);  // Draw locally on all clients
    }
}
```

**RPC Types Used:**
- `RpcTargets.All` - Send to everyone for immediate visual feedback
- `RpcTargets.StateAuthority` - Send to host for authoritative state

### `PointerDrawingMotif.cs` - Using the Whiteboard
**Location:** `example code/MRMotifs/ColocatedExperiences/Scripts/Whiteboard/PointerDrawingMotif.cs`

```csharp
private void HandlePointerDrawing(...)
{
    if (inputDown && InteractionStateManagerMotif.Instance.CanDrawWithPointer())
    {
        Object.RequestStateAuthority();  // Request authority before drawing
        InteractionStateManagerMotif.Instance.SetMode(mode);
        isDrawing = true;
        lastUV = m_whiteboardManagerMotif.WorldToUV(hitPoint);
        m_whiteboardManagerMotif.RPC_DrawLine(lastUV, lastUV, penColor, brushRadius);
    }
    else if (inputHeld && isDrawing)
    {
        var currentUV = m_whiteboardManagerMotif.WorldToUV(hitPoint);
        m_whiteboardManagerMotif.RPC_DrawLine(lastUV, currentUV, penColor, brushRadius);
        lastUV = currentUV;
    }
}
```

---

## 🎯 Quick Reference: Which Script to Copy

| Your Need | Example Script | Key Pattern |
|-----------|---------------|-------------|
| **Spawn bullets** | `BallSpawnerMotif.cs` | `Runner.Spawn()` + `Rigidbody.AddForce()` |
| **Bullet hit detection** | `BouncingBallMotif.cs` | `OnCollisionEnter()` |
| **Align players** | `ColocationManager.cs` | `InverseTransformPoint()` on camera rig |
| **Share spatial anchor** | `SharedSpatialAnchorManager.cs` | `OVRSpatialAnchor.ShareAsync()` |
| **Share room mesh** | `SpaceSharingManager.cs` | `MRUK.ShareRoomsAsync()` |
| **Sync avatar positions** | `AvatarMovementHandlerMotif.cs` | `[Networked] NetworkArray` |
| **Sync grabbable objects** | `ChessBoardHandlerMotif.cs` | `RequestStateAuthority()` on grab |
| **VR↔MR fade** | `PassthroughFader.cs` | Shader alpha + `OVRPassthroughLayer` |
| **Snap to surfaces** | `SurfacePlacementMotif.cs` | `EnvironmentRaycastManager.Raycast()` |
| **Real-time visual sync** | `WhiteboardManagerMotif.cs` | `[Rpc(RpcTargets.All)]` |

---

## 🔧 Implementation Checklist for Shooting Arena

Based on the example code, here's what your shooting arena needs:

### 1. Colocation Setup
- [ ] Copy `ColocationManager.cs` → `Assets/Scripts/Colocation/`
- [ ] Copy `SharedSpatialAnchorManager.cs` → `Assets/Scripts/Colocation/`
- [ ] Copy `SpaceSharingManager.cs` → `Assets/Scripts/Colocation/`

### 2. Bullet System
- [ ] Create `BulletSpawner.cs` based on `BallSpawnerMotif.cs`
- [ ] Create `Bullet.cs` based on `BouncingBallMotif.cs` with hit detection
- [ ] Create bullet prefab with `NetworkObject`, `Rigidbody`, `Collider`

### 3. Player Sync
- [ ] Adapt `AvatarMovementHandlerMotif.cs` for player positions
- [ ] Or use simpler `NetworkTransform` if not using object-relative positions

### 4. Hit Detection
- [ ] Add scoring system
- [ ] Sync hits via RPC (like `WhiteboardManagerMotif.RPC_DrawLine`)

### 5. Room Physics
- [ ] Ensure `MRUK.LoadSceneFromDevice()` on host
- [ ] Ensure `MRUK.LoadSceneFromSharedRooms()` on client
- [ ] Bullets should collide with room walls
