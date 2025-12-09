# Architecture & Responsibility Documentation

This document outlines the responsibilities of the key scripts in the project to ensure adherence to the **Single Responsibility Principle (SRP)**. The architecture follows a component-based "Motif" pattern, where complex behaviors are composed of smaller, focused scripts.

## 🎯 Shooting Mechanics (`Assets/Scripts/Shooting/`)

These scripts handle the core gameplay loop, weapon mechanics, and player state.

| Script | Responsibility | SRP Analysis |
| :--- | :--- | :--- |
| **`ShootingPlayerMotif.cs`** | Handles **weapon mechanics** only: input detection, bullet spawning, and local weapon visual effects (muzzle flash, recoil). | ✅ **Clean.** Does not handle health, stats, or UI. |
| **`PlayerHealthMotif.cs`** | Manages **health state**: taking damage (via `IDamageable`), dying, and respawning. | ✅ **Clean.** Decoupled from the source of damage (bullets/explosions). |
| **`PlayerStatsMotif.cs`** | A **data container** for networked statistics (Kills, Deaths, Accuracy). | ✅ **Clean.** Pure data holder; logic for *how* to get kills is elsewhere. |
| **`BulletMotif.cs`** | Handles **projectile physics**: movement, collision detection, and triggering damage on targets. | ✅ **Clean.** Self-contained projectile logic. |
| **`ShootingGameManagerMotif.cs`** | Manages **game rules**: round timer, game states (Waiting, Playing, Ended), and win conditions. | ✅ **Clean.** Acts as the central referee/orchestrator. |
| **`ShootingHUDMotif.cs`** | Manages **local player UI**: health bar, crosshair, and ammo display. | ✅ **Clean.** Listens to events from Health/Stats scripts to update UI. |
| **`ShootingSetupMotif.cs`** | Handles **initialization**: attaches shooting components to avatars when they spawn. | ✅ **Clean.** Separates setup logic from runtime logic. |
| **`ShootingGameConfigMotif.cs`** | Central **configuration**: holds tunable values (damage, speed, health) to avoid magic numbers. | ✅ **Clean.** Pure data configuration. |
| **`ShootingAudioMotif.cs`** | Central **audio registry**: holds references to audio clips and provides access to them. | ✅ **Clean.** Decouples asset management from logic. |
| **`DroneMotif.cs`** | Represents the **Drone Entity**: holds health and identity. | ✅ **Clean.** Separated from AI logic. |
| **`DroneAIMotif.cs`** | Handles **Drone Behavior**: state machine (Idle, Chase, Attack) and movement. | ✅ **Clean.** Controls the `DroneMotif` but is a separate component. |
| **`DroneSpawnerMotif.cs`** | Manages **enemy waves**: spawns drones at intervals on the Master Client. | ✅ **Clean.** Handles spawning logic, separated from drone behavior. |
| **`RoomColliderSetupMotif.cs`** | Manages **environment physics**: ensures the MRUK room mesh has colliders for bullets. | ✅ **Clean.** Focused purely on physics setup. |

## 👤 Avatar & Spawning System (`Assets/Scripts/Avatar/` & `Assets/Scripts/Spawning/`)

These scripts manage the visual representation of players and their spawn locations.

| Script | Responsibility | SRP Analysis |
| :--- | :--- | :--- |
| **`AvatarMovementHandlerMotif.cs`** | Synchronizes **avatar transform**: ensures avatars move smoothly across the network. | ✅ **Clean.** Focused on movement/transform sync. |
| **`AvatarSpawnerHandlerMotif.cs`** | Manages **avatar lifecycle**: spawning and despawning avatars when players join/leave. | ✅ **Clean.** Separates lifecycle management from movement logic. |
| **`AvatarNameTagHandlerMotif.cs`** | Manages **name tags**: displays player names above avatars. | ✅ **Clean.** Purely cosmetic/UI component. |
| **`SpawnManagerMotif.cs`** | Manages **spawn points**: handles player spawn locations and queuing. | ✅ **Clean.** Focused on position management, not instantiation. |

## 📍 Colocation & Environment (`Assets/Scripts/Colocation/`)

These scripts handle the Mixed Reality shared space setup.

| Script | Responsibility | SRP Analysis |
| :--- | :--- | :--- |
| **`RoomSharingMotif.cs`** | Manages **room mesh sharing**: serializing and transmitting MRUK room data to other clients. | ✅ **Clean.** Focused on data transfer of room meshes. |


## 🌐 Network & Platform (`Assets/Scripts/Network/` & `Platform/`)

Infrastructure for multiplayer connectivity.

