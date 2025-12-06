#!/bin/bash
#
# extract_metrics.sh - Extract metrics from connected Meta Quest devices
#
# Usage: ./extract_metrics.sh [session_name]
#

set -e

SESSION_NAME="${1:-$(date +%Y%m%d)}"
OUTPUT_DIR="research-paper/data/sessions"
PACKAGE_NAME="com.weareheadset.MetaColocationDemos_Official"

# Find ADB
if command -v adb &> /dev/null; then
    ADB="adb"
elif [ -f "$HOME/Android/Sdk/platform-tools/adb" ]; then
    ADB="$HOME/Android/Sdk/platform-tools/adb"
elif [ -f "/opt/android-sdk/platform-tools/adb" ]; then
    ADB="/opt/android-sdk/platform-tools/adb"
else
    echo "Error: ADB not found. Please install Android SDK platform-tools."
    exit 1
fi

echo "Using ADB: $ADB"

# Get connected devices
DEVICES=$($ADB devices | grep -E "^\S+\s+device$" | awk '{print $1}')

if [ -z "$DEVICES" ]; then
    echo "Error: No devices connected. Please connect your Meta Quest headset(s) via USB."
    exit 1
fi

DEVICE_COUNT=$(echo "$DEVICES" | wc -l)
echo "Found $DEVICE_COUNT device(s)"

# Create output directory
SESSION_DIR="$OUTPUT_DIR/$SESSION_NAME"
mkdir -p "$SESSION_DIR"

echo "Output directory: $SESSION_DIR"

DEVICE_INDEX=1
TOTAL_FILES=0

for DEVICE in $DEVICES; do
    HEADSET_DIR="$SESSION_DIR/H$DEVICE_INDEX"
    mkdir -p "$HEADSET_DIR"
    
    echo ""
    echo "Processing device $DEVICE_INDEX ($DEVICE)..."
    
    # Get device info
    DEVICE_MODEL=$($ADB -s "$DEVICE" shell getprop ro.product.model 2>/dev/null | tr -d '\r')
    ANDROID_VERSION=$($ADB -s "$DEVICE" shell getprop ro.build.version.release 2>/dev/null | tr -d '\r')
    BATTERY_LEVEL=$($ADB -s "$DEVICE" shell "dumpsys battery | grep level" 2>/dev/null | tr -d '\r')
    
    cat > "$HEADSET_DIR/device_info.json" << EOF
{
    "serial": "$DEVICE",
    "model": "$DEVICE_MODEL",
    "androidVersion": "$ANDROID_VERSION",
    "batteryLevel": "$BATTERY_LEVEL",
    "extractedAt": "$(date -Iseconds)"
}
EOF
    echo "  Saved device info"
    
    # Pull metrics files
    REMOTE_METRICS_DIR="/sdcard/Android/data/$PACKAGE_NAME/files/metrics/"
    
    # List and pull files
    REMOTE_FILES=$($ADB -s "$DEVICE" shell "ls $REMOTE_METRICS_DIR 2>/dev/null" 2>/dev/null || echo "")
    
    if [ -n "$REMOTE_FILES" ]; then
        echo "  Found metrics files:"
        
        for FILE in $REMOTE_FILES; do
            FILE=$(echo "$FILE" | tr -d '\r')
            if [ -n "$FILE" ]; then
                REMOTE_PATH="${REMOTE_METRICS_DIR}${FILE}"
                LOCAL_PATH="$HEADSET_DIR/$FILE"
                
                echo "    Pulling: $FILE"
                $ADB -s "$DEVICE" pull "$REMOTE_PATH" "$LOCAL_PATH" 2>/dev/null || true
                
                if [ -f "$LOCAL_PATH" ]; then
                    TOTAL_FILES=$((TOTAL_FILES + 1))
                fi
            fi
        done
    else
        echo "  No metrics files found"
    fi
    
    DEVICE_INDEX=$((DEVICE_INDEX + 1))
done

# Create session summary
cat > "$SESSION_DIR/session_info.json" << EOF
{
    "sessionName": "$SESSION_NAME",
    "extractedAt": "$(date -Iseconds)",
    "deviceCount": $DEVICE_COUNT,
    "totalFilesExtracted": $TOTAL_FILES
}
EOF

echo ""
echo "========================================"
echo "Extraction complete!"
echo "  Session: $SESSION_NAME"
echo "  Devices: $DEVICE_COUNT"
echo "  Files extracted: $TOTAL_FILES"
echo "  Output: $SESSION_DIR"
echo "========================================"
