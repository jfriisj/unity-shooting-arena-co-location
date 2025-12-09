using Unity.Netcode;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using MRMotifs.Shared;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Manages spawning of game elements like weapons and powerups.
    /// Uses MRUK to place items on physical surfaces (Tables, Desks). 
    /// </summary>
    public class SpawnManagerMotif : NetworkBehaviour
    {
        [Header("Weapon Spawning")]
        [SerializeField] private NetworkObject m_weaponPrefab;
        [SerializeField] private int m_weaponsPerTable = 1;
        [SerializeField] private float m_spawnHeightOffset = 0.1f;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (MRUK.Instance && MRUK.Instance.GetCurrentRoom() != null)
                {
                    SpawnWeapons();
                }
                else if (MRUK.Instance)
                {
                    MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            // MRUK doesn't support UnregisterSceneLoadedCallback
        }

        private void OnSceneLoaded()
        {
            SpawnWeapons();
        }

        private void SpawnWeapons()
        {
            if (m_weaponPrefab == null)
            {
                DebugLogger.Error("SPAWN", "Weapon Prefab not assigned!");
                return;
            }

            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null)
            {
                DebugLogger.Warning("SPAWN", "No MRUK Room found. Spawning fallback weapon.");
                SpawnFallbackWeapon();
                return;
            }

            var tables = room.GetRoomAnchors().Where(a => 
                a.Label == MRUKAnchor.SceneLabels.TABLE || 
                a.Label == MRUKAnchor.SceneLabels.COUCH).ToList();

            DebugLogger.Shooting($"Found {tables.Count} suitable surfaces for weapons.");

            if (tables.Count == 0)
            {
                // Fallback: Spawn in center of room, slightly elevated
                DebugLogger.Shooting("No tables found. Spawning weapon in room center.");
                Vector3 center = room.transform.position;
                SpawnWeapon(center + Vector3.up * 1.0f); // Floating at 1m
                return;
            }

            foreach (var table in tables)
            {
                // Determine spawn position on top of the surface
                Vector3 spawnPos = table.transform.position;

                // If it has volume bounds (3D box), use the top center
                if (table.VolumeBounds.HasValue)
                {
                    Bounds bounds = table.VolumeBounds.Value;
                    // Transform bounds center to world if needed? 
                    // MRUK VolumeBounds are usually in local space? No, let's check docs or assume world if accessed via property?
                    // Actually MRUKAnchor.VolumeBounds is usually local AABB.
                    // But let's look at the anchor transform.
                    
                    // Safer way: Use the anchor's transform position (usually center) and add half height.
                    // But we need the height.
                    
                    // Let's try to use the PlaneRect if it's a 2D surface (like a table top often is represented as a plane).
                    if (table.PlaneRect.HasValue)
                    {
                        // It's a plane. Position is center.
                        spawnPos = table.transform.position;
                    }
                    else
                    {
                        // It's a volume.
                        // We can try to get the size from the collider or the bounds.
                        // Let's just spawn at pivot + a bit up, assuming pivot is center.
                        // Ideally we'd raycast down to find the surface, or up?
                    }
                }
                
                // Add offset
                spawnPos += Vector3.up * m_spawnHeightOffset;

                SpawnWeapon(spawnPos);
            }
        }

        private void SpawnFallbackWeapon()
        {
             Vector3 spawnPos = new Vector3(0, 1f, 0.5f);
             if (Camera.main != null)
             {
                 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
             }
             SpawnWeapon(spawnPos);
        }

        private void SpawnWeapon(Vector3 position)
        {
            var instance = Instantiate(m_weaponPrefab, position, Quaternion.identity);
            instance.Spawn();
            DebugLogger.Shooting($"Spawned weapon at {position}");
        }
    }
}
