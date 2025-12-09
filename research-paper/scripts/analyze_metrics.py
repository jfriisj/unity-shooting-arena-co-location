#!/usr/bin/env python3
"""
analyze_metrics.py - Analyze collected metrics from co-located VR sessions.

This script processes CSV metrics files collected by MetricsLogger and generates
summary statistics and visualizations for research analysis.

Usage:
    python analyze_metrics.py [session_dir]
    
Example:
    python analyze_metrics.py research-paper/data/sessions/20251206
"""

import os
import sys
import json
import glob
from datetime import datetime
from pathlib import Path

import pandas as pd
import numpy as np

# Optional: matplotlib for visualization
try:
    import matplotlib.pyplot as plt
    HAS_MATPLOTLIB = True
except ImportError:
    HAS_MATPLOTLIB = False
    print("Note: matplotlib not installed. Visualizations will be skipped.")


# Performance thresholds
THRESHOLDS = {
    'fps_target': 90,
    'fps_minimum': 72,
    'latency_target_ms': 75,
    'latency_good_ms': 50,
    'calibration_target_mm': 10,
    'calibration_warning_mm': 25,
    'packet_loss_target_pct': 1.0,
}


def load_session_data(session_dir: str) -> pd.DataFrame:
    """Load and merge all metrics CSVs from a session directory."""
    all_data = []
    
    # Find all CSV files in headset subdirectories
    for headset_dir in glob.glob(os.path.join(session_dir, 'H*')):
        csv_files = glob.glob(os.path.join(headset_dir, '*.csv'))
        csv_files.extend(glob.glob(os.path.join(headset_dir, 'metrics', '*.csv')))
        
        for csv_file in csv_files:
            try:
                df = pd.read_csv(csv_file)
                df['source_file'] = os.path.basename(csv_file)
                all_data.append(df)
                print(f"  Loaded: {csv_file} ({len(df)} rows)")
            except Exception as e:
                print(f"  Error loading {csv_file}: {e}")
    
    if not all_data:
        print("No data files found.")
        return pd.DataFrame()
    
    # Merge all data
    merged_df = pd.concat(all_data, ignore_index=True)
    merged_df = merged_df.sort_values(['headset_id', 'timestamp_sec'])
    
    return merged_df


def calculate_statistics(df: pd.DataFrame) -> dict:
    """Calculate summary statistics for the session."""
    if df.empty:
        return {}
    
    stats = {
        # Session info
        'total_samples': len(df),
        'headset_count': df['headset_id'].nunique(),
        'headsets': df['headset_id'].unique().tolist(),
        'duration_seconds': df['timestamp_sec'].max(),
        'duration_minutes': df['timestamp_sec'].max() / 60,
        
        # Frame rate
        'fps_mean': df['frame_rate_fps'].mean(),
        'fps_std': df['frame_rate_fps'].std(),
        'fps_min': df['frame_rate_fps'].min(),
        'fps_max': df['frame_rate_fps'].max(),
        'fps_p5': df['frame_rate_fps'].quantile(0.05),
        'fps_p95': df['frame_rate_fps'].quantile(0.95),
        'fps_target_achieved_pct': (df['frame_rate_fps'] >= THRESHOLDS['fps_target']).mean() * 100,
        'fps_minimum_achieved_pct': (df['frame_rate_fps'] >= THRESHOLDS['fps_minimum']).mean() * 100,
        
        # Frame time
        'frame_time_mean_ms': df['frame_time_ms'].mean(),
        'frame_time_max_ms': df['frame_time_ms'].max(),
        
        # Network latency
        'latency_mean_ms': df['network_latency_ms'].mean(),
        'latency_std_ms': df['network_latency_ms'].std(),
        'latency_min_ms': df['network_latency_ms'].min(),
        'latency_max_ms': df['network_latency_ms'].max(),
        'latency_p50_ms': df['network_latency_ms'].quantile(0.50),
        'latency_p95_ms': df['network_latency_ms'].quantile(0.95),
        'latency_p99_ms': df['network_latency_ms'].quantile(0.99),
        'latency_target_achieved_pct': (df['network_latency_ms'] <= THRESHOLDS['latency_target_ms']).mean() * 100,
        
        # Packet loss
        'packet_loss_mean_pct': df['packet_loss_pct'].mean(),
        'packet_loss_max_pct': df['packet_loss_pct'].max(),
        'packet_loss_target_achieved_pct': (df['packet_loss_pct'] <= THRESHOLDS['packet_loss_target_pct']).mean() * 100,
        
        # Calibration
        'calibration_mean_mm': df['calibration_error_mm'].mean(),
        'calibration_std_mm': df['calibration_error_mm'].std(),
        'calibration_max_mm': df['calibration_error_mm'].max(),
        'calibration_target_achieved_pct': (df['calibration_error_mm'] <= THRESHOLDS['calibration_target_mm']).mean() * 100,
        
        # Battery Temperature
        'battery_temp_mean_c': df['battery_temp_c'].mean() if 'battery_temp_c' in df.columns else df.get('headset_temp_c', pd.Series([0])).mean(),
        'battery_temp_max_c': df['battery_temp_c'].max() if 'battery_temp_c' in df.columns else df.get('headset_temp_c', pd.Series([0])).max(),
        
        # CPU Usage
        'cpu_usage_mean_pct': df['cpu_usage_pct'].mean() if 'cpu_usage_pct' in df.columns else 0,
        'cpu_usage_max_pct': df['cpu_usage_pct'].max() if 'cpu_usage_pct' in df.columns else 0,
        
        # Memory Usage
        'memory_used_mean_mb': df['memory_used_mb'].mean() if 'memory_used_mb' in df.columns else 0,
        'memory_used_max_mb': df['memory_used_mb'].max() if 'memory_used_mb' in df.columns else 0,
        
        # Battery
        'battery_start_pct': df.groupby('headset_id')['battery_level'].first().mean(),
        'battery_end_pct': df.groupby('headset_id')['battery_level'].last().mean(),
        'battery_drain_pct': df.groupby('headset_id')['battery_level'].first().mean() - 
                            df.groupby('headset_id')['battery_level'].last().mean(),
    }
    
    return stats


