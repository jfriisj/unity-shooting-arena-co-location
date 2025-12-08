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

## ✅ Implemented Discover Patterns

This project has adopted key patterns from Meta's Discover sample:

### 1. IDamageable Interface (`Assets/Scripts/Shared/IDamageable.cs`)
Clean damage abstraction allowing any object to take damage uniformly.

```csharp
public interface IDamageable
{
    void TakeDamage(float damage, Vector3 position, Vector3 normal, DamageCallback callback = null);
    void Heal(float healing, DamageCallback callback = null);
    public delegate void DamageCallback(IDamageable affected, float amount, bool died);
}
```

**Usage:** `PlayerHealthMotif` implements `IDamageable`. `BulletMotif` uses `GetComponent<IDamageable>()` instead of specific types.

### 2. PhotonNetworkHelpers (`Assets/Scripts/Network/PhotonNetworkHelpers.cs`)
Utility extensions for common Photon Fusion patterns.

```csharp
// Check if master client
if (PhotonNetworkHelpers.Runner.IsMasterClient()) { }

// Get camera rig
var cameraRig = PhotonNetworkHelpers.CameraRig;

// Despawn object
networkObject.Despawn();

// Check if local player
if (playerRef.IsLocal()) { }
```

### 3. AlignCameraToAnchorManager (`Assets/Scripts/Colocation/AlignCameraToAnchorManager.cs`)
Handles HMD recenter events to maintain colocation alignment.

**Key Feature:** Automatically realigns camera to spatial anchor when user recenters HMD (holds Oculus button) or remounts headset.

**Integration:** Auto-added by `SharedSpatialAnchorManager` after colocation completes.

### 4. PlayerStatsMotif (`Assets/Scripts/Shooting/PlayerStatsMotif.cs`)
Separate stats tracking following single responsibility principle.

```csharp
public class PlayerStatsMotif : NetworkBehaviour
{
    [Networked] public int Kills { get; set; }
    [Networked] public int Deaths { get; set; }
    [Networked] public int ShotsFired { get; set; }
    [Networked] public int ShotsHit { get; set; }
    [Networked] public float DamageDealt { get; set; }
    [Networked] public float DamageTaken { get; set; }
    
    public float Accuracy => ShotsFired > 0 ? (float)ShotsHit / ShotsFired : 0f;
}
```

**Usage:** `PlayerHealthMotif` automatically adds `PlayerStatsMotif` component. Access via `PlayerHealth.PlayerStats.Kills`.

---

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

### Device ID
"C:\Users\jonfriis\Android\Sdk\platform-tools\adb.exe" devices
List of devices attached
2G0YC1ZF8B07WD  device
2G0YC5ZF9F00N1  device

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

---

# 📚 Discover Example Code Reference Guide

The `example code/Discover/` folder contains Meta's official **Discover** sample project with a full **DroneRage** game implementation. This is an excellent reference for a complete shooting game with networking, damage systems, and colocation.

## 🎮 DroneRage: Complete Shooting Game Reference

DroneRage is a wave-based drone shooting game that demonstrates:
- Networked weapons with raycasting
- Damage interfaces and health systems  
- Enemy AI and spawning
- Player stats tracking
- Colocation for multiplayer

---

## 🎯 Category 7: Weapon System (DroneRage)

### `Weapon.cs` - Core Weapon Logic
**Location:** `example code/Discover/DroneRage/Scripts/Weapons/Weapon.cs`

**Purpose:** Base weapon class with configurable spread, damage, knockback, and fire rate.

**Key Patterns:**
```csharp
public class Weapon : MonoBehaviour
{
    public event Action<Vector3, Vector3> WeaponFired;
    public event Action StartedFiring;
    public event Action StoppedFiring;

    [SerializeField] private LayerMask m_raycastLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private Vector2 m_weaponSpread = new(0.01f, 0.05f);
    [SerializeField] private Vector2 m_weaponDamage = new(8f, 14f);
    [SerializeField] private Vector2 m_weaponKnockback = new(10f, 20f);
    [SerializeField] private float m_fireRate = 0.0f;
    [SerializeField] protected Transform m_muzzleTransform;

    public WeaponHitHandler HitHandler { get; set; } = null;
    public IDamageable.DamageCallback DamageCallback = null;

    private bool m_isFiring = false;
    private float m_lastShotTime = 0.0f;

    private void Start()
    {
        HitHandler = new WeaponHitHandler(this);
    }

    public void StartFiring()
    {
        m_isFiring = true;
        StartedFiring?.Invoke();
    }

    public void StopFiring()
    {
        m_isFiring = false;
        StoppedFiring?.Invoke();
    }

    public void Shoot()
    {
        var shotOrigin = m_muzzleTransform.position;
        var shotDir = m_muzzleTransform.TransformDirection(WeaponUtils.RandomSpread(m_weaponSpread));
        
        HitHandler?.ResolveHits(shotOrigin, shotDir);
        WeaponFired?.Invoke(shotOrigin, shotDir);
    }
}
```

