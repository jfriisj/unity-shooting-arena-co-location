# C# Scripts Overview

**Generated on:** 2025-12-15

## Avatar
- `AvatarMovementHandlerMotif.cs` — Handles synchronization of avatar positions and rotations across networked clients. Manages the interaction between avatars and the object of interest, ensuring avatars are correctly positioned relative to the object and updated whenever the object is moved.
- `AvatarNameTagHandlerMotif.cs` — Handles attaching name tags to remote avatars by matching state authority and retrying unassigned tags after a delay.
- `AvatarSpawnerHandlerMotif.cs` — Handles the spawning of avatars in the scene, managing their positions using the spawn manager. Also, responsible for releasing spawn locations when players leave the scene.

## Colocation
- `ColocationManager.cs` — Exposes: RegisterHostCalibration, AlignUserToAnchor, GetCurrentCalibrationError...
- `RoomSharingMotif.cs` — Handles sharing and loading of room mesh data across co-located players. When the host creates the colocation session, their room is shared. Guests load the shared room to ensure consistent collision geometry.
- `SharedSpatialAnchorManager.cs` — Exposes: Spawned

## Network
- `HostMigrationHandlerMotif.cs` — Handles Host Migration for the shooting game. When the current host disconnects, this ensures the game state is preserved and a new host takes over seamlessly.

## Platform
- `GroupPresenceAndInviteHandlerMotif.cs` — The GroupPresenceAndInviteHandlerMotif class is responsible for managing group presence and launching the invite panel using the Oculus Platform SDK. It allows users to set their presence in a joinable state and invite friends to join them in a multiplayer session.
- `InvitationAcceptanceHandlerMotif.cs` — The InvitationAcceptanceHandlerMotif class handles deep link invitations using the Oculus Platform SDK. When the application is launched via a deep link (e.g., an invitation from a friend), it checks the launch details to determine if the user should be directed to a specific destination.

## Shared
- `HandleAnimationMotif.cs` — Manages scaling and alpha transitions for an object over time, using a coroutine to animate the object smoothly.

## Shared/Metrics
- `CalibrationAccuracyTracker.cs` — Tracks spatial calibration accuracy for colocation. Monitors drift and alignment errors over time.
- `MetricsLogger.cs` — Collects and logs performance and collaboration metrics for research data collection. Saves data to CSV files on the device for later extraction.
- `NetworkLatencyTracker.cs` — Measures network latency using RPC ping/pong messages. Must be attached to a NetworkObject for proper functionality.

## Shooting
- `BoundaryDisablerMotif.cs` — Disables the Quest Guardian boundary system to allow free movement in passthrough mode. Uses the Contextual Boundaryless API (OVRManager.shouldBoundaryVisibilityBeSuppressed) which properly suppresses the boundary when passthrough is active.  Requirements: - OVRManager Quest Features > Boundary Visibility Support must be set to "Supported" or "Required" - Passthrough Support must not be "None" - An OVRPassthroughLayer must be active for boundary suppression to work  For fully boundaryless apps (no VR segments at all), add to AndroidManifest.xml instead: <uses-feature android:name="com.oculus.feature.BOUNDARYLESS_APP" android:required="true"/>
- `BulletMotif.cs` — Represents a networked bullet/projectile in the shooting game. Handles physics, collision detection, hit registration, and visual effects.
- `CoverSpawnerMotif.cs` — Allows the host to spawn cover objects in the arena. Point with controller and press grip to place cover.
- `NetworkedCoverMotif.cs` — A networked cover object that can be spawned by the host. Provides physical barriers for players to hide behind in large open spaces.
- `PlayerHealthMotif.cs` — Manages player health, death, and respawn in the shooting game. Synchronizes health state across networked clients and handles scoring.
- `PracticeModeMotif.cs` — Enables single-player practice mode for testing shooting mechanics without networking. When enabled, spawns AI targets for the player to shoot at.
- `ShootingAudioMotif.cs` — Loads and provides audio clips for the shooting game from Resources/Audio folder. Attach to the [MR Motif] Shooting Game Manager to auto-assign sounds. Audio files should be placed in Assets/Resources/Audio/ folder.
- `ShootingDebugVisualizerMotif.cs` — Debug visualization for the shooting game. Shows player positions, health bars, bullet trajectories, and game state. Toggle with both thumbstick buttons pressed simultaneously.
- `ShootingGameConfigMotif.cs` — Central configuration for the shooting game. Modify these values to tune gameplay without editing multiple scripts. Attach to the [MR Motif] Shooting Game Manager GameObject.
- `ShootingGameManagerMotif.cs` — Manages the shooting game state, including round management, scoring, and UI updates. Coordinates between all players.
- `ShootingHUDMotif.cs` — Displays player HUD showing health, ammo, and score. Attaches to the camera rig and follows the player's view.
- `ShootingPlayerMotif.cs` — Handles player shooting mechanics in a co-located multiplayer shooting game. Spawns networked projectiles when the player presses the trigger and manages the player's weapon visuals locally.
- `ShootingSetupMotif.cs` — Scene-based setup for the shooting game. Attaches shooting components to players when their avatars spawn. Place this in the scene alongside the Avatar Spawner Handler.

## Spawning
- `SpawnManagerMotif.cs` — Manages the spawn locations for players in a multiplayer session. Controls the queuing system for players waiting for an available spawn location and ensures avatars are placed correctly at available locations.
