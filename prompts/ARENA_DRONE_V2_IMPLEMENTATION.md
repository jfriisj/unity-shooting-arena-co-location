# Arena-Drone-v2 Scene Implementation Prompt

## 🎯 Objective

Set up and configure the `Arena-Drone-v2` scene as a fully functional co-located MR drone shooting arena following the architecture in `ARCHITECTURE_AND_RESPONSIBILITIES.md` and the development plan in `GAME_DEVELOPMENT_PLAN.md`.

---

## 📋 Pre-Implementation Checklist

Before making changes, verify the following scene objects exist via Unity MCP:

```
Use: mcp_ai-game-devel_Scene_GetHierarchy with loadedSceneName: "Arena-Drone-v2"
```

### Required Building Blocks

| Object Name | Purpose |
|------------|---------|
| `[BuildingBlock] Camera Rig` | OVRCameraRig for head/hand tracking |
| `[BuildingBlock] Passthrough` | MR passthrough layer |
| `[BuildingBlock] Network Manager` | Unity NGO NetworkRunner |
| `[BuildingBlock] MR Utility Kit` | MRUK for room mesh |
| `[BuildingBlock] Colocation` | OVRColocationSession for alignment |
| `[BuildingBlock] Local Matchmaking` | Local network discovery |
| `[BuildingBlock] OVRInteractionComprehensive` | Hand/controller input |
| `[BuildingBlock] Occlusion` | Depth occlusion rendering |

---

## 🏗️ Scene Setup Tasks

### Task 1: Game Manager Setup

Create or verify `[Game] Shooting Manager` GameObject:

**Components Required:**
- `ShootingGameManagerMotif` - Game state machine (WaitingForPlayers → Countdown → Playing → RoundEnd)
- `ShootingGameConfigMotif` - Configuration values
- `ShootingAudioMotif` - Audio clip references
- `NetworkObject` - For NGO synchronization

**Configuration (ShootingGameConfigMotif):**
```csharp
minPlayersToStart = 1        // For solo drone mode
roundDuration = 180f         // 3 minute rounds
countdownDuration = 3        // 3 second countdown
respawnDelay = 3f            // 3 seconds to respawn
maxHealth = 100
bulletDamage = 25f
bulletSpeed = 50f
autoRestart = true
autoRestartDelay = 5f
```

### Task 2: Drone Spawner Setup

Create or verify `[Game] Drone Spawner` GameObject:

**Components Required:**
- `DroneSpawnerMotif` - Wave-based drone spawning
- `NetworkObject` - For NGO synchronization

**Configuration:**
```csharp
m_dronePrefab = "Assets/Prefabs/Drone.prefab"
m_baseWaveDroneCount = 3
m_droneCountIncrease = 1
m_maxLiveDrones = 4
m_spawnInterval = 3f
m_firstWaveDelay = 5f
m_spawnHeightOffset = 1.5f
m_spawnDistance = 5f
```

### Task 3: Spawn Manager Setup

Create or verify `[Game] Spawn Manager` GameObject:

**Components Required:**
- `SpawnManagerMotif` - Weapon spawn point management
- `NetworkObject` - For NGO synchronization

**Configuration:**
```csharp
m_weaponPrefab = "Assets/Prefabs/NetworkedGun.prefab"
m_spawnOnTables = true
m_spawnOnCouches = false
m_maxWeaponsInScene = 4
```

### Task 4: Room Collider Setup

Create or verify `[Environment] Room Colliders` GameObject:

**Components Required:**
- `RoomColliderSetupMotif` - Ensures MRUK mesh has colliders for bullets

### Task 5: Scoreboard UI

Create or verify `[UI] Scoreboard` GameObject:

**Components Required:**
- `ScoreboardMotif` - Displays player scores
- `Canvas` (World Space)
- Position: Above play area (e.g., Vector3(0, 2.5f, 0))

### Task 6: Player Prefab Verification

Verify `Assets/Prefabs/ShootingAvatar.prefab` contains:

| Component | Purpose |
|-----------|---------|
| `NetworkObject` | NGO network identity |
| `NetworkTransform` | Position sync |
| `PlayerHealthMotif` | Health + IDamageable |
| `PlayerStatsMotif` | Kill/Death tracking |
| `ShootingPlayerMotif` | Weapon mechanics |
| `PlayerRigMotif` | Hitbox management |

---

## 📜 Script Responsibilities Reminder

Follow Single Responsibility Principle:

