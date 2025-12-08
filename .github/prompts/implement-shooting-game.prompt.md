# Scene Mesh Room Sharing Implementation Guide

This prompt guides the implementation of a robust scene mesh solution for co-located MR experiences.

## Feature Requirements

### Room Sharing
1. **Host scans room** - Host device scans the physical room using MRUK
2. **Guests join shared room** - Guests load the same room mesh for consistent collisions
3. **Room persistence** - Save/load room configurations for future sessions
4. **Large room support** - Handle multiple rooms and large spaces

### Bullet & Player Physics
5. **Bullets collide with walls** - Bullets must detect collision with room mesh (GlobalMesh colliders)
6. **Bullets trigger impact animation** - On wall hit, play impact VFX/animation before destroying bullet
7. **Players collide with walls** - Players cannot walk through walls (character controller respects room mesh)
8. **Player wall impact feedback** - Optional haptic/visual feedback when player touches walls

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         SCENE MESH ARCHITECTURE                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────────┐     ┌──────────────────────┐                  │
│  │ RoomPersistenceManager│←→│  NetworkedRoomManager  │                  │
│  │  - Save room UUIDs     │     │  - Sync room data     │                  │
│  │  - Load saved rooms    │     │  - Share groupUuid    │                  │
│  │  - Handle large rooms  │     │  - Floor pose sync    │                  │
│  └──────────────────────┘     └──────────────────────┘                  │
│                 ↓                         ↓                              │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │                    RoomSharingMotif (Enhanced)            │           │
│  │  - Host: LoadSceneFromDevice → ShareRoomsAsync           │           │
│  │  - Guest: LoadSceneFromSharedRooms (with alignment)      │           │
│  └──────────────────────────────────────────────────────────┘           │
│                 ↓                         ↓                              │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │                    MRUK (Mixed Reality Utility Kit)       │           │
│  │  - Scene anchors, GlobalMesh, Room data                  │           │
│  └──────────────────────────────────────────────────────────┘           │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Phases

### Phase 1: Enhance RoomSharingMotif.cs (Core Room Sharing)

**Location:** `Assets/Scripts/Colocation/RoomSharingMotif.cs`

**Current Issues to Fix:**
- Missing proper alignment data when loading shared rooms
- No floor pose synchronization between host and guest
- Room UUIDs not properly networked to guests

**Required Changes:**

1. **Add Networked Properties for Room Data:**
```csharp
// Room UUIDs (comma-separated for multiple rooms)
[Networked] private NetworkString<_512> NetworkedRoomUuids { get; set; }

// Alignment room UUID (the room used for coordinate alignment)
[Networked] private NetworkString<_64> AlignmentRoomUuid { get; set; }

// Floor anchor pose for alignment (serialized as "posX,posY,posZ,rotX,rotY,rotZ,rotW")
[Networked] private NetworkString<_128> NetworkedFloorPose { get; set; }

// Flag indicating room data is ready for guests
[Networked] private NetworkBool RoomDataReady { get; set; }
```

2. **Host Flow (ShareRoomAsync):**
```csharp
// 1. Load scene from device
await MRUK.Instance.LoadSceneFromDevice();

// 2. Get current room and floor anchor pose
var room = MRUK.Instance.GetCurrentRoom();
var floorPose = new Pose(
    room.FloorAnchor.transform.position,
    room.FloorAnchor.transform.rotation
);

// 3. Share room with group
var result = await MRUK.Instance.ShareRoomsAsync(MRUK.Instance.Rooms, groupUuid);

// 4. Sync data to guests via Networked properties
if (result.Success)
{
    var roomUuids = MRUK.Instance.Rooms.Select(r => r.Anchor.Uuid.ToString());
    NetworkedRoomUuids = string.Join(",", roomUuids);
    AlignmentRoomUuid = room.Anchor.Uuid.ToString();
    NetworkedFloorPose = SerializePose(floorPose);
    RoomDataReady = true;
}
```