**Key Insight:** Uses **raycasting** instead of projectiles for instant hit detection. More performant for fast-firing weapons.

### `WeaponHitHandler` - Raycast Hit Resolution
**Purpose:** Resolves hits from raycast, applies damage and knockback.

```csharp
public class WeaponHitHandler
{
    private Weapon m_weapon;

    public virtual void ResolveHits(Vector3 shotOrigin, Vector3 shotDirection)
    {
        var hits = Physics.RaycastAll(shotOrigin, shotDirection, Mathf.Infinity, 
            m_weapon.RaycastLayers, QueryTriggerInteraction.Ignore);

        if (hits.Length <= 0) return;

        // Find closest hit
        var closestHit = hits[0];
        for (var i = 1; i < hits.Length; ++i)
        {
            if (hits[i].distance < closestHit.distance)
                closestHit = hits[i];
        }

        var hitStrength = Random.Range(0f, 1f);

        // Apply knockback to rigidbody
        if (closestHit.rigidbody)
        {
            closestHit.rigidbody.AddForceAtPosition(
                Mathf.Lerp(m_weapon.WeaponKnockback.x, m_weapon.WeaponKnockback.y, hitStrength) * shotDirection,
                closestHit.point, ForceMode.Impulse);
        }

        // Reduce damage for friendly fire
        var isFriendlyFire = closestHit.transform.GetComponent<Player>() != null;

        // Apply damage to all IDamageable components
        foreach (var damageable in closestHit.transform.GetComponents<IDamageable>())
        {
            var damage = Mathf.Lerp(m_weapon.WeaponDamage.x, m_weapon.WeaponDamage.y, hitStrength);
            if (isFriendlyFire) damage *= 0.2f;
            damageable.TakeDamage(damage, closestHit.point, closestHit.normal, m_weapon.DamageCallback);
        }
    }
}
```

### `IDamageable` - Damage Interface
**Location:** `example code/Discover/DroneRage/Scripts/Weapons/IDamageable.cs`

**Purpose:** Common interface for anything that can take damage.

```csharp
public interface IDamageable
{
    public delegate void DamageCallback(IDamageable damagableAffected, float hpAffected, bool targetDied);

    void Heal(float healing, DamageCallback callback = null);
    void TakeDamage(float damage, Vector3 position, Vector3 normal, DamageCallback callback = null);
}
```

**Why Use Interface:** Both `Player` and `Enemy` implement `IDamageable`, allowing weapons to damage anything without caring what it is.

### `NetworkedWeaponController.cs` - Networked Weapon Sync
**Location:** `example code/Discover/DroneRage/Scripts/Weapons/NetworkedWeaponController.cs`

**Purpose:** Syncs weapon firing across network - only master client resolves hits.

```csharp
public class NetworkedWeaponController : NetworkBehaviour
{
    // Override hit handler to only process on master client
    private class NetworkedWeaponHitHandler : WeaponHitHandler
    {
        public override void ResolveHits(Vector3 shotOrigin, Vector3 shotDirection)
        {
            // ONLY master client resolves hits - prevents double damage
            if (!PhotonNetwork.Runner.IsMasterClient())
                return;

            m_hitHandler.ResolveHits(shotOrigin, shotDirection);
        }
    }

    [Networked(OnChanged = nameof(OnIsFiringChanged))]
    public NetworkBool IsFiring { get; private set; }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void WeaponFiredClientRPC(Vector3 shotOrigin, Vector3 shotDirection)
    {
        // Master resolves hits, everyone plays visuals
        if (DroneRageGameController.Instance.HasStateAuthority)
            m_controlledWeapon.HitHandler.ResolveHits(shotOrigin, shotDirection);

        m_controlledWeaponVisuals.OnWeaponFired(shotOrigin, shotDirection);
    }
}
```

