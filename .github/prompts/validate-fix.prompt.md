---
agent: agent
---


Read the README.md file in this project to understand the current state. Then focus on:

1. **Investigate Calibration Error Discrepancy**: The MetricsLogger shows calibration_error_mm = 659mm on H1 (Client) but 0mm on H2 (Host). Determine if this is expected behavior (host creates anchor at own position) or a bug in CalibrationAccuracyTracker.

2. **Collect and Analyze Metrics**: Use these ADB commands to pull latest session data:
   - H1: `"C:/Users/jonfriis/Android/Sdk/platform-tools/adb.exe" -s 2G0YC1ZF8B07WD shell ls /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/`
   - H2: `"C:/Users/jonfriis/Android/Sdk/platform-tools/adb.exe" -s 2G0YC5ZF9F00N1 shell ls /sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/`

3. **Review MetricsLogger.cs** at `Assets/Scripts/Shared/Metrics/MetricsLogger.cs` - it was recently updated to use NetworkRunner.Instances for reliable network state detection.

The project uses Unity MCP tools - prefer using them for scene/GameObject operations over writing scripts.
```

---

## Detailed Context

### What Was Completed (Dec 6, 2025)

1. **MetricsLogger Simplified**: Reduced from 14 columns to 10 essential columns
2. **NetworkRunner Detection Fixed**: Changed from `FindAnyObjectByType<NetworkRunner>()` to `NetworkRunner.Instances` iteration
3. **Verified Working**: Both headsets now correctly report network state (Host/Client)

### Current MetricsLogger CSV Format
```
session_id,headset_id,participant_count,timestamp_sec,frame_rate_fps,network_latency_ms,calibration_error_mm,battery_temp_c,battery_level,scene_state
```

### Test Devices
| Device | Serial | Headset ID |
|--------|--------|------------|
| H1 | `2G0YC1ZF8B07WD` | `H_4193` |
| H2 | `2G0YC5ZF9F00N1` | `H_6444` |

### Key Files
- `Assets/Scripts/Shared/Metrics/MetricsLogger.cs` - Main metrics collection
- `Assets/Scripts/Shared/Metrics/CalibrationAccuracyTracker.cs` - Calibration drift tracking
- `Assets/Scripts/Colocation/ColocationManager.cs` - Anchor alignment
- `Assets/Scripts/Colocation/SharedSpatialAnchorManager.cs` - Anchor creation/sharing

### Open Issues to Investigate

#### 1. Calibration Error Discrepancy
**Symptom**: H1 shows 659mm, H2 shows 0mm calibration error

**Questions to answer**:
- Is the host always at 0mm because they create the anchor at their position?
- Should CalibrationAccuracyTracker measure drift from initial calibration instead?
- Is this expected behavior or a measurement bug?

**Files to review**:
- `CalibrationAccuracyTracker.cs` - How is reference position set?
- `ColocationManager.cs` - `AlignUserToAnchor()` and calibration callbacks

#### 2. Frame Rate Variance
**Symptom**: H1 has more FPS variance (50-75) vs H2 (stable 70-74)

**Possible causes**:
- H1 is client (more network overhead?)
- Different thermal states
- Background processes

### Unity MCP Workflow

This project uses Unity MCP for direct editor manipulation. Key tools:
- `mcp_ai-game-devel_Scene_GetHierarchy` - View scene structure
- `mcp_ai-game-devel_GameObject_Find` - Inspect components
- `mcp_ai-game-devel_Script_Read` - Read script content
- `mcp_ai-game-devel_Console_GetLogs` - Check for errors

**⚠️ CRITICAL**: Only send ONE MCP tool request at a time - the Unity MCP server can crash with parallel requests.

### Meta Quest MCP (HzOSDevMCP)

For device debugging and documentation:
- `mcp_hzosdevmcp_get_device_logcat` - Get device logs
- `mcp_hzosdevmcp_get_unity_documentation_index` - Meta Quest Unity docs
- `mcp_hzosdevmcp_fetch_meta_quest_doc` - Fetch specific doc pages

---

## Research Paper Context

This is a research project studying co-located MR experiences. The MetricsLogger collects data for analysis. Key metrics:
- **network_latency_ms**: Measured via Fusion's `GetPlayerRtt()`
- **calibration_error_mm**: Distance between expected and actual anchor position
- **participant_count**: Number of players in session
- **scene_state**: Network role (Host/Client/NotConnected)

Data is stored at: `/sdcard/Android/data/com.jJFiisJ.ArenaShooting/files/metrics/`