3. **Guest Flow (LoadSharedRoomAsync):**
```csharp
// 1. Wait for RoomDataReady flag
while (!RoomDataReady) await Task.Delay(500);

// 2. Parse networked room data
var roomUuids = NetworkedRoomUuids.ToString().Split(',')
    .Select(Guid.Parse).ToList();
var alignmentRoomUuid = Guid.Parse(AlignmentRoomUuid.ToString());
var floorPose = DeserializePose(NetworkedFloorPose.ToString());

// 3. Build alignment data tuple
var alignmentData = (alignmentRoomUuid, floorPose);

// 4. Load shared rooms WITH alignment
var result = await MRUK.Instance.LoadSceneFromSharedRooms(
    roomUuids: roomUuids,
    groupUuid: groupUuid,
    alignmentData: alignmentData,
    removeMissingRooms: true
);
```

4. **Pose Serialization Helpers:**
```csharp
private string SerializePose(Pose pose)
{
    return $"{pose.position.x},{pose.position.y},{pose.position.z}," +
           $"{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}";
}

private Pose DeserializePose(string data)
{
    var parts = data.Split(',').Select(float.Parse).ToArray();
    return new Pose(
        new Vector3(parts[0], parts[1], parts[2]),
        new Quaternion(parts[3], parts[4], parts[5], parts[6])
    );
}
```

---

### Phase 2: Create RoomPersistenceManager.cs

**Location:** `Assets/Scripts/Colocation/RoomPersistenceManager.cs`

**Purpose:** Save and load room configurations for quick session restarts and large room support.

**Key Features:**
- Save room UUIDs to PlayerPrefs or local JSON file
- Meta OS persists actual room mesh data (up to 15 rooms)
- App only stores UUID references for later retrieval

