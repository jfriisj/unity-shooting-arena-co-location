using System;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Meta.XR.MRUtilityKit;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    public class SpaceSharingManager : NetworkBehaviour
    {
        public NetworkVariable<FixedString512Bytes> NetworkedRoomUuids = new NetworkVariable<FixedString512Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            
        public NetworkVariable<FixedString128Bytes> NetworkedRemoteFloorPose = new NetworkVariable<FixedString128Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        private Guid m_sharedAnchorGroupId;

        public void SetSharedAnchorGroupId(Guid groupId)
        {
            m_sharedAnchorGroupId = groupId;
            // If we already have room UUIDs, try to load
            if (!IsServer && !string.IsNullOrEmpty(NetworkedRoomUuids.Value.ToString()))
            {
                LoadSharedRoom(m_sharedAnchorGroupId);
            }
        }

        // Host: Share rooms after colocation advertisement
        public async void ShareMrukRooms()
        {
            if (!IsServer) return;
            if (MRUK.Instance == null) return;

            var rooms = MRUK.Instance.Rooms;
            if (rooms == null || rooms.Count == 0) return;

            var result = await MRUK.Instance.ShareRoomsAsync(rooms, m_sharedAnchorGroupId);
            
            if (result.Success)
            {
                // Store UUIDs for clients to load
                var uuids = string.Join(",", rooms.Select(r => r.Anchor.Uuid));
                NetworkedRoomUuids.Value = new FixedString512Bytes(uuids);
                
                // Store floor pose for alignment
                var pose = rooms[0].FloorAnchor.transform;
                var poseString = $"{pose.position.x},{pose.position.y},{pose.position.z},{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}";
                NetworkedRemoteFloorPose.Value = new FixedString128Bytes(poseString);
            }
            else
            {
                Debug.LogError($"Failed to share rooms: {result.Status}");
            }
        }

        // Client: Load shared rooms
        public async void LoadSharedRoom(Guid groupUuid)
        {
            if (string.IsNullOrEmpty(NetworkedRoomUuids.Value.ToString())) return;

            var roomUuids = NetworkedRoomUuids.Value.ToString().Split(',').Select(Guid.Parse).ToArray();
            
            // Parse pose
            var poseParts = NetworkedRemoteFloorPose.Value.ToString().Split(',');
            if (poseParts.Length < 7) return;

            var pos = new Vector3(
                float.Parse(poseParts[0]),
                float.Parse(poseParts[1]),
                float.Parse(poseParts[2])
            );
            var rot = new Quaternion(
                float.Parse(poseParts[3]),
                float.Parse(poseParts[4]),
                float.Parse(poseParts[5]),
                float.Parse(poseParts[6])
            );
            
            var remoteFloorPose = new Pose(pos, rot);

            // This loads room mesh AND aligns client automatically
            // Using the signature from instructions: (IEnumerable<Guid> roomUuids, Guid groupUuid, (Guid, Pose)? alignmentData = null)
            var result = await MRUK.Instance.LoadSceneFromSharedRooms(
                roomUuids, 
                groupUuid, 
                (roomUuids[0], remoteFloorPose)
            );
             
             if (result != MRUK.LoadDeviceResult.Success)
             {
                 Debug.LogError("Failed to load scene from shared rooms");
             }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkedRoomUuids.OnValueChanged += OnRoomUuidsChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            NetworkedRoomUuids.OnValueChanged -= OnRoomUuidsChanged;
        }

        private void OnRoomUuidsChanged(FixedString512Bytes previousValue, FixedString512Bytes newValue)
        {
            if (!IsServer && !string.IsNullOrEmpty(newValue.ToString()) && m_sharedAnchorGroupId != Guid.Empty)
            {
                LoadSharedRoom(m_sharedAnchorGroupId);
            }
        }
    }
}
