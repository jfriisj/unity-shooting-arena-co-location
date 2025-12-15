# Scene Architecture: ShootingGame

**Generated on:** 15-12-2025
**Scene Name:** ShootingGame

## Overview
The `ShootingGame` scene is the main gameplay scene for the Unity Shooting Arena Co-location project. It integrates Mixed Reality features (Passthrough, Scene Mesh), Multiplayer (Photon Fusion), and Meta Avatars into a shared-space shooter experience.

## 1. Core Systems
Infrastructure components handling hardware abstraction, networking, and platform services.

### Camera & Input
*   **[BuildingBlock] Camera Rig**
    *   `OVRCameraRig`: Main VR camera rig.
    *   `OVRManager`: Manages VR session and performance.
    *   `OVRHeadsetEmulator`: For editor testing.
    *   `BoundaryDisablerMotif`: Disables guardian for MR.
    *   **TrackingSpace**: Contains anchors for Eyes, Hands, and Controllers.
        *   Includes `OVRHand`, `OVRSkeleton`, `OVRMesh` for Hand Tracking.
        *   Includes `OVRControllerHelper` and controller models for Controller Tracking.

### Networking (Photon Fusion)
*   **[BuildingBlock] Network Manager**
    *   `NetworkRunner`: Core Fusion runner.
    *   `FusionVoiceClient`: Photon Voice integration.
    *   `HostMigrationHandlerMotif`: Handles host migration logic.
*   **[BuildingBlock] Auto Matchmaking**
    *   `FusionBootstrap`: Handles auto-start and matchmaking.

### Platform & Colocation
*   **[BuildingBlock] Platform Init**
    *   `PlatformInit_`: Initializes Oculus Platform SDK.
*   **[BuildingBlock] Colocation**
    *   `ColocationController`: Manages shared space alignment.
    *   `SharedSpatialAnchorCore`: Core logic for SSA.
    *   `RoomSharingMotif`: Handles room sharing logic.
*   **[MR Motifs] Colocation Manager**
    *   `ColocationManager`: High-level manager for colocation flow.
*   **[MR Motifs] SSA Manager**
    *   `SharedSpatialAnchorManager`: Manages creation and sharing of spatial anchors.

## 2. Game Systems
Components driving the specific gameplay rules and mechanics.

### Game Loop & Logic
*   **[MR Motif] Shooting Game Manager**
    *   `ShootingGameManagerMotif`: Central game loop (Rounds, Scoring, State).
        *   *References:* `ScoreboardPanel`, `TimerText`, `StatusText`.
    *   `ShootingAudioMotif`: Handles game SFX (Start, End, Hit, Death).
    *   `ShootingGameConfigMotif`: Configuration data.
*   **[MR Motif] Practice Mode**
    *   `PracticeModeMotif`: Logic for solo/practice play.

### Spawning & Entities
*   **[MR Motif] Spawn Manager**
    *   `SpawnManagerMotif`: Handles player spawn points.
        *   *References:* `objectOfInterest` -> `[MR Motif] Arena`.
*   **[MR Motif] Avatar Spawner Handler**
    *   `AvatarSpawnerHandlerMotif`: Spawns player avatars.
*   **[BuildingBlock] Networked Avatar**
    *   `AvatarSpawnerFusion`: Spawns Fusion-synced Meta Avatars.
    *   `OvrAvatarManager`: Manages local avatar loading.
*   **[MR Motif] Cover Spawner**
    *   `CoverSpawnerMotif`: Spawns cover objects in the arena.
        *   *References:* `m_coverPrefab` -> `NetworkedCover`.

### Weapons
*   **[MR Motif] Shooting Setup**
    *   `ShootingSetupMotif`: Configures player weapons.
        *   *References:* `m_bulletPrefab` -> `BulletMotif`, `m_weaponPrefab` -> `FA M9T (Black)`.

## 3. UI Systems
User interface elements for HUD and menus.

*   **[MR Motif] Shooting HUD Canvas**
    *   `OVROverlayCanvas`: Renders UI as a high-quality overlay.
    *   `ShootingHUDMotif`: Controls HUD logic.
    *   **ScoreboardPanel**: Displays scores (Inactive by default).
    *   **TimerText**: Round timer.
    *   **StatusText**: Game status messages.
*   **[MR Motif] Group Presence**
    *   `GroupPresenceAndInviteHandlerMotif`: UI/Logic for inviting friends.

## 4. Environment & Mixed Reality
Components interacting with the physical world.

*   **[BuildingBlock] Passthrough**
    *   `OVRPassthroughLayer`: Renders camera feed as background.
*   **[BuildingBlock] MR Utility Kit**
    *   `MRUK`: Mixed Reality Utility Kit manager (Scene understanding).
*   **[BuildingBlock] Scene Mesh** (Inactive)
    *   `RoomMeshController`: Generates mesh from room data.

## 5. Debug & Tools
Development and analysis tools.

*   **[BuildingBlock] Scene Debugger** (Inactive)
    *   `SceneDebugger`: Visual debugger for scene understanding.
*   **[MR Motif] Metrics Logger**
    *   `MetricsLogger`: Logs performance/gameplay metrics.
*   **[MR Motif] Network Metrics**
    *   `NetworkLatencyTracker`: Tracks network performance.
*   **VoiceLogger** (Inactive)

## 6. Hierarchy Reference
*Condensed view of the scene hierarchy.*

*   **Directional Light**
*   **[BuildingBlock] Camera Rig**
*   **[BuildingBlock] Passthrough**
*   **[BuildingBlock] Network Manager**
*   **[BuildingBlock] Auto Matchmaking**
*   **[BuildingBlock] Platform Init**
*   **[BuildingBlock] Networked Avatar**
*   **[BuildingBlock] MR Utility Kit**
*   **[BuildingBlock] Colocation**
*   **[BuildingBlock] Scene Mesh**
*   **[MR Motif] Shooting Game Manager**
*   **[MR Motif] Spawn Manager**
*   **[MR Motif] Avatar Spawner Handler**
*   **[MR Motif] Shooting Setup**
*   **[MR Motif] Group Presence**
*   **[MR Motif] Shooting HUD Canvas**
*   **[MR Motifs] Colocation Manager**
*   **[MR Motifs] SSA Manager**
*   **[MR Motif] Practice Mode**
*   **VoiceLogger**
*   **[BuildingBlock] Scene Debugger**
*   **[MR Motif] Cover Spawner**
*   **[MR Motif] Room Sharing**
*   **[MR Motif] Metrics Logger**
*   **[MR Motif] Network Metrics**
