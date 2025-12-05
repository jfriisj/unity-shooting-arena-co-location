# Shooting Game - Co-location Multiplayer Project

A standalone Meta Quest multiplayer shooting game with co-location support, extracted from MR Motifs.

---

## 🎯 MVP Goal

**Create a working co-located arena shooting game where 2+ players in the same physical room can shoot at each other in mixed reality.**

### Key Design Decisions
- **Open Play Area Mode**: Players spawn at their physical headset position (no predefined spawn points)
- **Co-location First**: Uses shared spatial anchors to align all players in the same physical space
- **Minimal Scope**: No voice chat, practice mode, or other non-essential features

---

## ✅ Current Status: COMPILES SUCCESSFULLY

The project now compiles without errors. All blocking issues have been resolved.

### Fixes Applied
| Issue | Resolution |
|-------|------------|
| Missing `PHOTON_VOICE_DEFINED` causing errors | Removed from scripting define symbols |
| `AvatarSpeakerHandlerMotif.cs` Photon Voice dependency | Deleted (not MVP) |
| `SpaceSharingManager.cs` MRUtilityKit reference | Deleted (not needed) |
| `GroupPresenceAndInviteHandlerMotif.cs` SharedAssets reference | Fixed - removed dependency |
| `SpawnPointMotif.cs` reference in SpawnManager | Fixed - using open play area mode |
| Broken script GUIDs in scene | Fixed - updated to correct GUIDs |

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Avatar/                 # Avatar handling
│   │   ├── AvatarMovementHandlerMotif.cs    # Avatar position sync
│   │   ├── AvatarNameTagHandlerMotif.cs     # Player name tags
│   │   └── AvatarSpawnerHandlerMotif.cs     # Avatar spawn handling
│   ├── Shooting/               # Core shooting mechanics
│   │   ├── BulletMotif.cs                   # Networked bullet
│   │   ├── PlayerHealthMotif.cs             # Health, damage, respawn
│   │   ├── ShootingGameManagerMotif.cs      # Game state, rounds, scoring
│   │   ├── ShootingHUDMotif.cs              # HUD UI
│   │   ├── ShootingPlayerMotif.cs           # Trigger input, bullet spawning
│   │   ├── ShootingPlayerSpawnerMotif.cs    # Player spawner
│   │   └── ShootingSetupMotif.cs            # Player setup
│   ├── Spawning/               # Spawn system
│   │   └── SpawnManagerMotif.cs             # Open play area mode
│   ├── Colocation/             # Co-location system
│   │   ├── ColocationManager.cs             # Camera rig alignment
│   │   └── SharedSpatialAnchorManager.cs    # Anchor creation/sharing
│   ├── Platform/               # Quest platform
│   │   ├── GroupPresenceAndInviteHandlerMotif.cs  # Group presence
│   │   └── InvitationAcceptanceHandlerMotif.cs    # Invite handling
│   └── Shared/                 # Utilities
│       └── HandleAnimationMotif.cs
├── Prefabs/
│   ├── BulletMotif.prefab
│   ├── NetworkedRigMotif.prefab
│   ├── FusionAvatarSdk28PlusNoLegs.prefab
│   └── ScoreEntryMotif.prefab
├── Scenes/
│   └── ShootingGame.unity      # Main scene
└── Resources/
    └── OculusPlatformSettings.asset
