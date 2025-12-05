# Shooting Game Implementation Guide

You are helping implement a **Meta Quest co-location multiplayer shooting game** using Unity 6, Photon Fusion 2, and Meta XR SDK.

---

## Project Context

### What This Project Is
- A **2+ player arena shooter** where players in the **same physical room** can see and shoot each other in mixed reality
- Uses **shared spatial anchors** to align all players to the same coordinate system
- Players spawn at their **physical headset position** (Open Play Area mode)
- Built with **Photon Fusion 2** for networking (Shared Mode)

### Current State
- ✅ Project compiles successfully
- ✅ All core scripts exist and have logic implemented
- ✅ Scene structure is set up with Building Blocks
- ⚠️ Needs configuration and testing
- ⚠️ Some prefab references may need assignment

### Key Files to Know

| File | Purpose |
|------|---------|
| `Assets/Scenes/ShootingGame.unity` | Main scene |
| `Assets/Scripts/Shooting/ShootingGameManagerMotif.cs` | Game state machine |
| `Assets/Scripts/Shooting/PlayerHealthMotif.cs` | Health, damage, death, respawn |
| `Assets/Scripts/Shooting/ShootingPlayerMotif.cs` | Trigger input, bullet spawning |
| `Assets/Scripts/Shooting/BulletMotif.cs` | Networked projectile |
| `Assets/Scripts/Spawning/SpawnManagerMotif.cs` | Open play area spawning |
| `Assets/Scripts/Colocation/ColocationManager.cs` | Camera rig alignment |
| `Assets/Scripts/Colocation/SharedSpatialAnchorManager.cs` | Anchor creation/sharing |

---

## Architecture Overview

### Networking (Photon Fusion 2)
```
NetworkRunner (Shared Mode)
├── Auto Matchmaking → Players auto-join same session
├── NetworkObject prefabs → Spawned per-player
└── RPCs → For damage, game events
```

### Co-location Flow
```
1. Host creates spatial anchor at their position
2. Host advertises anchor via OVRColocationSession
3. Clients discover and load the shared anchor
4. ColocationManager aligns each client's camera rig to the anchor
5. All players now share the same coordinate system
```

### Game Flow
```
WAITING → (2+ players) → COUNTDOWN → (3 sec) → PLAYING → (win condition) → ROUND_END → WAITING
```

### Spawn Flow (Open Play Area)
```
1. Player connects to Fusion session
2. SpawnManagerMotif.GetSpawnLocation() returns headset position
3. Avatar spawns at physical location
4. No teleportation needed - player is already "there"
```

---

## Code Conventions

### Naming
- Classes: `*Motif` suffix (e.g., `ShootingPlayerMotif`)
- Private fields: `m_camelCase` prefix
- Shader properties: `s_propertyName` with `Shader.PropertyToID()`

### Fusion Patterns
```csharp
#if FUSION2
using Fusion;

public class MyNetworkBehaviour : NetworkBehaviour
{
    [Networked] public int SyncedValue { get; set; }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void MyRpc(int value) { }
}
#endif
```

### Meta SDK Patterns
```csharp
// Find components (Unity 6 style)
var cameraRig = FindAnyObjectByType<OVRCameraRig>();

// Spatial anchor async
await anchor.SaveAnchorAsync();
var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, anchors);
```

---

## Implementation Tasks

### If asked to configure prefab references:
1. Use `mcp_ai-game-devel_Scene_GetHierarchy` to find GameObjects
2. Use `mcp_ai-game-devel_Assets_Find` to locate prefabs
3. Use `mcp_ai-game-devel_GameObject_Modify` to assign references

### If asked to fix networking issues:
1. Check `Console_GetLogs` for Fusion errors
2. Verify `PhotonAppSettings` has valid App ID
3. Ensure NetworkObjects have correct `NetworkBehaviour` components
4. Check that RPCs use correct `RpcSources` and `RpcTargets`