**Key Pattern:** Master client is authoritative for hit resolution. All clients play visual effects.

---

## 🎯 Category 8: Player & Health System (DroneRage)

### `Player.cs` - Full Player Implementation
**Location:** `example code/Discover/DroneRage/Scripts/Player/Player.cs`

**Purpose:** Complete player with networked health, damage, death, and stats tracking.

```csharp
public class Player : NetworkMultiton<Player>, IDamageable
{
    public static Player LocalPlayer;
    public static IEnumerable<Player> LivePlayers => Players.Where(p => p.Health > 0);

    public event Action OnHpChange;
    public event Action OnDeath;

    [AutoSet] public PlayerStats PlayerStats;

    [Networked(OnChanged = nameof(OnHealthChanged))]
    public float Health { get; private set; }

    public void SetupPlayer()
    {
        Object.RequestStateAuthority();
        Health = 100;
        LocalPlayer = this;
    }

    // IDamageable implementation
    public void TakeDamage(float damage, Vector3 position, Vector3 normal, IDamageable.DamageCallback callback = null)
    {
        TakeDamageOwnerRPC(damage, position, normal);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void TakeDamageOwnerRPC(float damage, Vector3 position, Vector3 normal)
    {
        if (!HasStateAuthority) return;
        Health -= damage;
        TakeDamageClientRPC(damage, position, normal);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void TakeDamageClientRPC(float damage, Vector3 position, Vector3 normal)
    {
        PlayerStats.DamageTaken += damage;
        CreateHitFX(damage, position, normal);
    }

    private static void OnHealthChanged(Changed<Player> changed)
    {
        changed.Behaviour.OnHpChange?.Invoke();
        if (changed.Behaviour.Health <= 0f)
            changed.Behaviour.Die();
    }

    private void Die()
    {
        Health = 0;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        OnDeath?.Invoke();
        
        if (PlayersLeft <= 0)
            DroneRageGameController.Instance.TriggerGameOver(false);
    }
}
```

**Key Patterns:**
1. `[Networked(OnChanged = ...)]` for reactive health updates
2. Two-step RPC: `All→StateAuthority` (request damage) then `StateAuthority→All` (apply effects)
3. `NetworkMultiton` pattern for tracking all player instances

### `PlayerStats.cs` - Networked Statistics
**Location:** `example code/Discover/DroneRage/Scripts/Player/PlayerStats.cs`

```csharp
public class PlayerStats : NetworkBehaviour
{
    [Networked] public uint Score { get; set; }
    [Networked] public uint WavesSurvived { get; set; }
    [Networked] public uint ShotsFired { get; set; }
    [Networked] public uint ShotsHit { get; set; }
    [Networked] public uint EnemiesKilled { get; set; }
    [Networked] public float DamageDealt { get; set; }
    [Networked] public float DamageTaken { get; set; }
    [Networked] public float HealingReceived { get; set; }
    [Networked] public ulong TicksSurvived { get; set; }

    public double CalculateAccuracy() => ShotsFired == 0 ? 0 : ShotsHit / (double)ShotsFired;
}
```

---

## 🎯 Category 9: Colocation (Discover)

### `ColocationDriverNetObj.cs` - Automatic Colocation
**Location:** `example code/Discover/Scripts/Colocation/ColocationDriverNetObj.cs`

**Purpose:** Simplified colocation using Meta's `AutomaticColocationLauncher`.