```

---

## 🎮 Scene Structure (`ShootingGame.unity`)

| GameObject | Purpose |
|------------|---------|
| `[BuildingBlock] Camera Rig` | OVRCameraRig with tracking |
| `[MR Motif] Arena` | Reference point for avatar sync (child of Camera Rig) |
| `[BuildingBlock] Passthrough` | MR passthrough layer |
| `[BuildingBlock] Network Manager` | Fusion NetworkRunner |
| `[BuildingBlock] Auto Matchmaking` | Auto session join |
| `[BuildingBlock] Platform Init` | Oculus Platform init |
| `[BuildingBlock] Networked Avatar` | Avatar spawning |
| `[BuildingBlock] MR Utility Kit` | Room scanning |
| `[BuildingBlock] Colocation` | Colocation building block |
| `[MR Motif] Spawn Manager` | Open play area spawning |
| `[MR Motif] Avatar Spawner Handler` | Avatar spawn handling |
| `[MR Motif] Shooting Game Manager` | Game state management |
| `[MR Motif] Shooting Setup` | Player setup |
| `[MR Motif] Shooting HUD Canvas` | HUD UI |
| `[MR Motif] Group Presence` | Group presence handling |
| `[MR Motifs] Colocation Manager` | Anchor alignment |
| `[MR Motifs] SSA Manager` | Shared spatial anchors |

---

## ✅ Implemented Features

### Core Gameplay
| Feature | Script | Status |
|---------|--------|--------|
| Game states (Waiting/Countdown/Playing/RoundEnd) | `ShootingGameManagerMotif.cs` | ✅ |
| Round timer (3 min default) | `ShootingGameManagerMotif.cs` | ✅ |
| Win condition (10 kills default) | `ShootingGameManagerMotif.cs` | ✅ |
| Score tracking | `ShootingGameManagerMotif.cs` | ✅ |

### Health & Combat
| Feature | Script | Status |
|---------|--------|--------|
| Networked health (100 HP) | `PlayerHealthMotif.cs` | ✅ |
| Take damage RPC | `PlayerHealthMotif.cs` | ✅ |
| Death and respawn (3 sec) | `PlayerHealthMotif.cs` | ✅ |
| Invulnerability after respawn | `PlayerHealthMotif.cs` | ✅ |
| Kill tracking | `PlayerHealthMotif.cs` | ✅ |

### Weapons & Bullets
| Feature | Script | Status |
|---------|--------|--------|
| Trigger input detection | `ShootingPlayerMotif.cs` | ✅ |
| Dual-wield weapons | `ShootingPlayerMotif.cs` | ✅ |
| Networked bullet spawning | `ShootingPlayerMotif.cs` | ✅ |
| Bullet physics & velocity | `BulletMotif.cs` | ✅ |
| Hit detection (collision/trigger) | `BulletMotif.cs` | ✅ |
| Owner tracking | `BulletMotif.cs` | ✅ |

### HUD
| Feature | Script | Status |
|---------|--------|--------|
| Health slider | `ShootingHUDMotif.cs` | ✅ |
| Kills/deaths display | `ShootingHUDMotif.cs` | ✅ |
| Death panel with respawn countdown | `ShootingHUDMotif.cs` | ✅ |
| Hit markers | `ShootingHUDMotif.cs` | ✅ |

### Networking & Co-location
| Feature | Script | Status |
|---------|--------|--------|
| Photon Fusion 2 networking | Building Block | ✅ |
| Auto matchmaking | Building Block | ✅ |
| Anchor creation (3 modes) | `SharedSpatialAnchorManager.cs` | ✅ |
| Anchor advertisement/discovery | `SharedSpatialAnchorManager.cs` | ✅ |
| Camera rig alignment | `ColocationManager.cs` | ✅ |
| Open play area spawning | `SpawnManagerMotif.cs` | ✅ |

### Avatars
| Feature | Script | Status |
|---------|--------|--------|
| Meta Avatars with networking | Building Block | ✅ |
| Avatar position sync | `AvatarMovementHandlerMotif.cs` | ✅ |
| Object-of-interest parenting | `AvatarMovementHandlerMotif.cs` | ✅ |

---

## 🔧 Configuration

### Game Settings
| Setting | Default | Location |
|---------|---------|----------|
| Round Duration | 180 sec | `ShootingGameManagerMotif` |
| Kills to Win | 10 | `ShootingGameManagerMotif` |
| Min Players | 2 | `ShootingGameManagerMotif` |

### Player Settings
| Setting | Default | Location |
|---------|---------|----------|
| Max Health | 100 | `PlayerHealthMotif` |
| Respawn Delay | 3 sec | `PlayerHealthMotif` |
| Invulnerability | 2 sec | `PlayerHealthMotif` |

### Weapon Settings
| Setting | Default | Location |
|---------|---------|----------|
| Fire Force | 15 | `ShootingPlayerMotif` |
| Fire Rate | 0.2 sec | `ShootingPlayerMotif` |
| Bullet Lifetime | 5 sec | `ShootingPlayerMotif` |
| Bullet Damage | 10 | `BulletMotif` |

### Spawn Settings
| Setting | Default | Location |
|---------|---------|----------|
| Open Play Area | ✅ Enabled | `SpawnManagerMotif` |

---

## 📋 Remaining Tasks

### Phase 1: Verify Configuration
- [ ] Assign `BulletMotif.prefab` to `ShootingSetupMotif`
- [ ] Assign weapon prefab to `ShootingSetupMotif`
- [ ] Configure `PhotonAppSettings` with Fusion App ID
- [ ] Configure `OculusPlatformSettings` with App ID
- [ ] Test in Editor

### Phase 2: Test Networking
- [ ] Build and deploy to Quest
- [ ] Test 2-player connection
- [ ] Verify avatars visible to both players
- [ ] Verify game state transitions

### Phase 3: Test Co-location
- [ ] Test anchor creation on host
- [ ] Test anchor discovery on client
- [ ] Verify spatial alignment

### Phase 4: Test Shooting
- [ ] Verify bullets spawn and travel
- [ ] Verify hit detection
- [ ] Verify damage and death

### Phase 5: Test Game Loop
- [ ] Test respawn cycle
- [ ] Test win condition
- [ ] Test game restart

---

## 🚀 Quick Start

### Prerequisites
- Unity 6 (6000.0.x)
- Meta XR SDK packages installed
- Photon Fusion 2 SDK
- Valid Photon App ID
- Valid Oculus Platform App ID

### Setup
1. Open `ShootingGame.unity`
2. Configure Photon: **Fusion > Fusion Hub** → Set App ID
3. Configure Oculus: **Oculus > Platform > Edit Settings** → Set App ID
4. Build for Android (Quest)

### Testing Co-location
1. Deploy to two Quest headsets on same WiFi
2. Host starts game → creates anchor
3. Client discovers session → aligns to anchor
4. Both players in same physical space!

---

## ❌ Out of Scope (Not MVP)

- Voice chat
- Practice mode
- Predefined spawn points
- Seeing other players' health bars
- Persistent leaderboards
- Room scanning obstacles
- Multiple game modes
- Spectator mode
- Weapon variety
- Power-ups

---

## License
Based on Meta MR Motifs sample code.
