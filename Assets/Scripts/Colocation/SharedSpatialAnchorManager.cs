// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using System;
using UnityEngine;
using Meta.XR.Samples;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    public enum AnchorPlacementMode
    {
        AtOrigin,           // Create anchor at Vector3.zero (original behavior)
        AtHostPosition,     // Create anchor at host's current headset position
        ManualPlacement     // Wait for host to trigger placement with controller
    }

    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    public class SharedSpatialAnchorManager : NetworkBehaviour
    {
        [Header("Anchor Placement Settings")]
        [SerializeField]
        [Tooltip("How the host should place the colocation anchor:\n" +
                 "- AtOrigin: Anchor at world origin (Vector3.zero)\n" +
                 "- AtHostPosition: Anchor at host's current headset position\n" +
                 "- ManualPlacement: Host presses trigger to place anchor")]
        private AnchorPlacementMode m_anchorPlacementMode = AnchorPlacementMode.AtHostPosition;

        [SerializeField]
        [Tooltip("When using ManualPlacement, which controller button triggers anchor creation")]
        private OVRInput.Button m_placementButton = OVRInput.Button.PrimaryIndexTrigger;

        [SerializeField]
        [Tooltip("Visual indicator shown during manual placement mode")]
        private GameObject m_placementIndicatorPrefab;

        private ColocationManager m_colocationManager;
        private Guid m_sharedAnchorGroupId;
        private DateTime m_discoveryStartTime;
        private DateTime m_localizationStartTime;
        private OVRCameraRig m_cameraRig;
        private bool m_waitingForPlacement = false;
        private GameObject m_placementIndicator;

        public override void Spawned()
        {
            base.Spawned();
            m_colocationManager = FindAnyObjectByType<ColocationManager>();
            m_cameraRig = FindAnyObjectByType<OVRCameraRig>();
            PrepareColocation();
        }

        private void Update()
        {
            if (!m_waitingForPlacement) return;

            // Update placement indicator position at headset ground position
            if (m_placementIndicator != null && m_cameraRig != null)
            {
                var headPosition = m_cameraRig.centerEyeAnchor.position;
                m_placementIndicator.transform.position = new Vector3(headPosition.x, 0f, headPosition.z);
            }

            // Check for trigger press to confirm placement
            if (OVRInput.GetDown(m_placementButton))
            {
                m_waitingForPlacement = false;
                if (m_placementIndicator != null)
                {
                    Destroy(m_placementIndicator);
                }
                Debug.Log("Motif: Manual anchor placement confirmed by host.");
                StartAnchorCreation();
            }
        }

        private void PrepareColocation()
        {
            // Use IsSharedModeMasterClient for Shared Mode to determine who hosts colocation
            // This is more reliable than HasStateAuthority in Shared Mode
            bool isMasterClient = Runner != null && Runner.IsSharedModeMasterClient;
            
            Debug.Log($"Motif: PrepareColocation - IsMasterClient: {isMasterClient}, HasStateAuthority: {Object.HasStateAuthority}, PlayerCount: {Runner?.SessionInfo?.PlayerCount ?? 0}");
            
            if (isMasterClient)
            {
                Debug.Log("Motif: Starting advertisement (Master Client)...");
                AdvertiseColocationSession();
            }
            else
            {
                Debug.Log("Motif: Starting discovery (Client)...");
                DiscoverNearbySession();
            }
        }

        private async void AdvertiseColocationSession()
        {
            // Optional advertisement data, e.g. the name of the session or a message to other clients (max. size: 1024 bytes).
            // We can also leave it empty with "null".
            // var colocationSessionData = Encoding.UTF8.GetBytes("SharedSpatialAnchorSession");
            var startAdvertisementResult = await OVRColocationSession.StartAdvertisementAsync(null);

            if (startAdvertisementResult.Success)
            {
                m_sharedAnchorGroupId = startAdvertisementResult.Value;
                Debug.Log($"Motif: Advertisement started successfully. UUID: {m_sharedAnchorGroupId}");

                // Handle placement based on mode
                switch (m_anchorPlacementMode)
                {
                    case AnchorPlacementMode.ManualPlacement:
                        StartManualPlacementMode();
                        break;
                    default:
                        StartAnchorCreation();
                        break;
                }
            }
            else
            {
                Debug.LogError($"Motif: Advertisement failed with status: {startAdvertisementResult.Status}");
            }
        }

        private void StartManualPlacementMode()
        {
            Debug.Log("Motif: Waiting for host to place anchor. Press trigger to confirm anchor location.");
            m_waitingForPlacement = true;

            // Create visual indicator if prefab is assigned
            if (m_placementIndicatorPrefab != null)
            {
                m_placementIndicator = Instantiate(m_placementIndicatorPrefab);
            }
            else
            {
                // Create default indicator (simple sphere)
                m_placementIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_placementIndicator.name = "Anchor Placement Indicator";
                m_placementIndicator.transform.localScale = Vector3.one * 0.2f;

                // Remove collider so it doesn't interfere
                var collider = m_placementIndicator.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                // Make it semi-transparent green
                var renderer = m_placementIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0f, 1f, 0f, 0.5f);
                }
            }
        }

        private void StartAnchorCreation()
        {
            CreateAndShareAlignmentAnchor();
        }

        private async void DiscoverNearbySession()
        {
            // Wait a moment to ensure the master client has started advertising
            await System.Threading.Tasks.Task.Delay(2000);
            
            m_discoveryStartTime = DateTime.Now;
            OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;

            Debug.Log("Motif: Starting colocation discovery...");
            var discoveryResult = await OVRColocationSession.StartDiscoveryAsync();
            if (!discoveryResult.Success)
            {
                Debug.LogError($"Motif: Discovery failed with status: {discoveryResult.Status}");
                return;
            }

            Debug.Log("Motif: Discovery started successfully. Waiting for nearby session...");
            
            // Start a timeout coroutine to retry if discovery takes too long
            StartCoroutine(DiscoveryTimeoutCheck());
        }
        
        private IEnumerator DiscoveryTimeoutCheck()
        {
            yield return new WaitForSeconds(15f);
            
            // If we still haven't discovered after 15 seconds, log a warning
            TimeSpan elapsed = DateTime.Now - m_discoveryStartTime;
            Debug.LogWarning($"Motif: Discovery timeout check - elapsed {elapsed.TotalSeconds:F1}s. " +
                "Make sure both headsets are in the same physical space and Bluetooth is enabled.");
        }

        private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
        {
            OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;

            m_sharedAnchorGroupId = session.AdvertisementUuid;
            
            // Log discovery duration for metrics
            TimeSpan discoveryDuration = DateTime.Now - m_discoveryStartTime;
            Debug.Log($"Motif: Discovered session with UUID: {m_sharedAnchorGroupId}. Discovery time: {discoveryDuration.TotalSeconds:F2}s");
            
            LoadAndAlignToAnchor(m_sharedAnchorGroupId);
        }

        private async void CreateAndShareAlignmentAnchor()
        {
            Debug.Log("Motif: Creating alignment anchor...");

            // Determine anchor position based on placement mode
            Vector3 anchorPosition;
            Quaternion anchorRotation;

            switch (m_anchorPlacementMode)
            {
                case AnchorPlacementMode.AtHostPosition:
                case AnchorPlacementMode.ManualPlacement:
                    // Use host's current headset position projected to ground
                    if (m_cameraRig != null)
                    {
                        var headPosition = m_cameraRig.centerEyeAnchor.position;
                        anchorPosition = new Vector3(headPosition.x, 0f, headPosition.z);
                        // Use host's current forward direction (yaw only)
                        var headForward = m_cameraRig.centerEyeAnchor.forward;
                        headForward.y = 0;
                        anchorRotation = Quaternion.LookRotation(headForward.normalized, Vector3.up);
                        Debug.Log($"Motif: Creating anchor at host position: {anchorPosition}");
                    }
                    else
                    {
                        Debug.LogWarning("Motif: CameraRig not found, falling back to origin.");
                        anchorPosition = Vector3.zero;
                        anchorRotation = Quaternion.identity;
                    }
                    break;

                case AnchorPlacementMode.AtOrigin:
                default:
                    anchorPosition = Vector3.zero;
                    anchorRotation = Quaternion.identity;
                    break;
            }

            var anchor = await CreateAnchor(anchorPosition, anchorRotation);

            if (anchor == null)
            {
                Debug.LogError("Motif: Failed to create alignment anchor.");
                return;
            }

            if (!anchor.Localized)
            {
                Debug.LogError("Motif: Anchor is not localized. Cannot proceed with sharing.");
                return;
            }
            
            // Register host calibration for drift tracking
            if (m_colocationManager != null)
            {
                m_colocationManager.RegisterHostCalibration(anchorPosition);
            }

            var saveResult = await anchor.SaveAnchorAsync();
            if (!saveResult.Success)
            {
                Debug.LogError($"Motif: Failed to save alignment anchor. Error: {saveResult}");
                return;
            }

            Debug.Log($"Motif: Alignment anchor saved successfully. UUID: {anchor.Uuid}");
            Debug.Log("Motif: Attempting to share alignment anchor...");
            var shareResult = await OVRSpatialAnchor.ShareAsync(new List<OVRSpatialAnchor> { anchor }, m_sharedAnchorGroupId);

            if (!shareResult.Success)
            {
                Debug.LogError($"Motif: Failed to share alignment anchor. Error: {shareResult}");
                return;
            }

            Debug.Log($"Motif: Alignment anchor shared successfully. Group UUID: {m_sharedAnchorGroupId}");
        }

        private async Task<OVRSpatialAnchor> CreateAnchor(Vector3 position, Quaternion rotation)
        {
            var anchorGameObject = new GameObject("Motif: Alignment Anchor")
            {
                transform = { position = position, rotation = rotation }
            };

            var spatialAnchor = anchorGameObject.AddComponent<OVRSpatialAnchor>();
            while (!spatialAnchor.Created)
            {
                await Task.Yield();
            }

            Debug.Log($"Motif: Anchor created successfully. UUID: {spatialAnchor.Uuid}");
            return spatialAnchor;
        }

        private async void LoadAndAlignToAnchor(Guid groupUuid)
        {
            Debug.Log($"Motif: Loading anchors for Group UUID: {groupUuid}...");
            m_localizationStartTime = DateTime.Now;
            
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var loadResult = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupUuid, unboundAnchors);

            if (!loadResult.Success || unboundAnchors.Count == 0)
            {
                Debug.LogError($"Motif: Failed to load anchors. Success: {loadResult.Success}, Count: {unboundAnchors.Count}");
                return;
            }

            foreach (var unboundAnchor in unboundAnchors)
            {
                DateTime localizationAttemptStart = DateTime.Now;
                
                if (await unboundAnchor.LocalizeAsync())
                {
                    TimeSpan localizationDuration = DateTime.Now - localizationAttemptStart;
                    TimeSpan totalLoadTime = DateTime.Now - m_localizationStartTime;
                    
                    Debug.Log($"Motif: Anchor localized successfully. UUID: {unboundAnchor.Uuid}");
                    Debug.Log($"Motif: Localization time: {localizationDuration.TotalSeconds:F2}s, Total load time: {totalLoadTime.TotalSeconds:F2}s");

                    var anchorGameObject = new GameObject($"Anchor_{unboundAnchor.Uuid}");
                    var spatialAnchor = anchorGameObject.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(spatialAnchor);

                    m_colocationManager.AlignUserToAnchor(spatialAnchor);
                    Debug.Log($"Motif: Colocation complete. Calibration error: {m_colocationManager.GetCurrentCalibrationError():F2}mm");
                    return;
                }

                Debug.LogWarning($"Motif: Failed to localize anchor: {unboundAnchor.Uuid}");
            }
        }
    }
}
#endif
