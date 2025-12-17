### System Architecture Diagram

```mermaid
graph TD
    subgraph Hardware ["Physical Hardware Layer (Meta Quest)"]
        HMD[Headset Tracking]
        Controllers[Controller Input]
        SpatialAnchors[Spatial Anchors API]
        Passthrough[Passthrough / Rendering]
    end

    subgraph Unity ["Unity Components (Local Client)"]
        subgraph Platform [Platform Integration]
            OVRRig[OVRCameraRig]
            OVRInput[OVRInput System]
            AnchorMgr[SharedSpatialAnchorManager]
            Colocation[ColocationManager]
        end

        subgraph GameLogic [Game Logic]
            Player[ShootingPlayerMotif]
            GameMgr[ShootingGameManagerMotif]
            Avatar[Avatar System]
        end
        
        Runner[NetworkRunner]
    end

    subgraph Network ["Photon Fusion Network"]
        Cloud[Fusion Cloud / Shared Room]
        NetState[Networked State]
        RPCs[RPC Messages]
    end

    %% Hardware to Unity
    HMD -->|Pose Data| OVRRig
    Controllers -->|Button/Trigger| OVRInput
    SpatialAnchors <-->|Anchor UUIDs & Localization| AnchorMgr
    Unity -->|Visuals/Audio| Passthrough

    %% Unity Internal - Colocation & Platform
    OVRRig -->|Transform Data| Colocation
    AnchorMgr -->|Anchor Transform| Colocation
    Colocation -->|World Alignment Correction| OVRRig
    
    %% Unity Internal - Gameplay
    OVRInput -->|Fire Event| Player
    Player -->|Spawn Bullet Request| Runner
    
    %% Unity to Network
    Runner <-->|Sync State / RPCs| Cloud
    GameMgr <-->|GameState / Scores| Runner
    Avatar <-->|Position / Rotation| Runner
    AnchorMgr <-->|Share Anchor UUIDs| Runner

    %% Network Internal
    Cloud --> NetState
    Cloud --> RPCs
```

### Data Flow Explanation

1.  **Physical Hardware Layer (Meta Quest)**
    *   **Input:** The headset provides 6DOF tracking data, and controllers provide button inputs (triggers, grips).
    *   **Spatial Anchors:** The device manages the local spatial anchors and their localization in the physical environment.

2.  **Unity Components (Local Client)**
    *   **Platform Integration:**
        *   `OVRCameraRig`: Receives raw tracking data.
        *   `SharedSpatialAnchorManager`: Interfaces with the hardware to create or locate anchors. It sends the Anchor UUIDs to other players via Photon Fusion.
        *   `ColocationManager`: The bridge between physical and virtual. It takes the localized anchor transform and aligns the `OVRCameraRig` so that the virtual coordinate system matches the physical world for all players.
    *   **Game Logic:**
        *   `ShootingPlayerMotif`: Reads `OVRInput` to detect firing. It requests the `NetworkRunner` to spawn networked bullet objects.
        *   `ShootingGameManagerMotif`: Manages the high-level game state (Score, Timer, Phase). It synchronizes this data automatically using Fusion's `[Networked]` properties.

3.  **Photon Fusion Network**
    *   **NetworkRunner:** The central networking component. It handles the connection to the Fusion Cloud.
    *   **Data Sync:**
        *   **State Authority:** The Host (or Master Client) maintains the "truth" for game state and anchors.
        *   **Replication:** Game state, avatar positions, and bullet spawns are replicated to all connected clients in real-time.



```plantuml
@startuml
skinparam componentStyle uml2
skinparam packageStyle rectangle

package "Physical Hardware Layer (Meta Quest)" as Hardware {
    [Headset Tracking] as HMD
    [Controller Input] as Controllers
    [Spatial Anchors API] as SpatialAnchors
    [Passthrough / Rendering] as Passthrough
}

package "Unity Components (Local Client)" as Unity {
    package "Platform Integration" {
        [OVRCameraRig] as OVRRig
        [OVRInput System] as OVRInput
        [SharedSpatialAnchorManager] as AnchorMgr
        [ColocationManager] as Colocation
    }

    package "Game Logic" {
        [ShootingPlayerMotif] as Player
        [ShootingGameManagerMotif] as GameMgr
        [Avatar System] as Avatar
    }
    
    [NetworkRunner] as Runner
}

package "Photon Fusion Network" as Network {
    [Fusion Cloud / Shared Room] as Cloud
    [Networked State] as NetState
    [RPC Messages] as RPCs
}

' Hardware to Unity
HMD --> OVRRig : Pose Data
Controllers --> OVRInput : Button/Trigger
SpatialAnchors <--> AnchorMgr : Anchor UUIDs & Localization
OVRRig --> Passthrough : Visuals/Audio

' Unity Internal - Colocation & Platform
OVRRig --> Colocation : Transform Data
AnchorMgr --> Colocation : Anchor Transform
Colocation --> OVRRig : World Alignment Correction

' Unity Internal - Gameplay
OVRInput --> Player : Fire Event
Player --> Runner : Spawn Bullet Request

' Unity to Network
Runner <--> Cloud : Sync State / RPCs@startuml
skinparam componentStyle uml2
skinparam packageStyle rectangle

package "Physical Hardware Layer (Meta Quest)" as Hardware {
    [Headset Tracking] as HMD
    [Controller Input] as Controllers
    [Spatial Anchors API] as SpatialAnchors
    [Passthrough / Rendering] as Passthrough
}

package "Unity Components (Local Client)" as Unity {
    package "Platform Integration" {
        [OVRCameraRig] as OVRRig
        [OVRInput System] as OVRInput
        [SharedSpatialAnchorManager] as AnchorMgr
        [ColocationManager] as Colocation
    }

    package "Game Logic" {
        [ShootingPlayerMotif] as Player
        [ShootingGameManagerMotif] as GameMgr
        [Avatar System] as Avatar
    }
    
    [NetworkRunner] as Runner
}

package "Photon Fusion Network" as Network {
    [Fusion Cloud / Shared Room] as Cloud
    [Networked State] as NetState
    [RPC Messages] as RPCs
}

' Hardware to Unity
HMD --> OVRRig : Pose Data
Controllers --> OVRInput : Button/Trigger
SpatialAnchors <--> AnchorMgr : Anchor UUIDs & Localization
OVRRig --> Passthrough : Visuals/Audio

' Unity Internal - Colocation & Platform
OVRRig --> Colocation : Transform Data
AnchorMgr --> Colocation : Anchor Transform
Colocation --> OVRRig : World Alignment Correction

' Unity Internal - Gameplay
OVRInput --> Player : Fire Event
Player --> Runner : Spawn Bullet Request

' Unity to Network
Runner <--> Cloud : Sync State / RPCs
GameMgr <--> Runner : GameState / Scores
Avatar <--> Runner : Position / Rotation
AnchorMgr <--> Runner : Share Anchor UUIDs

' Network Internal
Cloud --> NetState
Cloud --> RPCs

@enduml
```