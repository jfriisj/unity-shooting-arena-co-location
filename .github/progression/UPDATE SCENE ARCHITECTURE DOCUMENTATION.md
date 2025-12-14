@workspace /agent **GENERATE DYNAMIC SCENE DOCUMENTATION**

**Objective:**
Analyze the currently open Unity Scene and generate/update the `Docs/scene-architecture.md` file. The documentation must reflect the *actual* hierarchy and component configuration found in the scene.

**Execution Strategy:**
1.  **Scene Scan:**
    * Execute a script to traverse the Scene Hierarchy.
    * Identify top-level categories (e.g., Core Systems, Game Systems, UI).
    * For key GameObjects (Managers, BuildingBlocks, Zones), list their attached Components.
2.  **Logic Trace:**
    * Check important public references on Manager scripts (e.g., "What is assigned to `m_spawnManager` on `InitManager`?").
    * Note these connections in the documentation.
3.  **Document Generation:**
    * Rewrite `Docs/scene-architecture.md`.
    * Structure it cleanly: Hierarchy Tree -> Component Details -> Data Flow.
    * *Crucial:* If a GameObject or Component exists in the scene but was missing from old docs, ADD IT. If something was deleted, REMOVE IT.

**Output:** The updated Markdown file content.

**Action:** perform the scan and update the documentation file.