# Co-Location Arena Shooter - Development Plan

## 1. Project Overview
**Goal:** Create a mixed reality co-location multiplayer shooter where players physically share a space and combat each other (or AI) using interactive physical weapons.
**Tech Stack:** Unity 6, NGO (Netcode for GameObjects), Meta XR SDK (Building Blocks, MRUK).

## 2. Architecture & Networking
- **Colocation:** Uses Meta's `[BuildingBlock] Local Matchmaking` for automatic discovery and anchor sharing.
- **Networking:** Unity NGO for game state synchronization.
- **Room:** MRUK (Meta MR Utility Kit) for room mesh and occlusion.

## 3. Development Phases

### Phase 1: The Weapon System (Completed)
We have transitioned from "magic hand shooting" to "physical weapon pickup".
- [x] **Create `NetworkedGun` Prefab:**
    - Base on `[BuildingBlock] Cube` (NetworkObject, NetworkTransform, Grabbable).
    - Replace Cube mesh with Easy FPS weapon model.
    - Add `NetworkedGunMotif` script.
    - **Status:** ✅ Created `Assets/Prefabs/NetworkedGun.prefab` with all required components.
- [x] **Implement `NetworkedGunMotif` Script:**
    - **Input:** Detect `OVRInteraction` (Grab, Trigger Pull).
    - **Networking:** `ServerRpc` for firing (authoritative).
    - **Feedback:** Play Audio & Muzzle Flash (locally for responsiveness, networked for others).
    - **Ammo:** Track ammo count, handle reloading (gesture or button).
    - **Status:** ✅ Fully implemented in `Assets/Scripts/Shooting/NetworkedGunMotif.cs`.
- [x] **Bullet System:**
    - Reuse/Refine `BulletMotif` for physical projectiles.
    - Ensure collision detection works with MRUK room mesh (walls).
    - **Status:** ✅ Complete with `Assets/Prefabs/Bullet.prefab` and MRUK integration.
- [x] **Registration:**
    - **Status:** ✅ All prefabs registered in `Assets/DefaultNetworkPrefabs.asset`.

### Phase 2: Player Systems ✅ COMPLETE
- [x] **ShootingAvatar.prefab Verification (Task 7):**
    - **Status:** ✅ Verified - contains all essential components (Fusion NetworkObject, NetworkTransform, PlayerHealthMotif, PlayerStatsMotif, ShootingPlayerMotif).
- [x] **PlayerHealthMotif.cs Verification (Task 8):**
    - **Status:** ✅ Complete - implements IDamageable interface, Unity.Netcode NetworkVariable health system, death/respawn mechanics, and feedback systems.
- [x] **PlayerStatsMotif.cs Verification (Task 9):**
    - **Status:** ✅ Complete - comprehensive statistics tracking with 9 NetworkVariable properties for kills, deaths, accuracy, damage, and timing.
- [x] **ShootingPlayerMotif.cs Verification (Task 10):**
    - **Status:** ✅ Complete - comprehensive weapon mechanics with network-synchronized shooting, VR controller input, and visual/audio feedback.
- [x] **PlayerRigMotif.cs Verification (Task 11):**
    - **Status:** ✅ Complete - excellent VR multiplayer rig synchronization with local/remote visual separation and hitbox management.
- [x] **Health System:**
    - Implement `IDamageable` interface on Player.
    - **Status:** ✅ `PlayerHealthMotif.cs` fully implements `IDamageable` and is attached to `ShootingAvatar.prefab`.
    - Networked Health variable (`NetworkVariable<int>`).
    - Visual feedback for taking damage (hit flash, sound effects).
- [x] **Player Rig:**
    - Ensure `NetworkedRigMotif` has hitboxes (Colliders) that move with the headset/controllers.
    - **Crucial:** Separate "Local Player" (invisible head) from "Remote Player" (visible avatar/head).
    - **Status:** ✅ Complete - `PlayerRigMotif.cs` properly implemented with VR rig synchronization and local/remote visual separation.

### Phase 3: Game Loop & Arena Management ✅ COMPLETE
**Arena-Drone-v2 Scene Status:** ✅ **FULLY IMPLEMENTED AND VERIFIED** *(Verified Dec 9, 2024)*

✅ **All arena-drone-wars.prompt.md Requirements Verified:**
- [x] **Task 1: Game Manager Setup** - ShootingGameManagerMotif verified with exact configuration:
  - `minPlayersToStart = 1`, `roundDuration = 180f`, `countdownDuration = 3`, `autoRestart = True`, 
  - `maxHealth = 100`, `bulletDamage = 25`, `bulletSpeed = 50`, `autoRestartDelay = 5f`
- [x] **Task 2: Drone Spawner Setup** - DroneSpawnerMotif verified with wave configuration:
  - `m_baseWaveDroneCount = 3`, `m_droneCountIncrease = 1`, `m_maxLiveDrones = 4`,
  - `m_spawnInterval = 3f`, `m_firstWaveDelay = 5f`, `m_spawnHeightOffset = 1.5f`, `m_spawnDistance = 5f`
- [x] **Task 3: Spawn Manager Setup** - SpawnManagerMotif verified with NetworkedGun prefab reference
- [x] **Task 4: Room Collider Setup** - RoomColliderSetupMotif verified for MRUK bullet physics
- [x] **Task 5: Scoreboard UI** - ScoreboardMotif verified with world-space Canvas configuration
- [x] **Task 6: Connection Manager Setup** - ConnectionManagerMotif verified with NetworkObject
- [x] **Task 7: Player Prefab Verification** - ShootingAvatar.prefab components confirmed from previous sessions
- [x] **Task 8: Gun Prefab Verification** - NetworkedGun.prefab components confirmed from previous sessions