| Script | Does | Does NOT |
|--------|------|----------|
| `ShootingGameManagerMotif` | Game state, timer, win conditions | Damage, spawning |
| `DroneSpawnerMotif` | Wave logic, spawn positions | AI behavior, damage |
| `DroneMotif` | Drone health, identity | Movement, attack |
| `DroneAIMotif` | Chase/attack state machine | Health, spawning |
| `PlayerHealthMotif` | Health, death, respawn | Shooting, movement |
| `ShootingPlayerMotif` | Firing, ammo, muzzle flash | Health, scoring |
| `BulletMotif` | Movement, collision, damage trigger | What takes damage |

---

## 🔧 Implementation Commands

### Step 1: Load Scene and Get Hierarchy
```
Use: mcp_ai-game-devel_Scene_Load with path: "Assets/Scenes/Arena-Drone-v2.unity"
Then: mcp_ai-game-devel_Scene_GetHierarchy with includeChildrenDepth: 3
```

### Step 2: Create Missing GameObjects
For each missing object, use:
```
mcp_ai-game-devel_GameObject_Create or appropriate Asset tools
```

### Step 3: Add Components
For each component, use:
```
mcp_ai-game-devel_GameObject_AddComponent with the component type
mcp_ai-game-devel_GameObject_Modify to set serialized field values
```

### Step 4: Verify Prefab References
Ensure DefaultNetworkPrefabs.asset includes:
- `Assets/Prefabs/ShootingAvatar.prefab`
- `Assets/Prefabs/Drone.prefab`
- `Assets/Prefabs/Bullet.prefab`
- `Assets/Prefabs/NetworkedGun.prefab`

### Step 5: Validate Setup
```
Use: mcp_ai-game-devel_Console_GetLogs to check for errors
```

---

## 🎮 Expected Game Flow

1. **Scene Load**: MRUK scans room, creates mesh colliders
2. **Player Join**: Local Matchmaking discovers nearby devices
3. **Colocation**: Devices align to shared spatial anchor
4. **Waiting State**: Game waits for `minPlayersToStart`
5. **Countdown**: 3-2-1 countdown
6. **Playing**: 
   - Drones spawn in waves
   - Players grab weapons from spawn points
   - Shoot drones for points
   - Avoid drone attacks
7. **Round End**: Timer expires, show scoreboard
8. **Auto-Restart**: After delay, reset for next round

---

## 🐛 Common Issues & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| Bullets pass through walls | No MeshCollider on MRUK mesh | Add `RoomColliderSetupMotif` |
| Drones spawn inside walls | Spawn points not using MRUK bounds | Set `m_spawnDistance` > room size |
| Players can't pick up guns | Missing Grabbable component | Verify `NetworkedGun.prefab` has `Grabbable` |
| Health not syncing | NetworkObject not spawned | Ensure prefab is in DefaultNetworkPrefabs |
| Game stuck in WaitingForPlayers | `minPlayersToStart > 1` | Set to 1 for solo testing |

---

## ✅ Validation Checklist

After setup, verify:

- [ ] Scene loads without console errors
- [ ] MRUK room mesh has colliders (bullets stop at walls)
- [ ] Game transitions from WaitingForPlayers → Countdown → Playing
- [ ] Drones spawn at appropriate positions
- [ ] Weapons spawn on MRUK-detected surfaces
- [ ] Player health displays correctly
- [ ] Scoreboard updates on kills
- [ ] Round ends when timer expires
- [ ] Auto-restart works after round end

---

## 📁 Key File Paths

| Asset | Path |
|-------|------|
| Scene | `Assets/Scenes/Arena-Drone-v2.unity` |
| Player Prefab | `Assets/Prefabs/ShootingAvatar.prefab` |
| Drone Prefab | `Assets/Prefabs/Drone.prefab` |
| Bullet Prefab | `Assets/Prefabs/Bullet.prefab` |
| Gun Prefab | `Assets/Prefabs/NetworkedGun.prefab` |
| Network Prefabs | `Assets/DefaultNetworkPrefabs.asset` |
| Game Config | Script: `ShootingGameConfigMotif` |

---

## 🚀 Quick Start Command

To implement this scene, tell the AI:

> "Set up the Arena-Drone-v2 scene following the ARENA_DRONE_V2_IMPLEMENTATION.md prompt. Load the scene, verify all required GameObjects and components exist, create any missing ones, and configure them according to the specification."
