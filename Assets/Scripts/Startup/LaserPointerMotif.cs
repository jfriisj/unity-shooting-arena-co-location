// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.Startup
{
    /// <summary>
    /// Simple laser pointer for UI interaction.
    /// Updates the LineRenderer to show where the controller is pointing.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    [RequireComponent(typeof(LineRenderer))]
    public class LaserPointerMotif : MonoBehaviour
    {
        [SerializeField] private float m_maxLength = 10f;
        [SerializeField] private Color m_defaultColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        [SerializeField] private Color m_hoverColor = new Color(0.4f, 0.8f, 1f, 1f);

        private LineRenderer m_lineRenderer;
        private Material m_material;

        private void Awake()
        {
            m_lineRenderer = GetComponent<LineRenderer>();
            
            // Configure line renderer
            m_lineRenderer.useWorldSpace = true;
            m_lineRenderer.startWidth = 0.005f;
            m_lineRenderer.endWidth = 0.002f;
            m_lineRenderer.positionCount = 2;
            
            // Create material
            m_material = new Material(Shader.Find("Unlit/Color"));
            m_material.color = m_defaultColor;
            m_lineRenderer.material = m_material;
        }

        private void Update()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * m_maxLength;
            
            // Raycast to find end point and change color on hover
            if (Physics.Raycast(startPos, transform.forward, out RaycastHit hit, m_maxLength))
            {
                endPos = hit.point;
                m_material.color = m_hoverColor;
            }
            else
            {
                m_material.color = m_defaultColor;
            }

            m_lineRenderer.SetPosition(0, startPos);
            m_lineRenderer.SetPosition(1, endPos);
        }

        private void OnDestroy()
        {
            if (m_material != null)
            {
                Destroy(m_material);
            }
        }
    }
}
