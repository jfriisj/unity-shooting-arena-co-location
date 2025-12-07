// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MRMotifs.SharedActivities.ShootingSample.Editor
{
    /// <summary>
    /// Editor utility to configure Easy FPS assets in the shooting scene.
    /// Run from Unity menu: Tools/MR Motifs/Configure Easy FPS Assets
    /// </summary>
    public static class EasyFPSConfigurator
    {
        private const string MUZZLE_FLASH_PATH = "Assets/Easy FPS/MuzzelFlash/MuzzlePrefabs/";
        private const string BULLET_HOLE_PATH = "Assets/Easy FPS/Prefabs/bulletHole.prefab";
        private const string BULLET_PREFAB_PATH = "Assets/Prefabs/Shooting/BulletMotif.prefab";

        [MenuItem("Tools/MR Motifs/Configure Easy FPS Assets")]
        public static void ConfigureEasyFPSAssets()
        {
            ConfigureShootingSetup();
            ConfigureBulletPrefab();
            
            Debug.Log("[EasyFPSConfigurator] Configuration complete! Don't forget to save the scene.");
        }

        [MenuItem("Tools/MR Motifs/Configure Easy FPS Assets (Shooting Setup Only)")]
        public static void ConfigureShootingSetupOnly()
        {
            ConfigureShootingSetup();
        }

        [MenuItem("Tools/MR Motifs/Configure Easy FPS Assets (Bullet Prefab Only)")]
        public static void ConfigureBulletPrefabOnly()
        {
            ConfigureBulletPrefab();
        }

        private static void ConfigureShootingSetup()
        {
            // Find ShootingSetupMotif in scene
            var shootingSetup = Object.FindAnyObjectByType<ShootingSetupMotif>();
            if (shootingSetup == null)
            {
                Debug.LogWarning("[EasyFPSConfigurator] ShootingSetupMotif not found in scene!");
                return;
            }

            // Get SerializedObject
            var so = new SerializedObject(shootingSetup);

            // Load muzzle flash prefabs
            var muzzleFlashNames = new string[] {
                "muzzelFlash 01.prefab",
                "muzzelFlash 02.prefab",
                "muzzelFlash 03.prefab",
                "muzzelFlash 04.prefab",
                "muzzelFlash 05.prefab"
            };

            var muzzleFlashPrefabs = new List<GameObject>();
            foreach (var name in muzzleFlashNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MUZZLE_FLASH_PATH + name);
                if (prefab != null)
                {
                    muzzleFlashPrefabs.Add(prefab);
                }
                else
                {
                    Debug.LogWarning($"[EasyFPSConfigurator] Could not load muzzle flash: {name}");
                }
            }

            // Set muzzle flash prefabs array
            var muzzleFlashProp = so.FindProperty("m_muzzleFlashPrefabs");
            if (muzzleFlashProp != null)
            {
                muzzleFlashProp.arraySize = muzzleFlashPrefabs.Count;
                for (int i = 0; i < muzzleFlashPrefabs.Count; i++)
                {
                    muzzleFlashProp.GetArrayElementAtIndex(i).objectReferenceValue = muzzleFlashPrefabs[i];
                }
                Debug.Log($"[EasyFPSConfigurator] Assigned {muzzleFlashPrefabs.Count} muzzle flash prefabs to ShootingSetupMotif");
            }

            // Set bullet hole prefab
            var bulletHolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BULLET_HOLE_PATH);
            var bulletHoleProp = so.FindProperty("m_bulletHolePrefab");
            if (bulletHoleProp != null && bulletHolePrefab != null)
            {
                bulletHoleProp.objectReferenceValue = bulletHolePrefab;
                Debug.Log("[EasyFPSConfigurator] Assigned bullet hole prefab to ShootingSetupMotif");
            }

            so.ApplyModifiedProperties();

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shootingSetup.gameObject.scene);
        }

        private static void ConfigureBulletPrefab()
        {
            // Load and modify the BulletMotif prefab
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BULLET_PREFAB_PATH);
            if (bulletPrefab == null)
            {
                Debug.LogWarning($"[EasyFPSConfigurator] Could not load bullet prefab at: {BULLET_PREFAB_PATH}");
                return;
            }

            var bulletMotif = bulletPrefab.GetComponent<BulletMotif>();
            if (bulletMotif == null)
            {
                Debug.LogWarning("[EasyFPSConfigurator] BulletMotif component not found on bullet prefab!");
                return;
            }

            // Get SerializedObject for the prefab
            var so = new SerializedObject(bulletMotif);

            // Set bullet hole prefab
            var bulletHolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BULLET_HOLE_PATH);
            var bulletHoleProp = so.FindProperty("m_bulletHolePrefab");
            if (bulletHoleProp != null && bulletHolePrefab != null)
            {
                bulletHoleProp.objectReferenceValue = bulletHolePrefab;
                Debug.Log("[EasyFPSConfigurator] Assigned bullet hole prefab to BulletMotif prefab");
            }

            so.ApplyModifiedProperties();
            
            // Save the prefab
            EditorUtility.SetDirty(bulletPrefab);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
