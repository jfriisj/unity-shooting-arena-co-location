# Shooting Arena - Co-location Multiplayer Game

A Meta Quest mixed reality arena shooter where 2+ players in the same physical room can see and shoot each other. Built with Unity 6, Photon Fusion 2, and Meta XR SDK.

---

## 🎯 Project Goal

**A co-located arena shooting game where players in the same physical space share a coordinate system via spatial anchors and compete in fast-paced combat.**

### Design Philosophy
- **Open Play Area Mode**: Players spawn at their physical headset position (no teleportation needed)
- **Co-location First**: Shared spatial anchors ensure all players see the same virtual content in the same physical space
- **Minimal Scope**: Focus on core shooting mechanics before adding voice chat, power-ups, etc.

---

## ✅ Current Status: READY FOR TESTING

The project compiles successfully with all core systems implemented. Ready for device testing and configuration.

| Area | Status |
|------|--------|
| **Compilation** | ✅ No errors |
| **Core Scripts** | ✅ All implemented |
| **Scene Setup** | ✅ Building blocks configured |
| **Networking** | ⚠️ Needs Photon App ID |
| **Platform** | ⚠️ Needs Oculus App ID |
| **Device Testing** | 🔲 Not yet tested |

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Avatar/                 # Avatar handling
│   │   ├── AvatarMovementHandlerMotif.cs    # Position sync via object-of-interest
│   │   ├── AvatarNameTagHandlerMotif.cs     # Player name tags above heads
│   │   └── AvatarSpawnerHandlerMotif.cs     # Avatar spawn handling
│   ├── Shooting/               # Core shooting mechanics
│   │   ├── BulletMotif.cs                   # Networked projectile with physics
│   │   ├── BoundaryDisablerMotif.cs         # Guardian suppression for free movement
│   │   ├── PlayerHealthMotif.cs             # Health, damage, death, respawn
│   │   ├── ShootingGameManagerMotif.cs      # Game state machine, scoring
│   │   ├── ShootingHUDMotif.cs              # Health bar, kills, death panel
│   │   ├── ShootingPlayerMotif.cs           # Trigger input, bullet spawning
│   │   └── ShootingSetupMotif.cs            # Attaches shooting to avatars
│   ├── Spawning/               # Spawn system
│   │   └── SpawnManagerMotif.cs             # Open play area spawning
│   ├── Colocation/             # Co-location system
│   │   ├── ColocationManager.cs             # Camera rig alignment to anchors
│   │   └── SharedSpatialAnchorManager.cs    # Anchor creation/sharing (3 modes)
│   ├── Network/                # Networking utilities
│   │   └── HostMigrationHandlerMotif.cs     # Seamless host migration
│   ├── Platform/               # Quest platform integration
│   │   ├── GroupPresenceAndInviteHandlerMotif.cs  # Group presence
│   │   └── InvitationAcceptanceHandlerMotif.cs    # Deep link invite handling
│   └── Shared/                 # Utilities
│       └── HandleAnimationMotif.cs
├── Prefabs/
│   ├── Shooting/
│   │   └── BulletMotif.prefab               # Networked bullet prefab
│   ├── Colocation/                          # Colocation-related prefabs
│   ├── NetworkedRigMotif.prefab             # Networked camera rig
│   ├── FusionAvatarSdk28PlusNoLegs.prefab   # Meta Avatar prefab
│   └── ScoreEntryMotif.prefab               # Scoreboard entry UI
├── Scenes/
│   └── ShootingGame.unity                   # Main game scene
└── Resources/
    └── OculusPlatformSettings.asset         # Platform configuration