```csharp
public class ColocationDriverNetObj : NetworkBehaviour
{
    public static Action<bool> OnColocationCompletedCallback;

    [SerializeField] private PhotonNetworkData m_networkData;
    [SerializeField] private PhotonNetworkMessenger m_networkMessenger;
    [SerializeField] private GameObject m_anchorPrefab;

    private SharedAnchorManager m_sharedAnchorManager;
    private AutomaticColocationLauncher m_colocationLauncher;

    public override void Spawned()
    {
        Init();
    }

    private async void Init()
    {
        m_ovrCameraRigTransform = FindFirstObjectByType<OVRCameraRig>().transform;
        m_oculusUser = await OculusPlatformUtils.GetLoggedInUser();
        m_playerDeviceUid = OculusPlatformUtils.GetUserDeviceGeneratedUid();
        SetupForColocation();
    }

    private void SetupForColocation()
    {
        m_networkMessenger.RegisterLocalPlayer(m_playerDeviceUid);
        m_sharedAnchorManager = new SharedAnchorManager { AnchorPrefab = m_anchorPrefab };

        NetworkAdapter.SetConfig(m_networkData, m_networkMessenger);

        m_colocationLauncher = new AutomaticColocationLauncher();
        m_colocationLauncher.Init(
            NetworkAdapter.NetworkData,
            NetworkAdapter.NetworkMessenger,
            m_sharedAnchorManager,
            m_ovrCameraRigTransform.gameObject,
            m_playerDeviceUid,
            m_oculusUser?.ID ?? default
        );
        
        m_colocationLauncher.ColocationReady += OnColocationReady;
        m_colocationLauncher.ColocationFailed += OnColocationFailed;
        
        if (HasStateAuthority)
            m_colocationLauncher.CreateColocatedSpace();
        else
            m_colocationLauncher.ColocateAutomatically();
    }

    private void OnColocationReady()
    {
        // Disable per-frame alignment, use only on recenter
        var alignCamBehaviour = FindFirstObjectByType<AlignCameraToAnchor>();
        alignCamBehaviour.enabled = false;
        
        var alignManager = alignCamBehaviour.gameObject.AddComponent<AlignCameraToAnchorManager>();
        alignManager.CameraAlignmentBehaviour = alignCamBehaviour;
        alignManager.RealignToAnchor();

        OnColocationCompletedCallback?.Invoke(true);
    }
}
```

**Key Insight:** Uses `AutomaticColocationLauncher` from `com.meta.xr.colocation` package for simplified setup.

### `AlignCameraToAnchorManager.cs` - Recenter Handling
**Purpose:** Re-aligns camera when user recenters HMD.

```csharp
public class AlignCameraToAnchorManager : MonoBehaviour
{
    public AlignCameraToAnchor CameraAlignmentBehaviour { get; set; }

    private void OnEnable()
    {
        OVRManager.display.RecenteredPose += RealignToAnchor;
        OVRManager.HMDMounted += RealignToAnchor;
    }

    public async void RealignToAnchor()
    {
#if UNITY_EDITOR
        await UniTask.Delay(1000); // Delay for Link
#endif
        CameraAlignmentBehaviour?.RealignToAnchor();
    }
}
```

---

## 🎯 Category 10: Networking Utilities (Discover)

### `NetworkGrabbableObject.cs` - Ownership Transfer on Grab
**Location:** `example code/Discover/Scripts/Networking/NetworkGrabbableObject.cs`

```csharp
[RequireComponent(typeof(Grabbable))]
public class NetworkGrabbableObject : NetworkBehaviour
{
    [AutoSet]
    [SerializeField] private Grabbable m_grabbable;

    private void OnEnable()
    {
        m_grabbable.WhenPointerEventRaised += OnPointerEventRaised;
    }

    private void OnPointerEventRaised(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select)
        {
            if (m_grabbable.SelectingPointsCount == 1)
                TransferOwnershipToLocalPlayer();
        }
    }

    private void TransferOwnershipToLocalPlayer()
    {
        if (!HasStateAuthority)
            Object.RequestStateAuthority();
    }
}
```

### `PhotonNetwork.cs` - Helper Extensions
**Location:** `example code/Discover/Scripts/Networking/PhotonNetwork.cs`

```csharp
public static class PhotonNetwork
{
    private static NetworkRunner s_runner;

    public static NetworkRunner Runner =>
        s_runner != null ? s_runner : (s_runner = NetworkRunner.Instances.FirstOrDefault());

    public static bool IsMasterClient(this NetworkRunner r) =>
        r != null && (r.IsSharedModeMasterClient || r.GameMode is GameMode.Single);

    public static OVRCameraRig CameraRig => AppInteractionController.Instance?.CameraRig;

    public static void Despawn(this NetworkObject obj) => Runner.Despawn(obj);
}
```

---

## 🎯 Category 11: Enemy & Spawning (DroneRage)

### `Spawner.cs` - Wave-Based Enemy Spawning
**Location:** `example code/Discover/DroneRage/Scripts/Enemies/Spawner.cs`