def calculate_per_headset_statistics(df: pd.DataFrame) -> pd.DataFrame:
    """Calculate statistics per headset."""
    if df.empty:
        return pd.DataFrame()
    
    stats_list = []
    
    for headset_id in df['headset_id'].unique():
        hdf = df[df['headset_id'] == headset_id]
        
        stats = {
            'headset_id': headset_id,
            'samples': len(hdf),
            'duration_min': hdf['timestamp_sec'].max() / 60,
            'scene_state': hdf['scene_state'].mode().iloc[0] if len(hdf) > 0 else 'Unknown',
            'fps_mean': hdf['frame_rate_fps'].mean(),
            'fps_min': hdf['frame_rate_fps'].min(),
            'latency_mean_ms': hdf['network_latency_ms'].mean(),
            'latency_max_ms': hdf['network_latency_ms'].max(),
            'calibration_mean_mm': hdf['calibration_error_mm'].mean(),
            'packet_loss_mean_pct': hdf['packet_loss_pct'].mean(),
            'battery_drain_pct': hdf['battery_level'].iloc[0] - hdf['battery_level'].iloc[-1],
        }
        stats_list.append(stats)
    
    return pd.DataFrame(stats_list)


def print_report(stats: dict, per_headset_stats: pd.DataFrame):
    """Print a formatted report of statistics."""
    print("\n" + "=" * 60)
    print("SESSION METRICS REPORT")
    print("=" * 60)
    
    print(f"\n{'Session Overview':=^60}")
    print(f"  Total Samples: {stats.get('total_samples', 0):,}")
    print(f"  Duration: {stats.get('duration_minutes', 0):.1f} minutes")
    print(f"  Headsets: {stats.get('headset_count', 0)}")
    
    print(f"\n{'Frame Rate (FPS)':=^60}")
    print(f"  Mean: {stats.get('fps_mean', 0):.1f} FPS")
    print(f"  Std Dev: {stats.get('fps_std', 0):.1f}")
    print(f"  Range: {stats.get('fps_min', 0):.1f} - {stats.get('fps_max', 0):.1f}")
    print(f"  5th-95th Percentile: {stats.get('fps_p5', 0):.1f} - {stats.get('fps_p95', 0):.1f}")
    print(f"  Target (≥{THRESHOLDS['fps_target']} FPS) Achieved: {stats.get('fps_target_achieved_pct', 0):.1f}%")
    
    print(f"\n{'Network Latency':=^60}")
    print(f"  Mean: {stats.get('latency_mean_ms', 0):.1f} ms")
    print(f"  Std Dev: {stats.get('latency_std_ms', 0):.1f} ms")
    print(f"  Range: {stats.get('latency_min_ms', 0):.1f} - {stats.get('latency_max_ms', 0):.1f} ms")
    print(f"  50th/95th/99th Percentile: {stats.get('latency_p50_ms', 0):.1f} / {stats.get('latency_p95_ms', 0):.1f} / {stats.get('latency_p99_ms', 0):.1f} ms")
    print(f"  Target (≤{THRESHOLDS['latency_target_ms']} ms) Achieved: {stats.get('latency_target_achieved_pct', 0):.1f}%")
    
    print(f"\n{'Packet Loss':=^60}")
    print(f"  Mean: {stats.get('packet_loss_mean_pct', 0):.3f}%")
    print(f"  Max: {stats.get('packet_loss_max_pct', 0):.3f}%")
    print(f"  Target (≤{THRESHOLDS['packet_loss_target_pct']}%) Achieved: {stats.get('packet_loss_target_achieved_pct', 0):.1f}%")
    
    print(f"\n{'Calibration Error':=^60}")
    print(f"  Mean: {stats.get('calibration_mean_mm', 0):.2f} mm")
    print(f"  Std Dev: {stats.get('calibration_std_mm', 0):.2f} mm")
    print(f"  Max: {stats.get('calibration_max_mm', 0):.2f} mm")
    print(f"  Target (≤{THRESHOLDS['calibration_target_mm']} mm) Achieved: {stats.get('calibration_target_achieved_pct', 0):.1f}%")
    
    print(f"\n{'Device Health':=^60}")
    print(f"  Battery Temperature: {stats.get('battery_temp_mean_c', 0):.1f}°C (max: {stats.get('battery_temp_max_c', 0):.1f}°C)")
    print(f"  CPU Usage: {stats.get('cpu_usage_mean_pct', 0):.1f}% (max: {stats.get('cpu_usage_max_pct', 0):.1f}%)")
    print(f"  Memory Used: {stats.get('memory_used_mean_mb', 0):.0f} MB (max: {stats.get('memory_used_max_mb', 0):.0f} MB)")
    print(f"  Battery Drain: {stats.get('battery_drain_pct', 0):.1f}%")
    
    if not per_headset_stats.empty:
        print(f"\n{'Per-Headset Summary':=^60}")
        print(per_headset_stats.to_string(index=False))
    
    print("\n" + "=" * 60)


