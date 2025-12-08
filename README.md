# MR Shooting Arena - Co-located Mixed Reality Game

A high-fidelity Mixed Reality (MR) co-located multiplayer shooting game for Meta Quest 3/3S/Pro. This project demonstrates how to build shared-space experiences where players can see and interact with each other and virtual elements in the same physical room.

Built with **Unity 6**, **Photon Fusion 2**, and **Meta Mixed Reality Utility Kit (MRUK)**.

## 🌟 Key Features

- **Co-located Multiplayer**: Seamlessly align multiple players in the same physical space using Meta's Shared Spatial Anchors and Colocation Discovery.
- **Mixed Reality Integration**:
  - **Passthrough**: Blends virtual content with the real world.
  - **Scene Understanding**: Uses MRUK to make virtual objects (bullets, drones) interact with physical walls and furniture.
  - **Room Sharing**: Host scans the room, and the mesh is shared over the network to all clients for consistent physics.
- **Combat System**:
  - **Projectile Physics**: Networked bullet system with gravity and ricochets.
  - **Damage System**: Universal `IDamageable` interface for players, drones, and props.
  - **Drones**: AI-controlled flying enemies that navigate the physical room.
- **Networking**:
  - Powered by **Photon Fusion 2** (Shared Mode).
  - Networked player stats (Kills, Deaths, Accuracy).
  - Low-latency state synchronization.

## 🛠️ Tech Stack

- **Engine**: Unity 6 (Universal Render Pipeline)
- **XR SDK**: Meta XR Core SDK & OpenXR
- **MR Tools**: Meta MR Utility Kit (MRUK)
- **Networking**: Photon Fusion 2
- **Avatars**: Meta Avatars SDK
- **Platform**: Meta Quest (Android)

## 📂 Project Structure

The project follows the "MR Motifs" architecture pattern, organizing features into self-contained modules.

```
Assets/
├── Scripts/
│   ├── Colocation/       # Shared anchors, alignment, and room sharing
│   ├── Network/          # Photon Fusion helpers and connection logic
│   ├── Shooting/         # Weapons, bullets, player health, and drones
│   ├── Avatar/           # Meta Avatar integration
│   └── Shared/           # Common interfaces (IDamageable)
├── MRMotifs/             # Reusable MR patterns and assets
└── Scenes/               # Main game scenes
```

### Key Components

- **`SharedSpatialAnchorManager`**: Handles the creation, saving, and sharing of spatial anchors to align coordinate systems.
- **`RoomSharingMotif`**: Serializes the host's room mesh (walls, tables) and sends it to clients so everyone sees the same physical boundaries.
- **`BulletMotif`**: Networked projectile logic with collision detection.
- **`DroneMotif`**: AI behavior for enemy drones.

## 🚀 Getting Started

### Prerequisites

- **Unity 6** (6000.0.x or later) with Android Build Support.
- **Meta Quest 3, 3S, or Pro** (Quest 2 supported but MR features are limited).
- **Photon App ID**: Create a Fusion App ID at [dashboard.photonengine.com](https://dashboard.photonengine.com).

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/jfriisj/unity-shooting-arena-co-location.git
   ```

2. **Open in Unity**:
   - Add the project to Unity Hub and open it.
   - Wait for packages to resolve.

3. **Configure Photon**:
   - Go to `Tools > Fusion > Hub`.
   - Paste your App ID into the `FusionAppSettings`.

4. **Build & Run**:
   - Go to `File > Build Settings`.
   - Switch platform to **Android**.
   - Ensure the main scene is added to the build list.
   - Click **Build And Run**.

### How to Play (Co-location)

1. **Host (Player 1)**:
   - Launch the app.
   - Allow permissions (Spatial Data, Local Network).
   - Scan the room if prompted (or use existing room setup).
   - The app will automatically create a session and advertise it via Bluetooth.

2. **Client (Player 2+)**:
   - Launch the app in the **same physical room**.
   - The app will discover the Host's session.
   - It will automatically download the anchor and room mesh.
   - Once aligned, you will see the Host's avatar in the correct physical location.

## 🎮 Controls

- **Left Stick**: Move (if not using physical movement) / Strafe
- **Right Stick**: Turn (Snap/Smooth)
- **Right Trigger**: Fire Weapon
- **Left Trigger**: (Optional) Secondary action / Shield
- **Grip**: Interact with objects

## 🤝 Contributing

1. Fork the project.
2. Create your feature branch (`git checkout -b feature/AmazingFeature`).
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4. Push to the branch (`git push origin feature/AmazingFeature`).
5. Open a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

*Based on Meta's MR Motifs and Discover samples.*
