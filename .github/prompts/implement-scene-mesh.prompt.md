---
agent: agent
---

# Implement Scene Mesh Sharing for Co-located MR Experience

## Task Overview
Implement scene mesh sharing to allow co-located players to share room scan data, enabling both physics collisions and visual consistency across headsets in the same physical space.

## Background Context

### Current Project State
- **Unity 6** project with **Photon Fusion 2** (Shared Mode) networking
- **Meta XR SDK** with MRUK, Shared Spatial Anchors, Colocation Discovery
- Scene: `Assets/Scenes/ShootingGame.unity`
- Target: Meta Quest 3 devices (co-located in same physical room)

### Relevant Files
| File | Purpose |
|------|---------|
| `Assets/Scripts/Colocation/RoomSharingMotif.cs` | Room mesh sharing (currently experimental, DISABLED in scene) |
| `Assets/Scripts/Colocation/SharedSpatialAnchorManager.cs` | Colocation anchor creation/sharing |
| `Assets/Scripts/Colocation/ColocationManager.cs` | Camera rig alignment & calibration tracking |

### Current Scene Objects (Colocation-related)
| Object | Status | Purpose |
|--------|--------|---------|
| `[BuildingBlock] MR Utility Kit` | Active | MRUK for room scanning/Scene API |
| `[BuildingBlock] Colocation` | Active | OVR Colocation building block |
| `[BuildingBlock] Scene Mesh` | **DISABLED** | Local scene mesh (conflicts with shared approach) |
| `[MR Motifs] SSA Manager` | Active | SharedSpatialAnchorManager |
| `[MR Motifs] Colocation Manager` | Active | ColocationManager |
| `[MR Motif] Room Sharing` | **DISABLED** | RoomSharingMotif (needs fixing) |

---

## Requirements

### Functional Requirements
1. **Host scans and shares room mesh** - When the master client (host) joins, their room scan data is shared with all guests
2. **Guests load shared room** - Guest devices load the host's room data instead of using their own local scan
3. **Coordinate frame alignment** - All players see virtual content in the same physical locations
4. **Physics collisions work** - GlobalMesh colliders are enabled on both host and guest for arena physics
5. **Visuals match** - Room geometry (walls, floor, furniture) renders consistently across devices
6. **Persistence** - Room sharing data persists across app restarts (players can rejoin)

### Technical Requirements
1. Use **MRUK Space Sharing API** (`ShareRoomsAsync` / `LoadSceneFromSharedRooms`)
2. Use **Photon Fusion 2 Shared Mode** for networking room data (group ID, room UUIDs, floor pose)
3. Enable **MRUK.EnableWorldLock** for automatic camera alignment on guests
4. Pass proper **alignmentData** to `LoadSceneFromSharedRooms` including floor pose
5. Keep `[BuildingBlock] Scene Mesh` DISABLED (we use shared mesh, not local)
6. Enable `[MR Motif] Room Sharing` after fixing the script

---

## Implementation Architecture

### Host Flow
```
1. MRUK.LoadSceneFromDevice() → Room is scanned/loaded locally
2. OVRColocationSession.StartAdvertisementAsync() → Get groupUuid
3. MRUK.ShareRoomsAsync(rooms, groupUuid) → Share room with group
4. Sync via Fusion: {groupUuid, roomUuid, floorPosition, floorRotation}
5. EnableGlobalMeshColliders() → Physics ready
```

### Guest Flow
```
1. OVRColocationSession.StartDiscoveryAsync() → Find nearby session
2. Receive alignment data via Fusion networked properties
3. MRUK.EnableWorldLock = true
4. MRUK.LoadSceneFromSharedRooms(roomUuids, groupUuid, alignmentData)
5. EnableGlobalMeshColliders() → Physics ready
```

### Alignment Data Structure
The guest needs to receive from host:
- `Guid groupUuid` - The colocation session ID (already shared via advertisement)
- `Guid roomUuid` - The UUID of the room anchor to load
- `Pose floorWorldPoseOnHost` - Position and rotation of floor anchor on host device

---

## API Reference (Meta MRUK Space Sharing)

### ShareRoomsAsync (Host)
```csharp
public OVRTask<OVRResult<OVRAnchor.ShareResult>> ShareRoomsAsync(
    IEnumerable<MRUKRoom> rooms,
    Guid groupUuid)
```

### LoadSceneFromSharedRooms (Guest)
```csharp
public async Task<LoadDeviceResult> LoadSceneFromSharedRooms(
    IEnumerable<Guid> roomUuids, 
    Guid groupUuid, 
    (Guid alignmentRoomUuid, Pose floorWorldPoseOnHost)? alignmentData, 
    bool removeMissingRooms = true)
```

