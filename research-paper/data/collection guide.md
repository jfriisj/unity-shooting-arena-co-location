# Metrics Collection Guide - Co-Located VR System

## Overview

This guide provides step-by-step instructions for collecting technical performance and collaboration metrics using Meta Quest 3 headsets in the MetaColocationDemos co-located VR system with Unity Netcode for GameObjects.

---

## Table of Contents

1. [Pre-Session Setup](#pre-session-setup)
2. [Hardware Preparation](#hardware-preparation)
3. [Network Configuration](#network-configuration)
4. [Unity Project Configuration](#unity-project-configuration)
5. [Calibration Procedure](#calibration-procedure)
6. [Data Collection During Sessions](#data-collection-during-sessions)
7. [Post-Session Data Extraction](#post-session-data-extraction)
8. [Data Analysis](#data-analysis)
9. [Troubleshooting](#troubleshooting)

---

## Pre-Session Setup

### Required Equipment

**Hardware:**
- 2-3× Meta Quest 3 headsets (fully charged)
- WiFi router/access point (802.11ac or newer recommended)
- Development PC with Unity 6000.0.62f1
- USB-C cables for headset connection

**Software:**
- Unity 6000.0.62f1 with Android Build Support
- Meta XR All-in-One SDK
- Unity Netcode for GameObjects
- ADB (Android Debug Bridge) - typically at `C:\Users\<username>\Android\Sdk\platform-tools\adb.exe`
- Python 3.8+ with pandas, numpy, matplotlib (for analysis)

**Note:** This guide describes the technical capabilities of the MetricsLogger system. No formal user studies have been conducted. This is a technical demonstration prototype.

**Physical Space:**
- Clear play area (minimum 3m × 3m per user)
- Good lighting for hand/controller tracking
- Headsets must be within Bluetooth range for colocation discovery (~10m)

### Headset Labeling

Label each headset clearly:
```
Headset 1 (H1) - Host
Headset 2 (H2) - Client
Headset 3 (H3) - Client (optional)
```

---

## Hardware Preparation

### 1. Charge All Headsets

Ensure all headsets are at **>90% battery** before each session:
```bash
# Check battery levels via ADB
adb devices
adb -s <DEVICE_SERIAL> shell dumpsys battery | grep level
```

### 2. Enable Developer Mode

On each headset:
1. Go to **Settings** → **System** → **Developer**
2. Enable **Developer Mode**
3. Enable **USB Debugging**

### 3. Configure Guardian Boundaries

Set up Guardian boundaries on all headsets in the shared physical space.

---

## Network Configuration

### WiFi Setup

**Requirements:**
1. All headsets on same WiFi network (5GHz recommended)
2. Bluetooth enabled on all headsets (required for Colocation Discovery)
3. Headsets physically near each other (within ~10m for Bluetooth discovery)

**Verify Network Connectivity:**
```bash
# Get IP addresses
adb -s <DEVICE_SERIAL> shell ip addr show wlan0 | grep inet
```

---

## Unity Project Configuration

### Scene Components

The following components are already configured in `ColocationLightSaberDemo.unity`:

| Component | GameObject | Purpose |
|-----------|------------|---------|
| **MetricsLogger** | `[MetricsLogger]` | Main metrics collection & CSV export |
| **NetworkLatencyTracker** | `[BuildingBlock] Network Manager` | Real-time RTT measurement via ping/pong RPCs |
| **CalibrationAccuracyTracker** | `[BuildingBlock] Shared Spatial Anchor Core` | Spatial alignment tracking |
| **GroupPresenceSetup** | `[BuildingBlock] Platform Init` | Enables invite functionality |

### MetricsLogger Configuration

The `MetricsLogger` component has these settings:

| Setting | Default | Description |
|---------|---------|-------------|
| `logInterval` | 1.0 | Seconds between metric samples |
| `enableLogging` | true | Enable/disable logging |
| `sessionId` | (auto) | Auto-generated from timestamp if empty |
| `showDebugUI` | true | Show on-screen debug overlay |

### Metrics Collected

**Technical Metrics** (logged every 1 second):

| Metric | Column Name | Description | Target |
|--------|-------------|-------------|--------|
| Session ID | `session_id` | Unique session identifier | - |
| Headset ID | `headset_id` | Unique device hash (H_XXXX) | - |
| Participants | `participant_count` | Connected clients count | - |
| Timestamp | `timestamp_sec` | Seconds since session start | - |
| Frame Rate | `frame_rate_fps` | Current FPS (0.5s average) | ≥90 |
| Frame Time | `frame_time_ms` | Time per frame | ≤11.1ms |
| Network Latency | `network_latency_ms` | Round-trip time via RPC ping | ≤75ms |
| Packet Loss | `packet_loss_pct` | Lost packets percentage | <1% |
| Calibration Error | `calibration_error_mm` | Spatial alignment accuracy | <10mm |
| Temperature | `headset_temp_c` | Estimated device temperature | <42°C |
| Battery Level | `battery_level` | Battery percentage | - |
| Scene State | `scene_state` | Host/Client/Offline | - |

### Data Persistence

- **Incremental saves**: Data appends to CSV every 60 seconds (no overwrites)
- **Pause/quit saves**: Remaining data saved on app pause or quit
- **Session files**: Each session creates unique timestamped files
- **Safe recovery**: Data preserved even if app crashes (up to last 60s save)

### Build and Deploy

```bash
# Build APK in Unity: File → Build Settings → Build

# Install on headsets
adb -s <DEVICE_SERIAL> install -r MetaColocationDemos.apk
```

---

## Calibration Procedure

### Colocation Discovery Flow

The system uses Meta's Colocation Discovery API with shared spatial anchors:

1. **Host Device** launches app first
   - Creates spatial anchor at current position
   - Advertises via Bluetooth Low Energy
   - Shares anchor UUID with clients

2. **Client Devices** launch app
   - Discover host via Bluetooth
   - Retrieve shared spatial anchor UUID
   - Localize to the same spatial reference frame

3. **Automatic Alignment**
   - ColocationController aligns camera rigs
   - CalibrationAccuracyTracker monitors alignment error
   - Target: <10mm alignment error

### Verification

The MetricsLogger debug overlay shows:
```
[MetricsLogger] Session: 20251204_143000
FPS: 90.2 | Temp: 34.5°C | Battery: 87%
Latency: 28.4ms | Participants: 2 | State: Host
Metrics Logged: 245
```

---

## Data Collection During Sessions

### Automatic Logging

All metrics are logged automatically by the MetricsLogger component at 1Hz sampling rate.

### File Locations

**On Quest Device:**
```
/sdcard/Android/data/com.weareheadset.MetaColocationDemos_Official/files/metrics/
├── session_20251204_143000_H_1234.csv      # Metrics data
└── session_20251204_143000_H_1234_metadata.json  # Session info
```

### Session Metadata (JSON)

```json
{
    "sessionId": "20251204_143000",
    "headsetId": "H_1234",
    "deviceName": "Quest 3",
    "deviceModel": "Quest 3",
    "startTime": "2025-12-04T14:30:00+01:00",
    "endTime": "2025-12-04T15:15:00+01:00",
    "durationMinutes": 45.0,
    "totalMetrics": 2700,
    "unityVersion": "6000.0.62f1",
    "appVersion": "1.0"
}
```

---

## Post-Session Data Extraction

### Using PowerShell (Windows)

```powershell
# Navigate to project root
cd C:\github\MetaColocationDemos

# Run extraction script
.\extract_metrics.ps1 -SessionName "my_session"
```

### Using Bash (Mac/Linux)

```bash
cd /path/to/MetaColocationDemos
./extract_metrics.sh my_session
```

### Manual Extraction

```bash
# Set package name
PACKAGE="com.weareheadset.MetaColocationDemos_Official"

# Create output directory
mkdir -p research-paper/data/sessions/$(date +%Y%m%d)

# Pull metrics from each headset
adb -s <DEVICE_SERIAL> pull /sdcard/Android/data/$PACKAGE/files/metrics/ ./H1/

# Pull Unity logs for debugging
adb -s <DEVICE_SERIAL> logcat -d | grep -E "MetricsLogger|Unity" > unity_logs.txt
```

### Expected File Structure

```
research-paper/data/sessions/
└── 20251204_session1/
    ├── H1/
    │   ├── session_20251204_143000_H_1234.csv
    │   ├── session_20251204_143000_H_1234_metadata.json
    │   └── device_info.json
    ├── H2/
    │   ├── session_20251204_143000_H_5678.csv
    │   ├── session_20251204_143000_H_5678_metadata.json
    │   └── device_info.json
    └── session_info.json
```

---

## Data Analysis

### CSV Format

```csv
session_id,headset_id,participant_count,timestamp_sec,frame_rate_fps,frame_time_ms,network_latency_ms,packet_loss_pct,calibration_error_mm,headset_temp_c,battery_level,scene_state
20251204_143000,H_1234,2,0.00,90.2,11.08,28.4,0.000,4.20,34.5,95,Host
20251204_143000,H_1234,2,1.00,90.1,11.10,29.1,0.000,4.25,34.6,95,Host
```

### Merge Data from All Headsets

```python
import pandas as pd
import glob

def merge_session_data(session_dir):
    """Merge metrics from all headsets"""
    all_data = []
    
    for headset_dir in glob.glob(f"{session_dir}/H*/"):
        csv_files = glob.glob(f"{headset_dir}/*.csv")
        for csv_file in csv_files:
            df = pd.read_csv(csv_file)
            all_data.append(df)
    
    merged_df = pd.concat(all_data, ignore_index=True)
    merged_df = merged_df.sort_values(['headset_id', 'timestamp_sec'])
    
    output_file = f"{session_dir}/merged_metrics.csv"
    merged_df.to_csv(output_file, index=False)
    print(f"Merged {len(merged_df)} rows to: {output_file}")
    return merged_df
```

### Calculate Summary Statistics

```python
def calculate_statistics(df):
    """Generate summary statistics"""
    stats = {
        'duration_min': df['timestamp_sec'].max() / 60,
        'mean_fps': df['frame_rate_fps'].mean(),
        'min_fps': df['frame_rate_fps'].min(),
        'mean_latency_ms': df['network_latency_ms'].mean(),
        'max_latency_ms': df['network_latency_ms'].max(),
        'mean_calibration_mm': df['calibration_error_mm'].mean(),
        'fps_target_achieved': (df['frame_rate_fps'] >= 90).mean() * 100,
        'latency_target_achieved': (df['network_latency_ms'] <= 75).mean() * 100,
    }
    return stats
```

### Use Existing Analysis Script

```bash
cd research-paper/scripts
python analyze_data.py
```

---

## Troubleshooting

### MetricsLogger Not Saving Data

```
Solution:
1. Check Unity console for [MetricsLogger] errors
2. Verify storage permissions in Android manifest
3. Check available storage: adb shell df /sdcard
4. Ensure enableLogging = true in inspector
```

### No Network Latency Data (0ms)

```
Solution:
1. Verify NetworkLatencyTracker is on a NetworkObject
2. Check that headsets are connected (scene_state = Host/Client)
3. NetworkLatencyTracker only measures on clients, not host
```

### Calibration Error Always 0mm

```
Solution:
1. Verify CalibrationAccuracyTracker is in scene
2. Call CalibrationAccuracyTracker.OnCalibrationComplete() after colocation
3. Check ColocationController is properly configured
```

### High Network Latency (>75ms)

```
Solution:
1. Ensure both headsets on same 5GHz WiFi network
2. Check for WiFi interference
3. Move closer to router
4. Verify QoS settings if available
```

### Colocation Discovery Failing

```
Solution:
1. Ensure Bluetooth is enabled on all headsets
2. Headsets must be within ~10m of each other
3. Check AndroidManifest has USE_COLOCATION_DISCOVERY_API permission
4. Verify Group Presence is set (GroupPresenceSetup component)
```

---

## Session Checklist

### Pre-Session

- [ ] Charge headsets to >90%
- [ ] Verify WiFi connectivity (same network)
- [ ] Verify Bluetooth enabled
- [ ] Install latest APK build
- [ ] Clear old metrics data (optional)

### Start of Session

- [ ] Launch app on Host first
- [ ] Wait for colocation discovery
- [ ] Verify debug overlay shows correct state
- [ ] Confirm participant count matches

### During Session

- [ ] Monitor FPS on debug overlay
- [ ] Watch for tracking issues
- [ ] Note any incidents

### End of Session

- [ ] Close app gracefully (data saves on quit)
- [ ] Extract data via PowerShell/bash script
- [ ] Verify CSV files contain data
- [ ] Backup to cloud storage

---

## Appendix: Component Scripts

### MetricsLogger.cs
Location: `Assets/Scripts/MetricsLogger.cs`
- Main metrics collection
- CSV export with append mode
- Session metadata JSON export
- On-screen debug overlay

### NetworkLatencyTracker.cs
Location: `Assets/Scripts/NetworkLatencyTracker.cs`
- Ping/pong RPC-based RTT measurement
- Packet loss tracking
- Min/max/average statistics

### CalibrationAccuracyTracker.cs
Location: `Assets/Scripts/CalibrationAccuracyTracker.cs`
- Spatial alignment error tracking
- Recalibration event logging
- Threshold alerting (>10mm)

### Data Extraction Scripts
- `extract_metrics.ps1` - PowerShell (Windows)
- `extract_metrics.sh` - Bash (Mac/Linux)

---

**Version:** 3.0  
**Last Updated:** December 4, 2025  
**Project:** MetaColocationDemos

**Disclaimer:** This guide describes technical capabilities of the MetricsLogger system for performance validation. The prototype is designed for technical demonstration and future empirical research.
