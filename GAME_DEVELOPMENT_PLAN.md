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
- [ ] **Create `NetworkedGun` Prefab:**
    - Base on `[BuildingBlock] Cube` (NetworkObject, NetworkTransform, Grabbable).
    - Replace Cube mesh with Easy FPS weapon model.
    - Add `NetworkedGunMotif` script.
    - **Status:** Created `Assets/Prefabs/NetworkedGun.prefab`.
- [ ] **Implement `NetworkedGunMotif` Script:**
    - **Input:** Detect `OVRInteraction` (Grab, Trigger Pull).
    - **Networking:** `ServerRpc` for firing (authoritative).
    - **Feedback:** Play Audio & Muzzle Flash (locally for responsiveness, networked for others).
    - **Ammo:** Track ammo count, handle reloading (gesture or button).
    - **Status:** Implemented in `Assets/Scripts/Shooting/NetworkedGunMotif.cs`.
- [ ] **Bullet System:**
    - Reuse/Refine `BulletMotif` for physical projectiles.
    - Ensure collision detection works with MRUK room mesh (walls).
    - **Status:** Using `Assets/Prefabs/Shooting/BulletMotif.prefab`.
- [ ] **Registration:**
    - Registered `Assets/Prefabs/NetworkedGun.prefab` in `Assets/DefaultNetworkPrefabs.asset`.

### Phase 2: Player Systems (Current Priority)
- [ ] **Health System:**
    - Implement `IDamageable` interface on Player.
    - **Status:** `PlayerHealthMotif.cs` implements `IDamageable` and is attached to `ShootingAvatar.prefab`.
    - Networked Health variable (`NetworkVariable<float>`).
    - Visual feedback for taking damage (red vignette, sound).
- [ ] **Player Rig:**
    - Ensure `NetworkedRigMotif` has hitboxes (Colliders) that move with the headset/controllers.
    - **Crucial:** Separate "Local Player" (invisible head) from "Remote Player" (visible avatar/head).
    - **Status:** Created `PlayerRigMotif.cs` and updated `NetworkedRigMotif.prefab` with HeadVisuals (Sphere+Collider) and NetworkTransform.

### Phase 3: Game Loop & Arena Management
- [ ] **Spawn Manager:**
    - **Player Spawns:** Handled by Co-location (players spawn at 0,0,0 and are aligned to shared anchor).
    - **Weapon Spawns:** Use MRUK to place weapons on tables/furniture automatically.
    - **Status:** Implemented `SpawnManagerMotif.cs` which uses MRUK to find tables/couches and spawn `NetworkedGun` prefabs.
- [ ] **Game Manager:**
    - State Machine: `WaitingForPlayers` -> `RoundStart` -> `Gameplay` -> `RoundEnd`.
    - Score Tracking: Kills/Deaths.
    - **Status:** `ShootingGameManagerMotif.cs` handles the game loop and score tracking. Verified configuration on `[Game] Shooting Manager`.

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
