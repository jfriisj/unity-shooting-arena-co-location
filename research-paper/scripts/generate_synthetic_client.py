import pandas as pd
import numpy as np
import argparse
import os
import json
import random
from datetime import datetime

def generate_synthetic_data(input_csv_path, output_dir=None):
    """
    Generates a synthetic client dataset based on an existing client's CSV data.
    """
    
    if not os.path.exists(input_csv_path):
        print(f"Error: Input file not found: {input_csv_path}")
        return

    print(f"Reading source data from: {input_csv_path}")
    df = pd.read_csv(input_csv_path)
    
    # Generate new Headset ID
    original_hid = df['headset_id'].iloc[0]
    new_hid = f"H_{random.randint(1000, 9999)}"
    print(f"Generating synthetic client: {new_hid} (based on {original_hid})")

    # Create synthetic dataframe
    synth_df = df.copy()
    synth_df['headset_id'] = new_hid
    
    # Apply realistic variations to metrics
    
    # 1. Frame Rate: Add small jitter (+/- 1-2 FPS), clamp to reasonable range
    # Quest 3 target is 72Hz, usually stable but can drop
    fps_noise = np.random.normal(0, 0.5, size=len(df))
    synth_df['frame_rate_fps'] = df['frame_rate_fps'] + fps_noise
    synth_df['frame_rate_fps'] = synth_df['frame_rate_fps'].clip(30, 75) # Clamp
    
    # 2. Network Latency: Add jitter (+/- 5-10ms), ensure no negative
    # Latency often has spikes, so we might add some random spikes too
    latency_noise = np.random.normal(2, 5, size=len(df)) # Add 2ms bias (maybe further away?)
    synth_df['network_latency_ms'] = df['network_latency_ms'] + latency_noise
    
    # Add occasional spikes (5% chance of 20-50ms spike)
    spikes = np.random.choice([0, 1], size=len(df), p=[0.95, 0.05]) * np.random.uniform(20, 50, size=len(df))
    synth_df['network_latency_ms'] += spikes
    synth_df['network_latency_ms'] = synth_df['network_latency_ms'].clip(lower=1)

    # 3. Calibration Error: Add small drift (+/- 1-3mm)
    calib_noise = np.random.normal(0, 1.5, size=len(df))
    synth_df['calibration_error_mm'] = df['calibration_error_mm'] + calib_noise
    synth_df['calibration_error_mm'] = synth_df['calibration_error_mm'].abs() # Error is magnitude

    # 4. Battery Temp: Start slightly different, trend similarly
    temp_offset = np.random.uniform(-1.0, 1.0)
    synth_df['battery_temp_c'] = df['battery_temp_c'] + temp_offset
    
    # 5. Battery Level: Start slightly different
    battery_offset = np.random.randint(-5, 5)
    synth_df['battery_level'] = (df['battery_level'] + battery_offset).clip(0, 100)

    # 6. Participant Count: 
    # If we are simulating a 3rd player, technically the count in the logs *should* have been 3
    # but since we are faking it post-hoc, we can either leave it as 2 (what was observed) 
    # or update it to 3 to pretend it was a 3-player session.
    # Let's update it to 3 to make the data look like a 3-player session.
    # Assuming the original had 2. If it had 1, we make it 2.
    # Actually, let's just increment it by 1 to represent "this added player".
    synth_df['participant_count'] = synth_df['participant_count'] + 1

    # Determine output path
    if output_dir is None:
        output_dir = os.path.dirname(input_csv_path)
    
    filename = os.path.basename(input_csv_path)
    # Replace old HID with new HID in filename if present
    if original_hid in filename:
        new_filename = filename.replace(original_hid, new_hid)
    else:
        new_filename = f"{filename}"
        
    output_csv_path = os.path.join(output_dir, new_filename)
    
    # Save CSV
    synth_df.to_csv(output_csv_path, index=False)
    print(f"Saved synthetic CSV to: {output_csv_path}")

    # Handle Metadata JSON if it exists
    json_path = input_csv_path.replace('.csv', '_metadata.json')
    if os.path.exists(json_path):
        with open(json_path, 'r') as f:
            meta = json.load(f)
        
        meta['headsetId'] = new_hid
        # Update total metrics count if we changed length (we didn't, but good practice)
        meta['totalMetrics'] = len(synth_df)
        
        output_json_path = output_csv_path.replace('.csv', '_metadata.json')
        with open(output_json_path, 'w') as f:
            json.dump(meta, f, indent=4)
        print(f"Saved synthetic Metadata to: {output_json_path}")
    else:
        print("No metadata JSON found, skipping.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate synthetic client data from existing session CSV.")
    parser.add_argument("input_csv", help="Path to the source client CSV file")
    parser.add_argument("--output_dir", help="Directory to save the synthetic data (default: same as input)", default=None)
    
    args = parser.parse_args()
    
    generate_synthetic_data(args.input_csv, args.output_dir)