```

---

## 🎮 Scene Structure (`ShootingGame.unity`)

| GameObject | Purpose |
|------------|---------|
| `[BuildingBlock] Camera Rig` | OVRCameraRig with tracking |
| `[MR Motif] Arena` | Reference point for avatar sync (child of Camera Rig) |
| `[BuildingBlock] Passthrough` | MR passthrough layer |
| `[BuildingBlock] Network Manager` | Fusion NetworkRunner (Shared Mode) |
| `[BuildingBlock] Auto Matchmaking` | Auto session join for same-room play |
| `[BuildingBlock] Platform Init` | Oculus Platform initialization |
| `[BuildingBlock] Networked Avatar` | Meta Avatar spawning |
| `[BuildingBlock] MR Utility Kit` | MRUK room scanning |
| `[BuildingBlock] Colocation` | Colocation building block |
| `[MR Motif] Spawn Manager` | Open play area spawning logic |
| `[MR Motif] Shooting Game Manager` | Game state, rounds, scoring |
| `[MR Motif] Shooting Setup` | Attaches shooting components to avatars |
| `[MR Motif] Shooting HUD Canvas` | Player HUD (health, kills, death panel) |
| `[MR Motifs] Colocation Manager` | Anchor alignment logic |
| `[MR Motifs] SSA Manager` | Shared Spatial Anchor management |

---

## ✅ Implemented Features

### Core Gameplay
| Feature | Script | Status |
|---------|--------|--------|
| Game states (Waiting/Countdown/Playing/RoundEnd) | `ShootingGameManagerMotif.cs` | ✅ |
| Round timer (3 min default) | `ShootingGameManagerMotif.cs` | ✅ |
| Win condition (10 kills default) | `ShootingGameManagerMotif.cs` | ✅ |
| Score tracking per player | `ShootingGameManagerMotif.cs` | ✅ |
| Manual round restart (hold both grips) | `ShootingGameManagerMotif.cs` | ✅ |
| Auto-restart after round end | `ShootingGameManagerMotif.cs` | ✅ |

### Health & Combat
| Feature | Script | Status |
|---------|--------|--------|
| Networked health (100 HP default) | `PlayerHealthMotif.cs` | ✅ |
| Take damage RPC | `PlayerHealthMotif.cs` | ✅ |
| Death and respawn (3 sec delay) | `PlayerHealthMotif.cs` | ✅ |
| Invulnerability after respawn (2 sec) | `PlayerHealthMotif.cs` | ✅ |
| Kill/death tracking per player | `PlayerHealthMotif.cs` | ✅ |
| Visual hit feedback | `PlayerHealthMotif.cs` | ✅ |

### Weapons & Bullets
| Feature | Script | Status |
|---------|--------|--------|
| Trigger input detection (both hands) | `ShootingPlayerMotif.cs` | ✅ |
| Dual-wield weapon support | `ShootingPlayerMotif.cs` | ✅ |
| Networked bullet spawning | `ShootingPlayerMotif.cs` | ✅ |
| Configurable fire rate (0.2 sec default) | `ShootingPlayerMotif.cs` | ✅ |
| Bullet physics with velocity | `BulletMotif.cs` | ✅ |
| Hit detection (collision + trigger) | `BulletMotif.cs` | ✅ |
| Owner tracking (no self-damage) | `BulletMotif.cs` | ✅ |
| Auto-despawn after lifetime | `BulletMotif.cs` | ✅ |
| Trail renderer effect | `BulletMotif.cs` | ✅ |

### HUD & UI
| Feature | Script | Status |
|---------|--------|--------|
| Health slider with text | `ShootingHUDMotif.cs` | ✅ |
| Kills/deaths display | `ShootingHUDMotif.cs` | ✅ |
| Death panel with respawn countdown | `ShootingHUDMotif.cs` | ✅ |
| Hit markers on successful hits | `ShootingHUDMotif.cs` | ✅ |
| Damage indicator when hit | `ShootingHUDMotif.cs` | ✅ |
| Scoreboard panel | `ShootingGameManagerMotif.cs` | ✅ |

### Networking & Co-location
| Feature | Script | Status |
|---------|--------|--------|
| Photon Fusion 2 Shared Mode | Building Block | ✅ |
| Auto matchmaking (same session) | Building Block | ✅ |
| Host migration support | `HostMigrationHandlerMotif.cs` | ✅ |
| Anchor creation (3 modes) | `SharedSpatialAnchorManager.cs` | ✅ |
| Anchor advertisement/discovery | `SharedSpatialAnchorManager.cs` | ✅ |
| Camera rig alignment to anchor | `ColocationManager.cs` | ✅ |
| Calibration error tracking | `ColocationManager.cs` | ✅ |
| Open play area spawning | `SpawnManagerMotif.cs` | ✅ |

### Avatars
| Feature | Script | Status |
|---------|--------|--------|
| Meta Avatars with networking | Building Block | ✅ |
| Avatar position sync via object-of-interest | `AvatarMovementHandlerMotif.cs` | ✅ |
| Player name tags | `AvatarNameTagHandlerMotif.cs` | ✅ |

### Platform Features
| Feature | Script | Status |
|---------|--------|--------|
| Guardian boundary suppression | `BoundaryDisablerMotif.cs` | ✅ |
| Group presence | `GroupPresenceAndInviteHandlerMotif.cs` | ✅ |
| Deep link invite handling | `InvitationAcceptanceHandlerMotif.cs` | ✅ |

---

## 🔧 Configuration

### Game Settings
| Setting | Default | Location |
|---------|---------|----------|
| Round Duration | 180 sec | `ShootingGameManagerMotif.m_roundDuration` |
| Kills to Win | 10 | `ShootingGameManagerMotif.m_killsToWin` |
| Min Players | 2 | `ShootingGameManagerMotif.m_minPlayersToStart` |
| Auto Restart | true | `ShootingGameManagerMotif.m_autoRestart` |
| Auto Restart Delay | 10 sec | `ShootingGameManagerMotif.m_autoRestartDelay` |

### Player Settings
| Setting | Default | Location |
|---------|---------|----------|
| Max Health | 100 | `PlayerHealthMotif.m_maxHealth` |
| Respawn Delay | 3 sec | `PlayerHealthMotif.m_respawnDelay` |
| Invulnerability | 2 sec | `PlayerHealthMotif.m_invulnerabilityDuration` |

### Weapon Settings
| Setting | Default | Location |
|---------|---------|----------|
| Fire Force | 15 | `ShootingPlayerMotif.m_fireForce` |
| Fire Rate | 0.2 sec | `ShootingPlayerMotif.m_fireRate` |
| Bullet Lifetime | 5 sec | `ShootingPlayerMotif.m_bulletLifetime` |
| Bullet Damage | 10 | `BulletMotif.m_damage` |

### Co-location Settings
| Setting | Default | Location |
|---------|---------|----------|
| Anchor Placement Mode | AtHostPosition | `SharedSpatialAnchorManager.m_anchorPlacementMode` |
| Manual Placement Button | Primary Index Trigger | `SharedSpatialAnchorManager.m_placementButton` |

---

## 📋 Setup Checklist

### Phase 1: Configuration (Required)
- [ ] **Photon App ID**: Go to **Fusion > Fusion Hub** → Enter your App ID from [Photon Dashboard](https://dashboard.photonengine.com)
- [ ] **Oculus App ID**: Go to **Oculus > Platform > Edit Settings** → Enter App ID from [Meta Developer Dashboard](https://developer.oculus.com)
- [ ] Verify `BulletMotif.prefab` is assigned to `ShootingSetupMotif` in scene
- [ ] (Optional) Assign weapon prefab to `ShootingSetupMotif` for visual weapons

### Phase 2: Editor Testing
- [ ] Enter Play mode
- [ ] Verify no console errors
- [ ] Check game state shows "Waiting for Players"

### Phase 3: Single Device Testing
- [ ] Build for Android (Quest)
- [ ] Install on Quest device
- [ ] Verify passthrough works
- [ ] Verify avatar spawns at headset position
- [ ] Verify trigger fires bullets (check visually)

### Phase 4: Two-Device Testing
- [ ] Both devices on same WiFi network
- [ ] Both devices with completed Space Setup
- [ ] Deploy to both devices
- [ ] Verify both players see each other
- [ ] Verify spatial alignment (players in correct positions)
- [ ] Verify game starts when 2 players present

### Phase 5: Combat Testing
- [ ] Bullets visible to both players
- [ ] Hit detection works (damage applied)
- [ ] Death and respawn cycle works
- [ ] Scoreboard updates correctly
- [ ] Win condition triggers round end

---

## 🚀 Quick Start

### Prerequisites
- Unity 6 (6000.0.x)
- Meta XR SDK packages installed
- Photon Fusion 2 SDK
- Valid Photon App ID (get from [Photon Dashboard](https://dashboard.photonengine.com))
- Valid Oculus Platform App ID (get from [Meta Developer Dashboard](https://developer.oculus.com))
- Two Meta Quest devices for co-location testing

### Setup
1. Open `Assets/Scenes/ShootingGame.unity`
2. Configure Photon: **Fusion > Fusion Hub** → Set App ID
3. Configure Oculus: **Oculus > Platform > Edit Settings** → Set App ID
4. Build for Android (Quest)

### Testing Co-location
1. Both Quest headsets must have completed **Space Setup** in the same room
2. Deploy app to both devices
3. Launch on both devices (same WiFi network)
4. Host starts game → creates spatial anchor
5. Client discovers session → aligns to shared anchor
6. Both players now share the same coordinate system!

---

## 🔮 Future Implementation Plans

### Phase 1: Polish & Bug Fixes
| Feature | Priority | Notes |
|---------|----------|-------|
| Visual weapon models on controllers | High | Assign weapon prefab to `ShootingSetupMotif` |
| Bullet impact VFX | High | Create hit effect prefab for `BulletMotif.m_hitEffectPrefab` |
| Audio feedback (shots, hits, death) | High | Add audio clips to relevant components |
| UI polish (scoreboard, round announcements) | Medium | Improve `ShootingHUDMotif` visuals |
| Calibration error display | Low | Show alignment quality to players |

### Phase 2: Gameplay Enhancements
| Feature | Priority | Notes |
|---------|----------|-------|
| Multiple weapon types | Medium | Create weapon variants (pistol, shotgun, etc.) |
| Ammunition system | Medium | Limited ammo with reload mechanic |
| Power-ups (health, speed, damage) | Medium | Spawnable pickups in play area |
| Team modes (2v2, 3v3) | Medium | Team assignment and team scoring |
| Different game modes (CTF, King of the Hill) | Low | Alternative win conditions |

### Phase 3: Advanced Features
| Feature | Priority | Notes |
|---------|----------|-------|
| Voice chat integration | Medium | Re-add Photon Voice support |
| Spectator mode | Low | Watch ongoing matches |
| Practice mode (single player) | Low | AI targets or solo warm-up |
| Room-aware obstacles | Low | Use MRUK to create cover from furniture |
| Persistent leaderboards | Low | Cloud-saved player stats |
| Custom avatar skins | Low | Unlockable cosmetics |

### Phase 4: Social & Discovery
| Feature | Priority | Notes |
|---------|----------|-------|
| Private room codes | Medium | Invite friends to specific sessions |
| Match history | Low | Track past games |
| Achievements | Low | Meta Platform achievements |
| Cross-session friends | Low | Play with same people again |

---

## 🐛 Known Issues & Troubleshooting

### Players Don't See Each Other
1. Verify both devices completed Space Setup in the same room
2. Check both devices are on the same WiFi network
3. Verify Photon App ID is configured correctly
4. Check console for Fusion connection errors
5. Ensure `AvatarMovementHandlerMotif` is syncing positions

### Bullets Don't Do Damage
1. Verify `BulletMotif` prefab has a collider set as trigger
2. Check avatar has collider for hit detection
3. Verify `TakeDamageRpc` is being called (add debug logs)
4. Ensure bullet's `OwnerPlayer` is set correctly (no self-damage)

### Game Doesn't Start
1. Check `ShootingGameManagerMotif.m_minPlayersToStart` (default: 2)
2. Verify players are registered via `RegisterPlayer()`
3. Check game state in Inspector while running

### Co-location Not Aligning
1. Both devices must have completed **Space Setup** (room scan)
2. Verify host creates anchor (`SharedSpatialAnchorManager` logs)
3. Verify client discovers anchor (check discovery logs)
4. Ensure `ColocationManager.AlignUserToAnchor()` is called
5. Check calibration error isn't too high

### Performance Issues
1. Reduce bullet lifetime (`ShootingPlayerMotif.m_bulletLifetime`)
2. Increase fire rate cooldown (`ShootingPlayerMotif.m_fireRate`)
3. Check for excessive debug logging

---

## 📚 Architecture Deep Dive

### Game State Machine
```
WAITING_FOR_PLAYERS
    ↓ (2+ players connected)
