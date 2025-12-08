using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    public class SharedSpatialAnchorManager : MonoBehaviour
    {
        [SerializeField] private ColocationManager m_colocationManager;
        [SerializeField] private SpaceSharingManager m_spaceSharingManager;
        
        private Guid m_sharedAnchorGroupId;

        public Guid SharedAnchorGroupId => m_sharedAnchorGroupId;

        private void Awake()
        {
            if (m_colocationManager == null)
            {
                m_colocationManager = GetComponent<ColocationManager>();
            }
        }

        // Host Flow
        public async Task HostSession()
        {
            var result = await OVRColocationSession.StartAdvertisementAsync(null);
            if (result.Success)
            {
                m_sharedAnchorGroupId = result.Value;
                Debug.Log($"Session advertised with Group UUID: {m_sharedAnchorGroupId}");
                
                if (m_spaceSharingManager != null)
                {
                    m_spaceSharingManager.SetSharedAnchorGroupId(m_sharedAnchorGroupId);
                }
            }
            else
            {
                Debug.LogError($"Failed to advertise session: {result.Status}");
            }
        }

        // Client Flow
        public async Task JoinSession()
        {
            OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;
            var result = await OVRColocationSession.StartDiscoveryAsync();
             if (!result.Success)
            {
                Debug.LogError($"Failed to start discovery: {result.Status}");
            }
        }

        private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
        {
            m_sharedAnchorGroupId = session.AdvertisementUuid;
            Debug.Log($"Session discovered: {m_sharedAnchorGroupId}");
            OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;
            OVRColocationSession.StopDiscoveryAsync();
            
            if (m_spaceSharingManager != null)
            {
                m_spaceSharingManager.SetSharedAnchorGroupId(m_sharedAnchorGroupId);
            }
            
            LoadAndAlignToAnchor(m_sharedAnchorGroupId);
        }

        private async void LoadAndAlignToAnchor(Guid groupUuid)
        {
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var result = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupUuid, unboundAnchors);
            
            if (result.Success)
            {
                foreach (var unbound in unboundAnchors)
                {
                    if (await unbound.LocalizeAsync())
                    {
                        var anchorGO = new GameObject("SharedAnchor");
                        var spatialAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
                        unbound.BindTo(spatialAnchor);
                        
                        if (m_colocationManager != null)
                        {
                            m_colocationManager.AlignUserToAnchor(spatialAnchor);
                        }
                    }
                }
            }
            else
            {
                 Debug.LogError($"Failed to load shared anchors: {result.Status}");
            }
        }
    }
}
