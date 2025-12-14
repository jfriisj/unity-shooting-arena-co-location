@workspace /agent **EXECUTE DYNAMIC CODEBASE HYGIENE SWEEP**

**Objective:**
Analyze the entire `Assets/Scripts/` directory to identify **Redundant**, **Duplicated**, or **Unused** code.
The goal is to reduce technical debt and avoid conflicts (like the `RoomSharingMotif` vs `SpaceSharingManager` issue).

**Constraint:**
**DO NOT DELETE** any files yet. Your output must be a "Cleanup Report". Safety first.

**Phase 1: Functional Overlap Analysis (The "Duplicate" Hunter)**
* **Task:** Read all script contents and look for classes that share significant logic patterns or identical responsibilities.
* **Focus Areas:**
    * **Colocation:** Look for multiple scripts handling `ShareRoom`, `LoadScene`, or `SpatialAnchor`.
    * **Spawning:** Check if multiple managers handle weapon/drone spawning overlappingly.
    * **Input:** Check for redundant input handlers.
* **Criteria:** If two scripts inherit from `NetworkBehaviour` and touch the same subsystems (MRUK, OVR), flag them as "High Risk of Conflict".

**Phase 2: The "Usage" Audit (The "Zombie" Hunter)**
* **Task:** Generate and execute a C# Editor Script (`ScriptUsageDetector.cs`) to find scripts that are NOT assigned to any GameObject in the Scene or Prefabs.
* **Script Logic:**
    1.  Get all `MonoScript` assets in the project.
    2.  Scan the current Scene and all Prefabs in `Assets/`.
    3.  Count usage of each script.
    4.  Report scripts with **0 references**.
    * *Exclusion:* Ignore Editor scripts or Abstract classes.

**Phase 3: Deep Code Analysis**
* **Task:** Analyze the scripts flagged in Phase 1 & 2.
* **Check:**
    * Are there `[SerializeField]` variables that are never read?
    * Are there `Update()` loops that are empty or do nothing?
    * Are there `[ServerRpc]` calls that are never invoked?

**Output Format: The Hygiene Report**
Produce a structured report:

"
## 🧹 CODEBASE HYGIENE REPORT

### ⚠️ FUNCTIONAL DUPLICATES (Action: Consolidate)
* **Conflict A:** `ScriptA.cs` vs `ScriptB.cs`
    * *Reason:* Both handle [Logic]. `ScriptA` seems to be the legacy version.
    * *Recommendation:* Remove `ScriptA`, migrate unique logic to `ScriptB`.

### 🧟 UNUSED / ORPHANED SCRIPTS (Action: Delete)
* `OldScript.cs` (0 Scene References, 0 Code References)
* `TestManager.cs`

### 📉 DEAD CODE BLOCKS (Action: Cleanup)
* `ShootingPlayer.cs`: The method `UnusedFunction()` is never called.
"

**Action:** Execute the scene scan script and generate the full analysis.