COUNTDOWN (3 seconds)
    ↓
PLAYING
    ↓ (player reaches kill target OR timer expires)
ROUND_END (show winner, 10 sec delay)
    ↓ (auto-restart OR hold both grips)
WAITING_FOR_PLAYERS
```

### Co-location Flow
```
1. Host creates OVRSpatialAnchor at headset position
2. Host saves anchor and gets UUID
3. Host advertises session via OVRColocationSession
4. Client discovers nearby session
5. Client loads anchor using shared UUID
6. ColocationManager aligns client's camera rig to anchor
7. All players now share identical world coordinates
```

### Networking Architecture (Fusion 2 Shared Mode)
```
NetworkRunner (Shared Mode)
├── Auto Matchmaking → Players auto-join same session
├── Host = State Authority for game state
├── Each player = Input Authority for their avatar
├── [Networked] properties → Auto-synced values
├── RPCs → For events (damage, game state changes)
└── Host Migration → Seamless takeover if host leaves
```

### Open Play Area Spawning
```
1. Player connects to Fusion session
2. SpawnManagerMotif.GetSpawnLocation() returns headset position
3. Avatar spawns at physical location (no teleportation)
4. Player is already "there" - no movement needed
```

---

## ❌ Explicitly Out of Scope (Not MVP)

- Voice chat (removed Photon Voice dependency)
- Predefined spawn points (using open play area)
- Seeing other players' health bars overhead
- Weapon pickups on the ground
- Environment destruction
- AI opponents

---

## 🔗 Dependencies

| Package | Purpose |
|---------|---------|
| Meta XR Core SDK | OVRManager, OVRCameraRig, OVRPassthroughLayer |
| Meta MR Utility Kit | MRUK for room awareness |
| Meta Avatars SDK | AvatarEntity, networked avatars |
| Photon Fusion 2 | NetworkRunner, NetworkBehaviour, RPCs |
| TextMesh Pro | UI text rendering |

---

## License

Based on Meta MR Motifs sample code. See individual files for copyright notices.
