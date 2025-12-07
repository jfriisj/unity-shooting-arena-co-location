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