### If asked to fix co-location:
1. Verify `OculusPlatformSettings` has valid App ID
2. Check anchor creation mode in `SharedSpatialAnchorManager`
3. Ensure both devices are on same WiFi
4. Verify `ColocationManager` is receiving anchor alignment events

### If asked to add shooting features:
1. Bullets are spawned via `Runner.Spawn()` in `ShootingPlayerMotif`
2. Hit detection is in `BulletMotif.OnCollisionEnter/OnTriggerEnter`
3. Damage is applied via `PlayerHealthMotif.TakeDamageRpc()`
4. Death triggers respawn after `m_respawnDelay` seconds

### If asked to modify game rules:
- Round duration: `ShootingGameManagerMotif.m_roundDuration`
- Kills to win: `ShootingGameManagerMotif.m_killsToWin`
- Min players: `ShootingGameManagerMotif.m_minPlayers`
- Bullet damage: `BulletMotif.m_damage`
- Health: `PlayerHealthMotif.m_maxHealth`
- Respawn delay: `PlayerHealthMotif.m_respawnDelay`

---

## Common Issues & Solutions

### "Players don't see each other"
- Check avatars are spawning (`AvatarSpawnerHandlerMotif`)
- Verify `NetworkedAvatar` building block is configured
- Check `AvatarMovementHandlerMotif` is syncing positions

### "Bullets don't do damage"
- Verify `BulletMotif` has collider set to trigger
- Check `PlayerHealthMotif` has collider on avatar
- Ensure `TakeDamageRpc` is being called (add debug logs)
- Verify bullet's `OwnerId` is set correctly

### "Game doesn't start"
- Check `ShootingGameManagerMotif.m_minPlayers` (default 2)
- Verify players are registered via `RegisterPlayer()`
- Check game state in Inspector

### "Co-location not aligning"
- Both devices must complete Space Setup
- Verify anchor is being created (`SharedSpatialAnchorManager`)
- Check `ColocationManager.OnAlignToAnchor()` is called
- Ensure camera rig transform is being modified

---

## Testing Checklist

Before declaring a feature complete:

1. **Editor Test**: Does it work in Play mode?
2. **Single Device**: Build and run on one Quest
3. **Two Devices**: Test with two Quests on same WiFi
4. **Co-location**: Verify spatial alignment is correct
5. **Edge Cases**: Test disconnect, rejoin, late join

---

## Quick Reference: Key Components in Scene

```
[BuildingBlock] Camera Rig          → OVRCameraRig
[BuildingBlock] Network Manager     → NetworkRunner
[BuildingBlock] Auto Matchmaking    → Auto session join
[BuildingBlock] Networked Avatar    → Avatar spawning
[BuildingBlock] Colocation          → Colocation system

[MR Motif] Arena                    → Reference point (child of Camera Rig)
[MR Motif] Spawn Manager            → SpawnManagerMotif (open play area)
[MR Motif] Shooting Game Manager    → ShootingGameManagerMotif
[MR Motif] Shooting Setup           → ShootingSetupMotif (player config)
[MR Motif] Shooting HUD Canvas      → ShootingHUDMotif

[MR Motifs] Colocation Manager      → ColocationManager
[MR Motifs] SSA Manager             → SharedSpatialAnchorManager
```

---

## When Using Unity MCP Tools

Prefer MCP tools for Unity operations:

```
# Check scene state first
Scene_GetHierarchy → See what exists

# Modify GameObjects
GameObject_Create → Add new objects
GameObject_Modify → Change properties
GameObject_AddComponent → Add components

# Work with assets
Assets_Find → Locate prefabs/materials
Assets_Read → Inspect asset contents
Assets_Prefab_Instantiate → Add prefab to scene

# Validate changes
Console_GetLogs → Check for errors
```

---

## Success Criteria for MVP

The game is complete when:

1. ✅ Two Quest headsets can connect to same session
2. ✅ Players see each other's avatars in correct physical positions
3. ✅ Trigger press fires visible bullets
4. ✅ Bullets hitting players causes damage
5. ✅ Players die and respawn after 3 seconds
6. ✅ First to 10 kills wins the round
7. ✅ Game can restart for another round
