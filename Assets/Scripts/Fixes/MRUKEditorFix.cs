using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Fixes MRUK initialization for Unity Editor testing
/// Based on Meta Quest documentation: https://developers.meta.com/horizon/llmstxt/documentation/unity/unity-mr-utility-kit-gs.md
/// </summary>
public class MRUKEditorFix : MonoBehaviour
{
    [Header("Editor Testing Settings")]
    [Tooltip("Automatically load fake room data in Unity Editor for testing")]
    public bool m_useSimulatedRoomInEditor = true;
    
    [Tooltip("Room prefab to use for simulation (should have MRUKRoom component)")]
    public GameObject m_roomPrefab;

    void Awake()
    {
        // Check if we're in Unity Editor and need to simulate room data
        if (Application.isEditor && m_useSimulatedRoomInEditor)
        {
            SetupEditorRoomSimulation();
        }
    }

    void SetupEditorRoomSimulation()
    {
        // Wait for MRUK to initialize, then provide fake room data
        StartCoroutine(WaitForMRUKAndSetupRoom());
    }

    System.Collections.IEnumerator WaitForMRUKAndSetupRoom()
    {
        // Wait for MRUK to be ready
        while (MRUK.Instance == null)
        {
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("[MRUKEditorFix] MRUK Instance found, setting up simulated room for Unity Editor");

        // If no room prefab specified, create a basic room
        if (m_roomPrefab == null)
        {
            CreateBasicSimulatedRoom();
        }
        else
        {
            // Instantiate the provided room prefab
            GameObject roomInstance = Instantiate(m_roomPrefab);
            Debug.Log($"[MRUKEditorFix] Instantiated room prefab: {m_roomPrefab.name}");
        }
    }

    void CreateBasicSimulatedRoom()
    {
        Debug.Log("[MRUKEditorFix] Creating basic simulated room for Unity Editor testing");
        
        // Create a basic room GameObject with MRUKRoom component
        GameObject roomObject = new GameObject("Simulated_Room");
        MRUKRoom room = roomObject.AddComponent<MRUKRoom>();
        
        // Create basic room anchors (floor, ceiling, walls)
        CreateRoomAnchor(roomObject, "Floor", MRUKAnchor.SceneLabels.FLOOR, Vector3.zero, new Vector3(10f, 0.1f, 10f));
        CreateRoomAnchor(roomObject, "Ceiling", MRUKAnchor.SceneLabels.CEILING, new Vector3(0, 3f, 0), new Vector3(10f, 0.1f, 10f));
        
        // Create walls
        CreateRoomAnchor(roomObject, "Wall_North", MRUKAnchor.SceneLabels.WALL_FACE, new Vector3(0, 1.5f, 5f), new Vector3(10f, 3f, 0.1f));
        CreateRoomAnchor(roomObject, "Wall_South", MRUKAnchor.SceneLabels.WALL_FACE, new Vector3(0, 1.5f, -5f), new Vector3(10f, 3f, 0.1f));
        CreateRoomAnchor(roomObject, "Wall_East", MRUKAnchor.SceneLabels.WALL_FACE, new Vector3(5f, 1.5f, 0), new Vector3(0.1f, 3f, 10f));
        CreateRoomAnchor(roomObject, "Wall_West", MRUKAnchor.SceneLabels.WALL_FACE, new Vector3(-5f, 1.5f, 0), new Vector3(0.1f, 3f, 10f));
        
        Debug.Log("[MRUKEditorFix] Basic simulated room created successfully");
    }

    void CreateRoomAnchor(GameObject parentRoom, string anchorName, MRUKAnchor.SceneLabels label, Vector3 position, Vector3 size)
    {
        GameObject anchorObject = new GameObject(anchorName);
        anchorObject.transform.SetParent(parentRoom.transform);
        anchorObject.transform.localPosition = position;
        
        MRUKAnchor anchor = anchorObject.AddComponent<MRUKAnchor>();
        
        // Create a box collider for physics
        BoxCollider collider = anchorObject.AddComponent<BoxCollider>();
        collider.size = size;
        
        Debug.Log($"[MRUKEditorFix] Created room anchor: {anchorName} with label: {label}");
    }

    void OnValidate()
    {
        // Ensure this only runs in Unity Editor
        if (!Application.isEditor)
        {
            m_useSimulatedRoomInEditor = false;
        }
    }
}