def create_visualizations(df: pd.DataFrame, output_dir: str):
    """Create visualization charts from the data."""
    if not HAS_MATPLOTLIB or df.empty:
        return
    
    fig, axes = plt.subplots(2, 2, figsize=(14, 10))
    fig.suptitle('Session Metrics Overview', fontsize=14)
    
    # FPS over time
    ax1 = axes[0, 0]
    for headset_id in df['headset_id'].unique():
        hdf = df[df['headset_id'] == headset_id]
        ax1.plot(hdf['timestamp_sec'] / 60, hdf['frame_rate_fps'], label=headset_id, alpha=0.7)
    ax1.axhline(y=THRESHOLDS['fps_target'], color='g', linestyle='--', label=f"Target ({THRESHOLDS['fps_target']} FPS)")
    ax1.axhline(y=THRESHOLDS['fps_minimum'], color='r', linestyle='--', label=f"Minimum ({THRESHOLDS['fps_minimum']} FPS)")
    ax1.set_xlabel('Time (minutes)')
    ax1.set_ylabel('Frame Rate (FPS)')
    ax1.set_title('Frame Rate Over Time')
    ax1.legend(loc='lower left')
    ax1.grid(True, alpha=0.3)
    
    # Latency over time
    ax2 = axes[0, 1]
    for headset_id in df['headset_id'].unique():
        hdf = df[df['headset_id'] == headset_id]
        ax2.plot(hdf['timestamp_sec'] / 60, hdf['network_latency_ms'], label=headset_id, alpha=0.7)
    ax2.axhline(y=THRESHOLDS['latency_target_ms'], color='r', linestyle='--', label=f"Target ({THRESHOLDS['latency_target_ms']} ms)")
    ax2.set_xlabel('Time (minutes)')
    ax2.set_ylabel('Network Latency (ms)')
    ax2.set_title('Network Latency Over Time')
    ax2.legend(loc='upper right')
    ax2.grid(True, alpha=0.3)
    
    # FPS distribution
    ax3 = axes[1, 0]
    ax3.hist(df['frame_rate_fps'], bins=50, edgecolor='black', alpha=0.7)
    ax3.axvline(x=THRESHOLDS['fps_target'], color='g', linestyle='--', label=f"Target ({THRESHOLDS['fps_target']} FPS)")
    ax3.set_xlabel('Frame Rate (FPS)')
    ax3.set_ylabel('Frequency')
    ax3.set_title('Frame Rate Distribution')
    ax3.legend()
    ax3.grid(True, alpha=0.3)
    
    # Latency distribution
    ax4 = axes[1, 1]
    ax4.hist(df['network_latency_ms'], bins=50, edgecolor='black', alpha=0.7)
    ax4.axvline(x=THRESHOLDS['latency_target_ms'], color='r', linestyle='--', label=f"Target ({THRESHOLDS['latency_target_ms']} ms)")
    ax4.set_xlabel('Network Latency (ms)')
    ax4.set_ylabel('Frequency')
    ax4.set_title('Network Latency Distribution')
    ax4.legend()
    ax4.grid(True, alpha=0.3)
    
    plt.tight_layout()
    
    output_path = os.path.join(output_dir, 'metrics_overview.png')
    plt.savefig(output_path, dpi=150)
    print(f"\nVisualization saved to: {output_path}")
    plt.close()