```csharp
public class Spawner : Singleton<Spawner>
{
    public static readonly int[] DronesPerWave = { 1, 8, 6, 21, 21, 28, 20, 0 };
    public static readonly int[] MaxLiveDronesPerWave = { 1, 2, 2, 3, 3, 4, 4, 0 };
    
    public event Action OnWaveAdvance;
    public GameObject DronePrefab;

    public int Wave { get; private set; } = 0;
    private int m_dronesSpawned = 0;
    private int m_liveDrones = 0;

    public void LOGDroneKill()
    {
        --m_liveDrones;
        if (m_liveDrones <= 0 && m_dronesSpawned >= DronesPerWave[Wave])
            AdvanceWave();
    }

    public Vector3 GetRandomSpawnPoint(Player targetPlayer)
    {
        var spawnOffset = targetPlayer.transform.forward;
        spawnOffset.y = 0f;
        spawnOffset = spawnOffset.normalized;

        var angle = Random.Range(-180f, 180f);
        spawnOffset = Quaternion.AngleAxis(angle, Vector3.up) * spawnOffset;
        return 2f * (RoomSize.magnitude + 1f) * spawnOffset + new Vector3(0f, m_roomMaxExtent.y - 1f, 0f);
    }
}
```

---

## 🎯 Updated Quick Reference: Which Script to Copy

| Your Need | Example Script | Location | Key Pattern |
|-----------|---------------|----------|-------------|
| **Raycast weapon** | `Weapon.cs` | Discover/DroneRage | `Physics.RaycastAll()` + `IDamageable` |
| **Networked weapon** | `NetworkedWeaponController.cs` | Discover/DroneRage | Master client resolves hits |
| **Damage interface** | `IDamageable.cs` | Discover/DroneRage | Common interface for damage |
| **Player health** | `Player.cs` | Discover/DroneRage | `[Networked] Health` + two-step RPC |
| **Player stats** | `PlayerStats.cs` | Discover/DroneRage | All `[Networked]` properties |
| **Auto colocation** | `ColocationDriverNetObj.cs` | Discover | `AutomaticColocationLauncher` |
| **Recenter handling** | `AlignCameraToAnchorManager.cs` | Discover | `OVRManager.display.RecenteredPose` |
| **Grab ownership** | `NetworkGrabbableObject.cs` | Discover | `RequestStateAuthority()` on select |
| **Wave spawning** | `Spawner.cs` | Discover/DroneRage | Wave arrays + spawn points |
| **Spawn bullets** | `BallSpawnerMotif.cs` | MRMotifs | `Runner.Spawn()` + `Rigidbody.AddForce()` |
| **Share room mesh** | `SpaceSharingManager.cs` | MRMotifs | `MRUK.ShareRoomsAsync()` |

---

## 🔧 Raycast vs Projectile: When to Use Each

### Raycast (DroneRage approach)
**Pros:**
- Instant hit detection
- No network sync needed for bullet objects
- More performant (no physics simulation)
- Better for fast-firing weapons

**Cons:**
- No visible bullet travel
- Harder to dodge

**Use When:** Fast-firing weapons, performance critical, short-range

### Projectile (MRMotifs approach)
**Pros:**
- Visible bullet travel
- Can be dodged
- More satisfying for slower weapons
- Physical interactions with world

**Cons:**
- Requires `Runner.Spawn()` for each bullet
- Physics simulation cost
- Need to sync across network

**Use When:** Slower weapons, player-vs-player (dodging matters), want physical bullet behavior

---

## 🔧 Updated Implementation Checklist

Based on BOTH example codebases:

### Option A: Raycast Weapons (DroneRage style)
- [ ] Create `IDamageable` interface
- [ ] Create `Weapon.cs` with configurable spread/damage/knockback
- [ ] Create `NetworkedWeaponController.cs` for network sync
- [ ] Implement `IDamageable` on `PlayerHealthMotif`
- [ ] Master client resolves all hits

### Option B: Projectile Weapons (MRMotifs style)
- [ ] Keep current `BulletMotif.cs` with `Runner.Spawn()`
- [ ] Keep `ShootingPlayerMotif.cs` for spawning
- [ ] Sync bullet impacts via RPC

### Colocation (Choose One)
- [ ] **Simple:** Use `AutomaticColocationLauncher` (Discover style)
- [ ] **Custom:** Use `OVRColocationSession` + Space Sharing (MRMotifs style)
