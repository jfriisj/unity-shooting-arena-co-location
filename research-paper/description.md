# 🛸 Project Description: Arena Drone

**Arena Drone** is a multiplayer Mixed Reality (MR) shooter for Meta Quest 3, where the players' physical living room is transformed into a futuristic battle zone. The project combines advanced room scanning with fast-paced multiplayer action.

### 🎯 Vision
To create an intense, cooperative experience where players are physically in the same room (Co-location) and fight back-to-back against waves of invading drones that break through the walls of their own homes.

### 🛠️ Core Technologies
* **Game Engine:** Unity 6 (Universal Render Pipeline).
* **Hardware:** Meta Quest 3 (focus on Passthrough and Depth API).
* **Networking:** Photon Fusion (Shared Mode) for synchronizing players, enemies and projectiles.
* **Mixed Reality:** Meta MR Utility Kit (MRUK) to scan the room and generate "Scene Anchors" (walls, floors, tables) that the game interacts with.
* **Platform:** Meta Core SDK & Interaction SDK for handling VR input and avatars.

### 🌟 Key Features (The "Big 5")

1. **Seamless Co-location:**
* Players automatically share the same coordinate system via "Shared Spatial Anchors".
* When you see a virtual gun on the table, your friend sees the same gun on the same table.
* No manual calibration required after initial setup.

2. **Room-Aware Gameplay:**
* The game understands the geometry of the room. Drones do not fly through walls, but spawn *on* them.
* Furniture (tables, sofas) act as cover or spawn points for weapons.

3. **Network Sync:**
* High-frequency synchronization of avatars (head/hands) and weapons.
* Host-authoritarian logic controls the drones' AI and the game state (Wave 1, 2, 3...) to avoid cheating and desynchronization.

4. **Tactile Weapon Mechanics:**
* Physical weapons that must be picked up, aimed, and reloaded manually.
* Realistic ballistics with projectiles that react to gravity and collision.

5. **Intelligent Drone AI:**
* Enemies that chase the player dynamically.
* Wave System with increasing difficulty.
* Visual and auditory feedback (warning lights before attacks).

---

# 🗺️ Project Roadmap

This roadmap shows the journey from empty Unity scene to finished game. We are currently in the transition between **Phase 2** and **Phase 3**.

###  Phase 1: Core Systems
*Goal: To get two players to see each other in the same physical space.*
* **[ ] Feature 1: Networking Basics:** Photon Fusion setup, connecting to lobby.
* **[ ] Feature 2: Room Awareness:** Integration of MRUK, generation of wall/floor mesh.
* **[ ] Feature 3: Co-location & Avatars:** Sharing of Spatial Anchors, so players line up. Synchronization of Meta Avatars (or bots).

###  Phase 2: Gameplay Loop (Mechanics)
*Goal: To give players something to do (shoot and survive).*
* **[ ] Feature 4: Weapons:** Networked guns, physical projectiles (bullets), "Grabbable" interaction.
* **[ ] Feature 5: Enemy Drones:** AI drones, wave manager, damage system, win/loss logic.

### 🚧 Phase 3: Integration & Polish (Current Focus)
*Goal: To tie the systems together into a playable product with feedback.*
* **[-] Feature 6: Player HUD & UI:**
* [ ] Health, Ammo and Wave number visibility.
* [ ] Scoreboard integration in the scene.
* [ ] Victory/Defeat screens.
* **[ ] Feature 7: Scene Integration (The "Glue"):**
* [ ] Correct placement of Managers in the scene (what we are working on now via `requirements.md` above).
* [ ] Setup of the "Ready Zone" (starting area).

### 🔮 Phase 4: "Juice" & Release (Future)
*Goal: To make the experience delicious and flawless.*
* **[ ] Audio Polish:** 3D Spatial Audio, sound effects for all actions.
* **[ ] Visual FX:** Nicer explosions, bullet trails, hit markers.
* **[ ] Progression:** Highscore list (Local/Global), multiple weapon types.
* **[ ] Stress Test:** 4-player test, network optimization.

---