### Key Properties
- `MRUK.Instance.EnableWorldLock` - Set to `true` on guest for automatic camera alignment
- `MRUKRoom.FloorAnchor.transform` - Get position/rotation for alignment data
- `MRUKRoom.Anchor.Uuid` - Get room UUID to share with guests

---

## Code Changes Required

### 1. Update RoomSharingMotif.cs

**Current Issues:**
- Uses reflection to access private fields (fragile)
- Doesn't pass `alignmentData` to `LoadSceneFromSharedRooms`
- Missing proper room UUID synchronization

**Required Changes:**

```csharp
// Add these networked properties:
[Networked] private Vector3 HostFloorPosition { get; set; }
[Networked] private Quaternion HostFloorRotation { get; set; }
[Networked] private NetworkString<_64> RoomUuidString { get; set; }
[Networked] private NetworkBool RoomShared { get; set; }

// Host: After room is shared
var room = MRUK.Instance.GetCurrentRoom();
var floorAnchor = room.FloorAnchor;

HostFloorPosition = floorAnchor.transform.position;
HostFloorRotation = floorAnchor.transform.rotation;
RoomUuidString = room.Anchor.Uuid.ToString();

await MRUK.Instance.ShareRoomsAsync(new[] { room }, groupId);
RoomShared = true;

// Guest: Load with alignment
MRUK.Instance.EnableWorldLock = true;
var roomUuid = Guid.Parse(RoomUuidString.ToString());
var alignmentData = (
    alignmentRoomUuid: roomUuid,
    floorWorldPoseOnHost: new Pose(HostFloorPosition, HostFloorRotation)
);
await MRUK.Instance.LoadSceneFromSharedRooms(new[] { roomUuid }, groupId, alignmentData);
```

### 2. Update SharedSpatialAnchorManager.cs

**Required Change:**
- Expose `m_sharedAnchorGroupId` publicly so `RoomSharingMotif` can access it
- Add a public property or event when colocation session is established

```csharp
public Guid SharedAnchorGroupId => m_sharedAnchorGroupId;
public event Action<Guid> OnColocationSessionEstablished;
```

### 3. Scene Setup (via MCP or manual)
- Enable `[MR Motif] Room Sharing` GameObject
- Keep `[BuildingBlock] Scene Mesh` disabled
- Verify MRUK is configured with GlobalMesh enabled

---

## Success Criteria

### Functional Tests
- [ ] Host joins → room is scanned and MRUK room loads
- [ ] Guest joins → discovers host session via Bluetooth
- [ ] Guest receives room data → room appears aligned with physical space
- [ ] Physics works → bullets/objects collide with room geometry on both devices
- [ ] Visuals match → virtual content appears in same physical positions
- [ ] App restart → can rejoin same shared space

### Technical Validation
- [ ] No reflection used (clean API access)
- [ ] `alignmentData` passed correctly to `LoadSceneFromSharedRooms`
- [ ] `EnableWorldLock = true` set before loading on guest
- [ ] GlobalMesh colliders enabled on both host and guest
- [ ] No errors in Unity console related to room sharing

### Performance
- [ ] Room sharing completes within 10 seconds of guest joining
- [ ] No noticeable frame drops during mesh loading

---

## Known Issues & Mitigations

### From Meta Documentation:
1. **Enhanced Spatial Services permission** - Must be enabled manually on devices before testing
   - Settings > Privacy > Device Permissions > Share Point Cloud Data
   
2. **Guests should move around** before loading - Helps improve spatial localization accuracy

3. **Misalignment issues** - If room appears offset on guest:
   - Clear Physical Space History on guest device
   - Restart app and reload

4. **LoadSceneFromSharedRooms may fail silently** - Both devices should move around the physical space for better mapping before sharing/loading

---

## Testing Devices
- H1: `2G0YC1ZF8B07WD` (H_4193)
- H2: `2G0YC5ZF9F00N1` (H_6444)

---

## References
- [Meta Space Sharing Overview](https://developers.meta.com/horizon/documentation/unity/space-sharing-overview)
- [Unity Space Sharing API](https://developers.meta.com/horizon/documentation/unity/unity-space-sharing)
- [Space Sharing Sample on GitHub](https://github.com/oculus-samples/Unity-SpaceSharing)
- [Colocation Tips and FAQs](https://developers.meta.com/horizon/documentation/unity/unity-colocation-tips-tricks-faq)