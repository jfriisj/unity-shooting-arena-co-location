// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Manages room configuration persistence for quick session restarts and large room support.
    /// Saves room UUIDs to PlayerPrefs while Meta OS persists actual room mesh data.
    /// </summary>
    [MetaCodeSample("MRMotifs-ColocatedExperiences")]
    [Serializable]
    public class SavedRoomData
    {
        public string RoomName;
        public List<string> RoomUuids;
        public string SavedAt;
    }

    [Serializable]
    public class SavedRoomsCollection
    {
        public List<SavedRoomData> Rooms = new List<SavedRoomData>();
    }

    public class RoomPersistenceManager : MonoBehaviour
    {
        private const string SAVED_ROOMS_KEY = "SavedRoomConfigurations";

        /// <summary>
        /// Save current MRUK rooms with a name.
        /// </summary>
        public void SaveCurrentRooms(string roomName)
        {
            var rooms = MRUK.Instance?.Rooms;
            if (rooms == null || !rooms.Any())
            {
                Debug.LogWarning("[RoomPersistence] No rooms to save.");
                return;
            }

            var savedRooms = GetAllSavedRooms();
            
            // Remove existing with same name
            savedRooms.Rooms.RemoveAll(r => r.RoomName == roomName);
            
            // Add new entry
            savedRooms.Rooms.Add(new SavedRoomData
            {
                RoomName = roomName,
                RoomUuids = rooms.Select(r => r.Anchor.Uuid.ToString()).ToList(),
                SavedAt = DateTime.UtcNow.ToString("o")
            });

            var json = JsonUtility.ToJson(savedRooms);
            PlayerPrefs.SetString(SAVED_ROOMS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[RoomPersistence] Saved {rooms.Count()} rooms as '{roomName}'");
        }

        /// <summary>
        /// Get list of all saved room configurations.
        /// </summary>
        public SavedRoomsCollection GetAllSavedRooms()
        {
            var json = PlayerPrefs.GetString(SAVED_ROOMS_KEY, "{}");
            try
            {
                return JsonUtility.FromJson<SavedRoomsCollection>(json) 
                       ?? new SavedRoomsCollection();
            }
            catch
            {
                return new SavedRoomsCollection();
            }
        }

        /// <summary>
        /// Load a saved room configuration by name.
        /// </summary>
        public async Task<bool> LoadSavedRoom(string roomName)
        {
            var savedRooms = GetAllSavedRooms();
            var roomData = savedRooms.Rooms.FirstOrDefault(r => r.RoomName == roomName);
            
            if (roomData == null)
            {
                Debug.LogError($"[RoomPersistence] No saved room found with name: {roomName}");
                return false;
            }

            Debug.Log($"[RoomPersistence] Loading saved room: {roomName} with {roomData.RoomUuids.Count} room(s)");
            
            // Load rooms from device - they should still be persisted by Meta OS
            await MRUK.Instance.LoadSceneFromDevice();
            
            // Verify our saved rooms are still available
            var loadedRoomUuids = MRUK.Instance.Rooms.Select(r => r.Anchor.Uuid.ToString()).ToHashSet();
            var missingRooms = roomData.RoomUuids.Where(u => !loadedRoomUuids.Contains(u)).ToList();
            
            if (missingRooms.Any())
            {
                Debug.LogWarning($"[RoomPersistence] {missingRooms.Count} saved room(s) no longer available. User may need to rescan.");
            }

            return !missingRooms.Any();
        }

        /// <summary>
        /// Delete a saved room configuration.
        /// </summary>
        public void DeleteSavedRoom(string roomName)
        {
            var savedRooms = GetAllSavedRooms();
            savedRooms.Rooms.RemoveAll(r => r.RoomName == roomName);
            
            var json = JsonUtility.ToJson(savedRooms);
            PlayerPrefs.SetString(SAVED_ROOMS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[RoomPersistence] Deleted saved room: {roomName}");
        }

        /// <summary>
        /// Get room at a specific world position.
        /// </summary>
        public MRUKRoom GetRoomAtPosition(Vector3 worldPosition)
        {
            if (MRUK.Instance == null) return null;

            foreach (var room in MRUK.Instance.Rooms)
            {
                if (room.IsPositionInRoom(worldPosition))
                {
                    return room;
                }
            }
            return null;
        }
    }
}
