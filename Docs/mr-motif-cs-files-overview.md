# MR Motifs C# Scripts Overview

**Generated on:** 2025-12-15

## ColocatedExperiences/Scripts/BouncingBall
- `BallManagerMotif.cs` — Exposes: Spawned
- `BouncingBallMotif.cs` — No summary provided.

## ColocatedExperiences/Scripts/Colocation
- `ColocationManager.cs` — Exposes: AlignUserToAnchor, GetCurrentCalibrationError, ValidateCalibration...
- `SharedSpatialAnchorManager.cs` — Exposes: Spawned

## ColocatedExperiences/Scripts/Editor
- `ClearPlayerPrefsEditor.cs` — Exposes: ClearPlayerPrefs, PrintPlayerPrefs

## ColocatedExperiences/Scripts/Space Sharing
- `SpaceSharingManager.cs` — Exposes: Spawned

## ColocatedExperiences/Scripts/Spatial Anchors
- `SpatialAnchorLoader.cs` — No summary provided.
- `SpatialAnchorManager.cs` — No summary provided.
- `SpatialAnchorStorage.cs` — Exposes: SaveUuidToPlayerPrefs, LoadAllUuidsFromPlayerPrefs, ClearAllUuids

## ColocatedExperiences/Scripts/Whiteboard
- `InteractionStateManagerMotif.cs` — Exposes: CanDrawWithPointer, CanDrawWithRaycast, CanManipulatePanel...
- `NetworkedPanelPlacementMotif.cs` — Exposes: Spawned, UpdateRollingAverage
- `PointerDrawingMotif.cs` — Exposes: Spawned
- `PointerHandlerMotif.cs` — No summary provided.
- `RaycastDrawingMotif.cs` — Exposes: Spawned, Despawned
- `WhiteboardManagerMotif.cs` — The WhiteboardManagerMotif maintains an authoritative whiteboard texture. Clients send immediate drawing commands via RPC_DrawLine for real‑time feedback. When a new client joins, they can request a snapshot of the current texture. The state authority sends the snapshot as a series of small RPC chunks.
- `WhiteboardSnapshotReceiverMotif.cs` — Exposes: ReceiveChunk

## InstantContentPlacement/Scripts/Depth Effects
- `EnvironmentDepthMatrixHelperMotif.cs` — No summary provided.
- `OrbSpawnerMotif.cs` — Exposes: LaunchOrb, TriggerDetonation
- `ShockWaveEffectMotif.cs` — Expands the scan wave effect over a set duration, then destroys the object.
- `ShockWaveOrbMotif.cs` — Represents a throwable orb that can be launched, attach to surfaces upon impact, and trigger a scanning effect. The orb plays audio clips on spawn and when it sticks to a surface. It integrates with an EnvironmentRaycastManager for collision detection. When detonated, it invokes a UnityEvent to notify listeners of the event and destroys itself.

## InstantContentPlacement/Scripts/Instant Content Placement
- `GroundingShadowMotif.cs` — Projects a realistic shadow of a target object onto detected surfaces beneath it. The shadow adjusts its position, size, and opacity based on the target’s proximity to the surface.
- `SurfacePlacementMotif.cs` — Positions and snaps an interactable object to the nearest detected surface upon release. Uses ray casting to find horizontal surfaces below the object and smooths the object's position and rotation towards the target surface if within a specified snap distance. Displays a placement indicator and line from the object to the surface while grabbed and in range.

## PassthroughTransitioning/Scripts
- `AudioController.cs` — No summary provided.
- `PassthroughDissolver.cs` — No summary provided.
- `PassthroughFader.cs` — A unified passthrough fader that supports both Selective and Underlay modes. Select the mode in the inspector via the Passthrough Viewing Mode property.
- `PassthroughSlider.cs` — No summary provided.
- `PerlinNoiseTexture.cs` — No summary provided.

## PassthroughTransitioning/Scripts/Editor
- `PassthroughFaderEditor.cs` — Exposes: OnInspectorGUI

## Shared Assets/Scripts
- `ConstraintInjectorMotif.cs` — Injects rotation constraints into the <see cref="GrabFreeTransformer"/> component at runtime. Used to limit the rotation of the chess board and movie screen in the samples scenes of this MR Motif.
- `HomeScene.cs` — No summary provided.
- `LazyFollowUIPanel.cs` — No summary provided.
- `MenuPanel.cs` — Exposes: ToggleMenu
- `MetricsLogger.cs` — Collects and logs technical performance metrics for VR training research. Tracks frame rate, network latency, calibration accuracy, and thermal performance. Based on metrics collection guide specifications.
- `SceneLoader.cs` — No summary provided.

## SharedActivities/Scripts
- `ExperimentTimerMotif.cs` — Displays an experiment timer in the scene, showing elapsed time since session start. Attaches to a world-space canvas for VR visibility.

## SharedActivities/Scripts/Avatars
- `AvatarMovementHandlerMotif.cs` — Handles synchronization of avatar positions and rotations across networked clients. Manages the interaction between avatars and the object of interest, ensuring avatars are correctly positioned relative to the object and updated whenever the object is moved.
- `AvatarNameTagHandlerMotif.cs` — Handles attaching name tags to remote avatars by matching state authority and retrying unassigned tags after a delay.
- `AvatarSpawnerHandlerMotif.cs` — Handles the spawning of avatars in the scene, managing their positions using the spawn manager. Also, responsible for releasing spawn locations when players leave the scene.
- `AvatarSpeakerHandlerMotif.cs` — Handles attaching speakers to remote avatars by matching state authority and retrying unassigned speakers after a delay.

## SharedActivities/Scripts/Chess Sample
- `ChessBoardHandlerMotif.cs` — Handles synchronization of chess piece positions and rotations across networked clients and manages interaction events for selecting and moving chess pieces.

## SharedActivities/Scripts/Helpers
- `HandleAnimationMotif.cs` — Manages scaling and alpha transitions for an object over time, using a coroutine to animate the object smoothly.

## SharedActivities/Scripts/Movie Sample
- `MovieControlsHandlerMotif.cs` — Handles user interactions with video player controls, such as play/pause, volume, settings, and timeline adjustments, and synchronizes these states across a networked multiplayer environment.

## SharedActivities/Scripts/Quest Platform
- `GroupPresenceAndInviteHandlerMotif.cs` — The GroupPresenceAndInviteHandlerMotif class is responsible for managing group presence and launching the invite panel using the Oculus Platform SDK. It allows users to set their presence in a joinable state and invite friends to join them in a multiplayer session.
- `InvitationAcceptanceHandlerMotif.cs` — The InvitationAcceptanceHandlerMotif class handles deep link invitations using the Oculus Platform SDK. When the application is launched via a deep link (e.g., an invitation from a friend), it checks the launch details to determine if the user should be directed to a specific destination.

## SharedActivities/Scripts/Spawning
- `SpawnManagerMotif.cs` — Manages the spawn locations for players in a multiplayer session. Controls the queuing system for players waiting for an available spawn location and ensures avatars are placed correctly at available locations.
- `SpawnPointMotif.cs` — Represents a spawn point in the scene for use by the <see cref="SpawnManagerMotif"/>.
