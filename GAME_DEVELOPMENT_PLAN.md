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
    - **Status:** Created `Assets/Prefabs/NetworkedGun.prefab`.
- [x] **Implement `NetworkedGunMotif` Script:**
    - **Input:** Detect `OVRInteraction` (Grab, Trigger Pull).
    - **Networking:** `ServerRpc` for firing (authoritative).
    - **Feedback:** Play Audio & Muzzle Flash (locally for responsiveness, networked for others).
    - **Ammo:** Track ammo count, handle reloading (gesture or button).
    - **Status:** Implemented in `Assets/Scripts/Shooting/NetworkedGunMotif.cs`.
- [x] **Bullet System:**
    - Reuse/Refine `BulletMotif` for physical projectiles.
    - Ensure collision detection works with MRUK room mesh (walls).
    - **Status:** Using `Assets/Prefabs/Shooting/BulletMotif.prefab`.
- [x] **Registration:**
    - Registered `Assets/Prefabs/NetworkedGun.prefab` in `Assets/DefaultNetworkPrefabs.asset`.

### Phase 2: Player Systems (Current Priority)
- [x] **Health System:**
    - Implement `IDamageable` interface on Player.
    - **Status:** `PlayerHealthMotif.cs` implements `IDamageable` and is attached to `ShootingAvatar.prefab`.
    - Networked Health variable (`NetworkVariable<float>`).
    - Visual feedback for taking damage (red vignette, sound).
- [ ] **Player Rig:**
    - Ensure `NetworkedRigMotif` has hitboxes (Colliders) that move with the headset/controllers.
    - **Crucial:** Separate "Local Player" (invisible head) from "Remote Player" (visible avatar/head).

### Phase 3: Game Loop & Arena Management
- [ ] **Spawn Manager:**
    - **Player Spawns:** Define spawn points in the room (or use MRUK to find open spaces).
    - **Weapon Spawns:** Use MRUK to place weapons on tables/furniture automatically.
- [ ] **Game Manager:**
    - State Machine: `WaitingForPlayers` -> `RoundStart` -> `Gameplay` -> `RoundEnd`.
    - Score Tracking: Kills/Deaths.
    - UI: World-space canvas for Scoreboard and Timer.

### Phase 4: Polish & Juice
- [ ] **Audio:** Spatialized audio for gunshots and footsteps.
- [ ] **VFX:** Bullet trails, impact particles (sparks on walls, blood/glitch on players).
- [ ] **Hand IK:** (Optional) Snap hands to gun handles for visual fidelity.

## 4. Game Design Decisions
1. **Game Mode:** **PvP (Player vs Player)**.
2. **Reload Mechanic:** **Button Press** (e.g., 'A' or 'X') with a **Reload Timer** (e.g., 2-3 seconds) for realism.
3. **Locomotion:** **1:1 Room Scale Walking** only. No joystick movement to ensure safety in the shared physical space.