def save_report(stats: dict, per_headset_stats: pd.DataFrame, output_dir: str):
    """Save statistics to JSON and CSV files."""
    # Save overall stats as JSON
    stats_path = os.path.join(output_dir, 'summary_statistics.json')
    
    # Convert numpy types for JSON serialization
    json_stats = {}
    for k, v in stats.items():
        if isinstance(v, (np.integer, np.floating)):
            json_stats[k] = float(v)
        elif isinstance(v, np.ndarray):
            json_stats[k] = v.tolist()
        else:
            json_stats[k] = v
    
    with open(stats_path, 'w') as f:
        json.dump(json_stats, f, indent=2)
    print(f"Statistics saved to: {stats_path}")
    
    # Save per-headset stats as CSV
    if not per_headset_stats.empty:
        per_headset_path = os.path.join(output_dir, 'per_headset_statistics.csv')
        per_headset_stats.to_csv(per_headset_path, index=False)
        print(f"Per-headset statistics saved to: {per_headset_path}")


def main():
    if len(sys.argv) < 2:
        # Find the most recent session
        sessions_dir = 'research-paper/data/sessions'
        if os.path.exists(sessions_dir):
            sessions = sorted(glob.glob(os.path.join(sessions_dir, '*')))
            if sessions:
                session_dir = sessions[-1]
                print(f"Using most recent session: {session_dir}")
            else:
                print(f"No sessions found in {sessions_dir}")
                print("Usage: python analyze_metrics.py <session_directory>")
                sys.exit(1)
        else:
            print("Usage: python analyze_metrics.py <session_directory>")
            sys.exit(1)
    else:
        session_dir = sys.argv[1]
    
    if not os.path.exists(session_dir):
        print(f"Error: Directory not found: {session_dir}")
        sys.exit(1)
    
    print(f"Analyzing session: {session_dir}")
    print("-" * 40)
    
    # Load data
    df = load_session_data(session_dir)
    
    if df.empty:
        print("No data to analyze.")
        sys.exit(1)
    
    # Save merged data
    merged_path = os.path.join(session_dir, 'merged_metrics.csv')
    df.to_csv(merged_path, index=False)
    print(f"\nMerged data saved to: {merged_path}")
    
    # Calculate statistics
    stats = calculate_statistics(df)
    per_headset_stats = calculate_per_headset_statistics(df)
    
    # Print report
    print_report(stats, per_headset_stats)
    
    # Save report files
    save_report(stats, per_headset_stats, session_dir)
    
    # Create visualizations
    create_visualizations(df, session_dir)


if __name__ == '__main__':
    main()