**Implementation:**
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    [Serializable]
    public class SavedRoomData
    {
        public string RoomName;
        public List<string> RoomUuids;
        public string SavedAt;
    }

    [Serializable]
    public class SavedRoomsCollection
    {
        public List<SavedRoomData> Rooms = new List<SavedRoomData>();
    }

    public class RoomPersistenceManager : MonoBehaviour
    {
        private const string SAVED_ROOMS_KEY = "SavedRoomConfigurations";

        /// <summary>
        /// Save current MRUK rooms with a name
        /// </summary>
        public void SaveCurrentRooms(string roomName)
        {
            var rooms = MRUK.Instance?.Rooms;
            if (rooms == null || !rooms.Any())
            {
                Debug.LogWarning("[RoomPersistence] No rooms to save.");
                return;
            }

            var savedRooms = GetAllSavedRooms();
            
            // Remove existing with same name
            savedRooms.Rooms.RemoveAll(r => r.RoomName == roomName);
            
            // Add new entry
            savedRooms.Rooms.Add(new SavedRoomData
            {
                RoomName = roomName,
                RoomUuids = rooms.Select(r => r.Anchor.Uuid.ToString()).ToList(),
                SavedAt = DateTime.UtcNow.ToString("o")
            });

            var json = JsonUtility.ToJson(savedRooms);
            PlayerPrefs.SetString(SAVED_ROOMS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[RoomPersistence] Saved {rooms.Count()} rooms as '{roomName}'");
        }

        /// <summary>
        /// Get list of all saved room configurations
        /// </summary>
        public SavedRoomsCollection GetAllSavedRooms()
        {
            var json = PlayerPrefs.GetString(SAVED_ROOMS_KEY, "{}");
            try
            {
                return JsonUtility.FromJson<SavedRoomsCollection>(json) 
                       ?? new SavedRoomsCollection();
            }
            catch
            {
                return new SavedRoomsCollection();
            }
        }

        /// <summary>
        /// Load a saved room configuration by name
        /// </summary>
        public async Task<bool> LoadSavedRoom(string roomName)
        {
            var savedRooms = GetAllSavedRooms();
            var roomData = savedRooms.Rooms.FirstOrDefault(r => r.RoomName == roomName);
            
            if (roomData == null)
            {
                Debug.LogError($"[RoomPersistence] No saved room found with name: {roomName}");
                return false;
            }

            Debug.Log($"[RoomPersistence] Loading saved room: {roomName} with {roomData.RoomUuids.Count} room(s)");
            
            // Load rooms from device - they should still be persisted by Meta OS
            await MRUK.Instance.LoadSceneFromDevice();
            
            // Verify our saved rooms are still available
            var loadedRoomUuids = MRUK.Instance.Rooms.Select(r => r.Anchor.Uuid.ToString()).ToHashSet();
            var missingRooms = roomData.RoomUuids.Where(u => !loadedRoomUuids.Contains(u)).ToList();
            
            if (missingRooms.Any())
            {
                Debug.LogWarning($"[RoomPersistence] {missingRooms.Count} saved room(s) no longer available. User may need to rescan.");
            }

            return !missingRooms.Any();
        }

        /// <summary>
        /// Delete a saved room configuration
        /// </summary>
        public void DeleteSavedRoom(string roomName)
        {
            var savedRooms = GetAllSavedRooms();
            savedRooms.Rooms.RemoveAll(r => r.RoomName == roomName);
            
            var json = JsonUtility.ToJson(savedRooms);
            PlayerPrefs.SetString(SAVED_ROOMS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[RoomPersistence] Deleted saved room: {roomName}");
        }
    }
}
```

---

### Phase 3: Large Room Support

**Key Considerations:**

1. **Meta OS Room Limits:**
   - Up to 15 rooms can be persisted per device
   - Each room can be scanned independently
   - Rooms are automatically located based on user's current position

2. **Multiple Room Handling:**
```csharp
// Load ALL rooms from device
await MRUK.Instance.LoadSceneFromDevice();

// Get all available rooms
var allRooms = MRUK.Instance.Rooms.ToList();
Debug.Log($"Found {allRooms.Count} rooms on device");

// Share ALL rooms with guests (for large spaces)
var result = await MRUK.Instance.ShareRoomsAsync(allRooms, groupUuid);
```

3. **Room Detection at Runtime:**
```csharp
// Check which room contains a position
public MRUKRoom GetRoomAtPosition(Vector3 worldPosition)
{
    foreach (var room in MRUK.Instance.Rooms)
    {
        if (room.IsPositionInRoom(worldPosition, includeVolumes: true))
        {
            return room;
        }
    }
    return null;
}
```

4. **Large Room Best Practices:**
   - Have both host AND guest walk around the space before sharing/loading
   - If misalignment occurs, clear Physical Space History (Settings > Privacy)
   - Set `MRUK.EnableWorldLock = true` for proper camera alignment

---

### Phase 4: Required Android Manifest Permissions

**Location:** `Assets/Plugins/Android/AndroidManifest.xml`

```xml
<!-- Scene Understanding -->
<uses-feature android:name="com.oculus.feature.PASSTHROUGH" android:required="true" />
<uses-permission android:name="com.oculus.permission.USE_SCENE" />

<!-- Spatial Anchors -->
<uses-permission android:name="com.oculus.permission.USE_ANCHOR_API" />
<uses-permission android:name="com.oculus.permission.IMPORT_EXPORT_IOT_MAP_DATA" />

<!-- Space Sharing (Enhanced Spatial Services) -->
<uses-feature android:name="com.oculus.software.experimental.enhanced_spatial_services" 
              android:required="false" />
```

---

## Testing Checklist

### Room Sharing
- [ ] **Host scans room:** MRUK loads GlobalMesh with colliders
- [ ] **Guest joins:** Receives room mesh via Space Sharing API
- [ ] **Collision test:** Both devices have working physics on room mesh
- [ ] **Alignment test:** Place virtual object on host, verify guest sees it at same physical location
- [ ] **Persistence test:** Save room, exit app, reload - room loads correctly
- [ ] **Large room test:** Scan 2+ connected rooms, verify all share to guests

### Bullet & Player Physics
- [ ] **Bullet-wall collision:** Bullets stop and don't pass through walls
- [ ] **Bullet impact VFX:** Impact animation/particles play on wall hit
- [ ] **Player-wall collision:** Player cannot walk through walls
- [ ] **Network sync:** Bullet impacts are visible on all clients

---

## Phase 5: Bullet & Player Wall Collision

### 5.1 Bullet Wall Collision System

**Location:** `Assets/Scripts/Shooting/BulletWallCollision.cs` (or integrate into existing bullet script)

**Requirements:**
- Bullets must have a Rigidbody and Collider
- Room mesh must have MeshCollider enabled (handled by RoomSharingMotif)
- On collision with wall layer, trigger impact animation and destroy bullet

**Implementation:**
```csharp
using UnityEngine;
using Fusion;

namespace Shooting
{
    public class BulletWallCollision : NetworkBehaviour
    {
        [Header("Impact Settings")]
        [SerializeField] private GameObject m_impactVFXPrefab;
        [SerializeField] private AudioClip m_impactSound;
        [SerializeField] private float m_impactVFXDuration = 1f;
        
        [Header("Collision Layers")]
        [SerializeField] private LayerMask m_wallLayers;  // Include "Default" or custom "Room" layer
        
        private bool m_hasImpacted = false;

        private void OnCollisionEnter(Collision collision)
        {
            // Check if we hit a wall/room mesh
            if (m_hasImpacted) return;
            
            // Check layer mask or tag
            if (((1 << collision.gameObject.layer) & m_wallLayers) != 0 
                || collision.gameObject.CompareTag("RoomMesh"))
            {
                HandleWallImpact(collision);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Alternative if using trigger colliders
            if (m_hasImpacted) return;
            
            if (((1 << other.gameObject.layer) & m_wallLayers) != 0 
                || other.CompareTag("RoomMesh"))
            {
                HandleWallImpact(other.transform.position, -transform.forward);
            }
        }

        private void HandleWallImpact(Collision collision)
        {
            m_hasImpacted = true;
            
            // Get impact point and normal
            var contact = collision.GetContact(0);
            var impactPoint = contact.point;
            var impactNormal = contact.normal;

            SpawnImpactEffect(impactPoint, impactNormal);
            DestroyBullet();
        }

        private void HandleWallImpact(Vector3 point, Vector3 normal)
        {
            m_hasImpacted = true;
            SpawnImpactEffect(point, normal);
            DestroyBullet();
        }

        private void SpawnImpactEffect(Vector3 position, Vector3 normal)
        {
            // Spawn VFX
            if (m_impactVFXPrefab != null)
            {
                var vfx = Instantiate(m_impactVFXPrefab, position, Quaternion.LookRotation(normal));
                Destroy(vfx, m_impactVFXDuration);
            }

            // Play sound
            if (m_impactSound != null)
            {
                AudioSource.PlayClipAtPoint(m_impactSound, position);
            }
        }

        private void DestroyBullet()
        {
            // For networked bullets, use proper despawn
            if (Object != null && Object.HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
```

### 5.2 Player Wall Collision (Character Controller)

**Requirements:**
- Use Unity's CharacterController or Rigidbody-based movement
- Room mesh colliders block player movement automatically
- Add optional haptic feedback when touching walls

**Implementation Notes:**
```csharp
// If using OVRPlayerController or custom movement:
// The CharacterController.Move() automatically respects colliders

// For haptic feedback on wall touch:
public class PlayerWallFeedback : MonoBehaviour
{
    [SerializeField] private float m_hapticDuration = 0.1f;
    [SerializeField] private float m_hapticAmplitude = 0.3f;
    
    private CharacterController m_controller;

    private void Update()
    {
        // CharacterController.collisionFlags tells us what we hit
        if (m_controller != null && 
            (m_controller.collisionFlags & CollisionFlags.Sides) != 0)
        {
            // Player is touching a wall
            TriggerHapticFeedback();
        }
    }

    private void TriggerHapticFeedback()
    {
        // Trigger on both controllers
        OVRInput.SetControllerVibration(m_hapticAmplitude, m_hapticAmplitude, 
                                         OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(m_hapticAmplitude, m_hapticAmplitude, 
                                         OVRInput.Controller.RTouch);
    }
}
```

### 5.3 Room Mesh Layer Setup

**In RoomSharingMotif.cs - EnableGlobalMeshColliders():**
```csharp
private void EnableGlobalMeshColliders()
{
    var room = m_mruk?.GetCurrentRoom();
    if (room == null) return;

    // Set layer for room mesh (create "RoomMesh" layer in Unity)
    int roomMeshLayer = LayerMask.NameToLayer("RoomMesh");
    if (roomMeshLayer < 0) roomMeshLayer = 0; // Fallback to Default

    // GlobalMesh setup
    var globalMeshAnchors = room.GlobalMeshAnchor;
    if (globalMeshAnchors != null)
    {
        var meshFilter = globalMeshAnchors.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Add/enable MeshCollider
            var meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.enabled = true;
            
            // Set layer for collision detection
            meshFilter.gameObject.layer = roomMeshLayer;
            meshFilter.gameObject.tag = "RoomMesh";
            
            Debug.Log("[RoomSharing] GlobalMesh collider enabled on layer: " + 
                      LayerMask.LayerToName(roomMeshLayer));
        }
    }

    // Also setup room anchor colliders (walls, floor, ceiling)
    foreach (var anchor in room.Anchors)
    {
        if (anchor == null) continue;
        anchor.gameObject.layer = roomMeshLayer;
        
        var colliders = anchor.GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            collider.enabled = true;
            collider.gameObject.layer = roomMeshLayer;
        }
    }
}
```

### 5.4 Physics Layer Matrix Setup (Realistic Collisions)

**In Unity Editor: Edit > Project Settings > Physics**

Create layers:
- `Player` (layer 8)
- `Bullet` (layer 9)  
- `RoomMesh` (layer 10)
- `Avatar` (layer 11)

Configure collision matrix for **REALISTIC physics** (all objects collide):
| Layer | Player | Bullet | RoomMesh | Avatar |
|-------|--------|--------|----------|--------|
| Player | ✅ | ✅ | ✅ | ✅ |
| Bullet | ✅ | ✅ | ✅ | ✅ |
| RoomMesh | ✅ | ✅ | ❌ | ✅ |
| Avatar | ✅ | ✅ | ✅ | ✅ |

This ensures **realistic behavior**:
- Bullets collide with walls, avatars, players, AND other bullets (bullet-on-bullet collision)
- Players collide with walls, avatars, and can bump into each other
- Avatars have full collision with everything
- Only RoomMesh-to-RoomMesh is disabled (not needed)

### 5.5 Realistic Bullet Physics Setup

For realistic bullet behavior:
```csharp
// Bullet Prefab Configuration:
// - Rigidbody: useGravity = true (bullets drop realistically)
// - Rigidbody: mass = 0.01 (light projectile)
// - Rigidbody: drag = 0.1 (air resistance)
// - Rigidbody: collisionDetectionMode = ContinuousDynamic (prevent tunneling)
// - SphereCollider or CapsuleCollider: isTrigger = false (physical collision)

[RequireComponent(typeof(Rigidbody))]
public class RealisticBullet : NetworkBehaviour
{
    [Header("Bullet Physics")]
    [SerializeField] private float m_muzzleVelocity = 50f;  // m/s
    [SerializeField] private float m_maxLifetime = 5f;
    [SerializeField] private bool m_useGravity = true;
    
    [Header("Impact Effects")]
    [SerializeField] private GameObject m_wallImpactVFX;
    [SerializeField] private GameObject m_bulletCollisionVFX;  // Bullet-on-bullet
    [SerializeField] private GameObject m_avatarImpactVFX;
    [SerializeField] private AudioClip m_impactSound;
    [SerializeField] private float m_ricochetChance = 0.3f;  // 30% chance to ricochet
    
    private Rigidbody m_rb;
    private float m_spawnTime;
    private bool m_hasImpacted;

    public override void Spawned()
    {
        m_rb = GetComponent<Rigidbody>();
        m_rb.useGravity = m_useGravity;
        m_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        m_spawnTime = Time.time;
        
        // Apply initial velocity
        m_rb.velocity = transform.forward * m_muzzleVelocity;
    }

    public override void FixedUpdateNetwork()
    {
        // Lifetime check
        if (Time.time - m_spawnTime > m_maxLifetime)
        {
            if (Object.HasStateAuthority)
                Runner.Despawn(Object);
        }
        
        // Orient bullet to velocity direction (looks realistic in flight)
        if (m_rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(m_rb.velocity.normalized);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_hasImpacted) return;
        
        var contact = collision.GetContact(0);
        var impactPoint = contact.point;
        var impactNormal = contact.normal;
        var otherLayer = collision.gameObject.layer;

        // Determine what we hit
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Bullet-on-bullet collision!
            HandleBulletCollision(impactPoint, collision);
        }
        else if (collision.gameObject.CompareTag("Avatar") || 
                 collision.gameObject.GetComponentInParent<NetworkObject>() != null)
        {
            // Hit an avatar/player
            HandleAvatarImpact(impactPoint, impactNormal, collision.gameObject);
        }
        else if (collision.gameObject.CompareTag("RoomMesh") || 
                 LayerMask.LayerToName(otherLayer) == "RoomMesh")
        {
            // Hit a wall - maybe ricochet?
            HandleWallImpact(impactPoint, impactNormal, collision);
        }
        else
        {
            // Generic collision
            HandleGenericImpact(impactPoint, impactNormal);
        }
    }

    private void HandleBulletCollision(Vector3 point, Collision collision)
    {
        // Both bullets destroy each other with sparks
        if (m_bulletCollisionVFX != null)
        {
            Instantiate(m_bulletCollisionVFX, point, Quaternion.identity);
        }
        PlayImpactSound(point);
        DestroyBullet();
    }

    private void HandleWallImpact(Vector3 point, Vector3 normal, Collision collision)
    {
        // Check for ricochet
        float impactAngle = Vector3.Angle(-m_rb.velocity.normalized, normal);
        
        if (impactAngle < 30f && Random.value < m_ricochetChance)
        {
            // Ricochet! Reflect velocity
            Vector3 reflectedVelocity = Vector3.Reflect(m_rb.velocity, normal);
            m_rb.velocity = reflectedVelocity * 0.6f;  // Lose some energy
            
            // Spawn ricochet spark
            if (m_wallImpactVFX != null)
            {
                var vfx = Instantiate(m_wallImpactVFX, point, Quaternion.LookRotation(normal));
                vfx.transform.localScale *= 0.5f;  // Smaller spark for ricochet
                Destroy(vfx, 1f);
            }
            PlayImpactSound(point);
            // Don't destroy - bullet continues
        }
        else
        {
            // Full impact - destroy bullet
            m_hasImpacted = true;
            if (m_wallImpactVFX != null)
            {
                var vfx = Instantiate(m_wallImpactVFX, point, Quaternion.LookRotation(normal));
                Destroy(vfx, 2f);
            }
            PlayImpactSound(point);
            DestroyBullet();
        }
    }

    private void HandleAvatarImpact(Vector3 point, Vector3 normal, GameObject target)
    {
        m_hasImpacted = true;
        
        if (m_avatarImpactVFX != null)
        {
            var vfx = Instantiate(m_avatarImpactVFX, point, Quaternion.LookRotation(normal));
            Destroy(vfx, 2f);
        }
        PlayImpactSound(point);
        
        // Notify hit system (your existing hit detection)
        // target.SendMessage("OnBulletHit", this, SendMessageOptions.DontRequireReceiver);
        
        DestroyBullet();
    }

    private void HandleGenericImpact(Vector3 point, Vector3 normal)
    {
        m_hasImpacted = true;
        if (m_wallImpactVFX != null)
        {
            var vfx = Instantiate(m_wallImpactVFX, point, Quaternion.LookRotation(normal));
            Destroy(vfx, 2f);
        }
        PlayImpactSound(point);
        DestroyBullet();
    }

    private void PlayImpactSound(Vector3 position)
    {
        if (m_impactSound != null)
        {
            AudioSource.PlayClipAtPoint(m_impactSound, position);
        }
    }

    private void DestroyBullet()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
```

### 5.6 Player-to-Player Collision

For players to realistically collide with each other:
```csharp
public class PlayerCollisionHandler : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float m_pushForce = 5f;
    [SerializeField] private bool m_enableHapticOnCollision = true;
    
    private CharacterController m_controller;
    private Rigidbody m_rb;

    private void Awake()
    {
        m_controller = GetComponent<CharacterController>();
        m_rb = GetComponent<Rigidbody>();
        
        // If using CharacterController, add a trigger collider for player detection
        var playerCollider = gameObject.AddComponent<CapsuleCollider>();
        playerCollider.isTrigger = true;
        playerCollider.radius = 0.4f;
        playerCollider.height = 1.8f;
        playerCollider.center = new Vector3(0, 0.9f, 0);
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if we're touching another player
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Avatar"))
        {
            // Calculate push direction
            Vector3 pushDirection = (transform.position - other.transform.position).normalized;
            pushDirection.y = 0;  // Keep horizontal
            
            // Apply push via CharacterController
            if (m_controller != null)
            {
                m_controller.Move(pushDirection * m_pushForce * Time.deltaTime);
            }
            
            // Haptic feedback
            if (m_enableHapticOnCollision)
            {
                OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.LTouch);
                OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.RTouch);
            }
        }
    }
}

---

## API Reference (Meta Documentation)

### Key MRUK Functions:

| Function | Host/Guest | Description |
|----------|------------|-------------|
| `LoadSceneFromDevice()` | Host | Load room mesh from device |
| `ShareRoomsAsync(rooms, groupUuid)` | Host | Share room(s) with colocation group |
| `LoadSceneFromSharedRooms(roomUuids, groupUuid, alignmentData)` | Guest | Load shared room with alignment |
| `GetCurrentRoom()` | Both | Get the primary room |
| `Rooms` | Both | Get all loaded rooms |

### Key Properties:

| Property | Type | Description |
|----------|------|-------------|
| `room.FloorAnchor` | Transform | Floor anchor for alignment |
| `room.Anchor.Uuid` | Guid | Unique room identifier |
| `room.GlobalMeshAnchor` | MRUKAnchor | Global mesh with colliders |

---

## Known Issues & Mitigations

1. **Guest fails to load shared room:**
   - Mitigation: Have both devices move around in the shared space before sharing/loading

2. **Room appears misaligned on guest:**
   - Mitigation: Clear Physical Space History on guest device, restart app

3. **Enhanced Spatial Services permission not prompted:**
   - Mitigation: Users must enable manually in Settings > Privacy > Device Permissions

---

## File Summary

| File | Action | Priority |
|------|--------|----------|
| `Assets/Scripts/Colocation/RoomSharingMotif.cs` | Enhance | High |
| `Assets/Scripts/Colocation/RoomPersistenceManager.cs` | Create | Medium |
| `Assets/Scripts/Shooting/BulletWallCollision.cs` | Create/Enhance | High |
| `Assets/Scripts/Shooting/PlayerWallFeedback.cs` | Create | Low |
| `Assets/Plugins/Android/AndroidManifest.xml` | Update | High |
| `ProjectSettings/TagManager.asset` | Update (add layers) | High |
| `ProjectSettings/Physics2DSettings.asset` | Update (collision matrix) | High |

---

## Next Steps

1. Start with Phase 1: Enhance `RoomSharingMotif.cs` with alignment data sync
2. **Phase 5: Add bullet-wall collision to existing bullet prefab**
3. Test basic room sharing between 2 devices
4. Verify bullets stop at walls and play impact animation
5. Implement Phase 2: Room persistence for saved rooms
6. Test large room scenarios with multiple rooms