✅ **All 8 Required Building Blocks Verified Present:**
- `[BuildingBlock] Camera Rig`, `[BuildingBlock] Passthrough`, `[BuildingBlock] Network Manager`, 
- `[BuildingBlock] MR Utility Kit`, `[BuildingBlock] Colocation`, `[BuildingBlock] Local Matchmaking`, 
- `[BuildingBlock] OVRInteractionComprehensive`, `[BuildingBlock] Occlusion`

✅ **All Game Systems Verified Configured:**
- `[Game] Shooting Manager` (4 components: NetworkObject, ShootingGameManagerMotif, ShootingGameConfigMotif, ShootingAudioMotif)
- `[Game] Drone Spawner` (2 components: NetworkObject, DroneSpawnerMotif)
- `[Game] Spawn Manager` (2 components: NetworkObject, SpawnManagerMotif)
- `[Environment] Room Colliders` (1 component: RoomColliderSetupMotif)
- `[UI] Scoreboard` (3 components: NetworkObject, ScoreboardMotif, Canvas WorldSpace)
- `[System] Connection Manager` (2 components: ConnectionManagerMotif, NetworkObject)

✅ **All Networking Prefabs Confirmed Registered:**
- ShootingAvatar, NetworkedGun, Bullet, Drone prefabs in DefaultNetworkPrefabs.asset

**✅ Expected Game Flow Fully Implemented:** Scene Load → Player Join → Colocation → Waiting → Countdown → Playing → Round End → Auto-Restart

- [x] **Spawn Manager:**
    - **Player Spawns:** Handled by Co-location (players spawn at 0,0,0 and are aligned to shared anchor).
    - **Weapon Spawns:** Use MRUK to place weapons on tables/furniture automatically.
    - **Status:** ✅ Complete - SpawnManagerMotif configured for MRUK surface detection and weapon placement.
- [x] **Game Manager:**
    - State Machine: `WaitingForPlayers` -> `RoundStart` -> `Gameplay` -> `RoundEnd`.
    - Score Tracking: Kills/Deaths.
    - **Status:** ✅ Complete - ShootingGameManagerMotif handles game loop with verified configuration values.

### Phase 4: Polish & Juice
- [ ] **UI**: World-space canvas for Scoreboard and Timer.
    - **Status**: Created `ScoreboardMotif.cs` and instantiated a "Scoreboard" GameObject in the scene.
- [ ] **Audio**: Spatialized audio for gunshots and footsteps.
    - **Status**: Updated `NetworkedGun.prefab` AudioSource to use 3D spatial blend. `ShootingAudioMotif` handles global sounds.
- [ ] **VFX**: Bullet trails, impact particles (sparks on walls, blood/glitch on players).
    - **Status**: `BulletMotif.prefab` has `BulletHole` and `BloodEffect` assigned.
- [ ] **Hand IK**: (Optional) Snap hands to gun handles for visual fidelity. (Skipped for simplicity)

## 4. Conclusion
The core gameplay loop, networking, and MR features are now implemented.
- **Weapons**: Physical pickup, networked firing, ammo.
- **Players**: Health, damage, death/respawn, visual separation.
- **Arena**: Weapon spawning on tables via MRUK.
- **Game Loop**: Round timer, scoring, scoreboard.

Ready for playtesting and refinement.

## 5. Future Roadmap

### Phase 5: Advanced Combat & Arsenal
- [ ] **Weapon Variety:**
    - **Shotgun:** Short-range, high spread, pump-action reload gesture.
    - **Sniper Rifle:** Long-range, scope rendering (render texture), bolt-action.
    - **Grenades:** Physical throwing mechanics with explosive radius damage.
- [ ] **Power-ups:**
    - Spawn Health Packs and Ammo Crates on MRUK surfaces.
    - Temporary buffs: Speed Boost, Double Damage, Shield.
- [ ] **Melee Combat:**
    - Pistol whipping or dedicated knife weapon.

### Phase 6: Game Modes & Social
- [ ] **Team Support:**
    - Red vs Blue teams.
    - Friendly fire settings.
    - Team-based scoring.
- [ ] **King of the Hill:**
    - Use MRUK to identify a "zone" (e.g., a specific rug or table) that players must hold.
    - Visual feedback for zone control state.
- [ ] **Meta Avatars Integration:**
    - Re-enable `[BuildingBlock] Networked Avatar`.
    - Sync avatar hands with weapon holding.
    - Customization support (colors/hats).

### Phase 7: Immersion & Polish
- [ ] **Destructible Environment:**
    - Virtual cover objects that can be destroyed by gunfire.
    - Debris physics.
- [ ] **Advanced Audio:**
    - Audio occlusion based on MRUK room geometry (walls muffling sound).
    - Reverb zones matching room size.
- [ ] **Haptics:**
    - Nuanced controller vibration for shooting, reloading, and taking damage.
    - Heartbeat haptics when low on health.

## 4. Game Design Decisions
1. **Game Mode:** **PvP (Player vs Player)**.
2. **Reload Mechanic:** **Button Press** (e.g., 'A' or 'X') with a **Reload Timer** (e.g., 2-3 seconds) for realism.
3. **Locomotion:** **1:1 Room Scale Walking** only. No joystick movement to ensure safety in the shared physical space.
