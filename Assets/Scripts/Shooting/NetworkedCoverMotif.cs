// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using Fusion;
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// A networked cover object that can be spawned by the host.
    /// Provides physical barriers for players to hide behind in large open spaces.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class NetworkedCoverMotif : NetworkBehaviour
    {
        [Header("Cover Settings")]
        [Tooltip("The size of the cover object.")]
        [SerializeField] private Vector3 m_size = new Vector3(1f, 1.5f, 0.3f);

        [Tooltip("Color of the cover object.")]
        [SerializeField] private Color m_coverColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);

        [Tooltip("Material smoothness value (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float m_materialSmoothness = 0.2f;

        [Header("Network Settings")]
        [Tooltip("Speed of position/rotation interpolation for network sync.")]
        [Range(1f, 50f)]
        [SerializeField] private float m_interpolationSpeed = 10f;

        /// <summary>
        /// Networked position for synchronization.
        /// </summary>
        [Networked] public Vector3 NetworkedPosition { get; set; }

        /// <summary>
        /// Networked rotation for synchronization.
        /// </summary>
        [Networked] public Quaternion NetworkedRotation { get; set; }

        private MeshRenderer m_renderer;
        private BoxCollider m_collider;

        private void Awake()
        {
            // Create visual mesh
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateCubeMesh();

            m_renderer = gameObject.AddComponent<MeshRenderer>();
            m_renderer.material = CreateCoverMaterial();

            // Add collider for bullet collision
            m_collider = gameObject.AddComponent<BoxCollider>();
            m_collider.size = m_size;
            m_collider.center = new Vector3(0, m_size.y / 2f, 0);

            // Set layer for physics
            gameObject.layer = 0; // Default layer
        }

        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasStateAuthority)
            {
                NetworkedPosition = transform.position;
                NetworkedRotation = transform.rotation;
            }
            else
            {
                // Apply networked transform
                transform.position = NetworkedPosition;
                transform.rotation = NetworkedRotation;
            }

            // Scale the mesh to match size
            transform.localScale = m_size;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                // Interpolate to networked position
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, m_interpolationSpeed * Runner.DeltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, NetworkedRotation, m_interpolationSpeed * Runner.DeltaTime);
            }
        }

        /// <summary>
        /// Initialize the cover with position and rotation.
        /// </summary>
        public void Initialize(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            NetworkedPosition = position;
            NetworkedRotation = rotation;
        }

        private Mesh CreateCubeMesh()
        {
            // Use Unity's built-in cube mesh
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = cube.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(cube);
            return mesh;
        }

        private Material CreateCoverMaterial()
        {
            // Create a simple unlit material
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = m_coverColor;
            material.SetFloat("_Smoothness", m_materialSmoothness);
            return material;
        }
    }
}
#endif