| Script | Responsibility | SRP Analysis |
| :--- | :--- | :--- |
| **`ConnectionManagerMotif.cs`** | Monitors **connection health**: handles disconnects and app pause/resume states. | ✅ **Clean.** System-level connection monitoring. |
| **`HostMigrationHandlerMotif.cs`** | Handles **host transfer**: logic for when the Master Client disconnects. | ✅ **Clean.** Specific fallback logic. |
| **`PhotonNetworkHelpers.cs`** | **Utility class**: static helper methods for common Fusion tasks. | ✅ **Clean.** Collection of stateless utilities. |

## 🛠️ Shared Utilities (`Assets/Scripts/Shared/`)

Common interfaces and helpers used across modules.

| Script | Responsibility | SRP Analysis |
| :--- | :--- | :--- |
| **`IDamageable.cs`** | **Interface definition**: defines the contract for taking damage. | ✅ **Clean.** Pure abstraction. |
| **`DebugLogger.cs`** | **Logging utility**: standardized logging format. | ✅ **Clean.** Utility. |

---

## 🔍 Architecture Summary

The project adheres to a **Component-Based Architecture**. Instead of monolithic "Manager" classes that do everything, responsibilities are broken down into:

1.  **Data Components** (`PlayerStatsMotif`, `ShootingGameConfigMotif`)
2.  **Logic Components** (`ShootingPlayerMotif`, `DroneAIMotif`)
3.  **Visual/UI Components** (`ShootingHUDMotif`, `AvatarNameTagHandlerMotif`)
4.  **Infrastructure Components** (`ConnectionManagerMotif`, `SharedSpatialAnchorManager`)

This structure ensures that changes to one system (e.g., how bullets move) do not inadvertently break unrelated systems (e.g., how health is calculated).

## 📐 Design Principles (SOLI)

To maintain code quality while remaining pragmatic within Unity's ecosystem, we aim for **SOLI** (SOLID without strict Dependency Inversion):

*   **S - Single Responsibility:** Continue splitting logic into focused components (e.g., `Health` vs `Shooting`).
*   **O - Open/Closed:** Use abstract classes or interfaces for systems likely to expand (e.g., Weapons, Enemy Types) so we can add new ones without rewriting existing code.
*   **L - Liskov Substitution:** Ensure implementations of interfaces like `IDamageable` behave consistently (no "fake" implementations that throw errors).
*   **I - Interface Segregation:** Keep interfaces small and specific (`IDamageable`, `IScorable`) rather than monolithic.
*   **D - Dependency Inversion (Pragmatic):** We accept some coupling to concrete `MonoBehaviour` types (via `FindAnyObjectByType`) to avoid the complexity of a full Dependency Injection framework, unless testing requirements change.

---

## 🏗️ Scene Architecture: Arena-Drone-v2

This scene utilizes Meta XR Building Blocks to establish the Mixed Reality and Multiplayer infrastructure.

| Object Name | Responsibility |
| :--- | :--- |
| **`[BuildingBlock] Camera Rig`** | **Player Rig**: The OVRCameraRig handling Head/Hand tracking and the OVRManager. |
| **`[BuildingBlock] Passthrough`** | **Visuals**: Manages the Passthrough layer to blend virtual content with the real world. |
| **`[BuildingBlock] Network Manager`** | **Networking**: The `NetworkRunner` prefab that manages the Photon Fusion session and spawns the player avatar. |
| **`[BuildingBlock] Networked Avatar`** | **Player Representation**: The Meta Avatar prefab spawned for each player to show their presence. |
| **`[BuildingBlock] MR Utility Kit`** | **Environment**: Manages Scene Understanding (Room Mesh), Raycasting, and Anchor loading. |
| **`[BuildingBlock] Colocation`** | **Alignment**: Manages the `OVRColocationSession` to align coordinate systems of multiple devices in the same room. |
| **`[BuildingBlock] Shared Spatial Anchor Core`** | **Persistence**: Handles the low-level saving and sharing of Spatial Anchors for colocation. |
| **`[BuildingBlock] Local Matchmaking`** | **Discovery**: Handles discovering other devices on the local network for colocation. |
| **`[BuildingBlock] OVRInteractionComprehensive`** | **Input**: The OVRInteractionSDK setup for Hand Tracking, Controller Grabs, Rays, and Pokes. |
| **`[BuildingBlock] Occlusion`** | **Rendering**: Handles depth occlusion so virtual objects correctly disappear behind real-world furniture/walls. |
| **`[BuildingBlock] Room Guardian`** | **Safety**: Prevents players from walking into virtual objects or outside the playspace (if configured). |
| **`[BuildingBlock] Controller Tracking (L/R)`** | **Visuals**: Displays the controller models and handles their tracking updates. |

