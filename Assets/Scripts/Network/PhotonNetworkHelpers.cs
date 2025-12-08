// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System.Linq;
using Fusion;
using UnityEngine;
using MRMotifs.Shared;

namespace MRMotifs.SharedActivities
{
    /// <summary>
    /// Utility extensions and helpers for Photon Fusion networking.
    /// Based on Discover sample patterns for cleaner network code.
    /// </summary>
    public static class PhotonNetworkHelpers
    {
        private static NetworkRunner s_cachedRunner;

        /// <summary>
        /// Gets the active NetworkRunner instance.
        /// </summary>
        public static NetworkRunner Runner
        {
            get
            {
                if (s_cachedRunner != null && s_cachedRunner.IsRunning)
                    return s_cachedRunner;
                    
                s_cachedRunner = NetworkRunner.Instances.FirstOrDefault();
                return s_cachedRunner;
            }
        }

        /// <summary>
        /// Checks if this runner is the master client (shared mode) or single player.
        /// </summary>
        public static bool IsMasterClient(this NetworkRunner runner)
        {
            if (runner == null) return false;
            return runner.IsSharedModeMasterClient || runner.GameMode == GameMode.Single;
        }

        /// <summary>
        /// Gets the OVRCameraRig in the scene.
        /// </summary>
        public static OVRCameraRig CameraRig
        {
            get
            {
                var cameraRig = Object.FindAnyObjectByType<OVRCameraRig>();
                if (cameraRig == null)
                {
                    DebugLogger.Warning("NETWORK", "OVRCameraRig not found in scene");
                }
                return cameraRig;
            }
        }

        /// <summary>
        /// Despawns a networked object.
        /// </summary>
        public static void Despawn(this NetworkObject obj)
        {
            if (Runner != null && obj != null)
            {
                Runner.Despawn(obj);
            }
        }

        /// <summary>
        /// Checks if the local player has state authority for this object.
        /// </summary>
        public static bool HasLocalAuthority(this NetworkObject obj)
        {
            return obj != null && obj.HasStateAuthority;
        }

        /// <summary>
        /// Gets the local player's PlayerRef.
        /// </summary>
        public static PlayerRef LocalPlayer
        {
            get
            {
                if (Runner != null && Runner.IsRunning)
                    return Runner.LocalPlayer;
                return PlayerRef.None;
            }
        }

        /// <summary>
        /// Checks if a PlayerRef represents the local player.
        /// </summary>
        public static bool IsLocal(this PlayerRef player)
        {
            return player == LocalPlayer;
        }

        /// <summary>
        /// Clears the cached runner (call when runner shuts down).
        /// </summary>
        public static void ClearCache()
        {
            s_cachedRunner = null;
        }
    }
}
#endif
