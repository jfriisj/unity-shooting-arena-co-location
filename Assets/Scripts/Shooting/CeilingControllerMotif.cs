using UnityEngine;
using System.Collections;

namespace MRMotifs.Shooting
{
    public class CeilingControllerMotif : MonoBehaviour
    {
        [SerializeField] private Material m_ceilingMaterial;
        [SerializeField] private float m_animationDuration = 2.0f;
        [SerializeField] private Texture2D m_noiseTexture;

        private int m_visibilityPropID;
        private Coroutine m_animationCoroutine;

        private void Awake()
        {
            m_visibilityPropID = Shader.PropertyToID("_Visibility");
            
            // Generate noise texture if missing
            if (m_noiseTexture == null)
            {
                m_noiseTexture = GenerateNoiseTexture();
            }

            if (m_ceilingMaterial != null)
            {
                m_ceilingMaterial.SetTexture("_MainTex", m_noiseTexture);
                // Start closed (Visibility = 1)
                m_ceilingMaterial.SetFloat(m_visibilityPropID, 1.0f);
            }
        }

        private void OnEnable()
        {
            DroneSpawnerMotif.OnWaveStarted += OnWaveStarted;
            DroneSpawnerMotif.OnWaveCompleted += OnWaveCompleted;
        }

        private void OnDisable()
        {
            DroneSpawnerMotif.OnWaveStarted -= OnWaveStarted;
            DroneSpawnerMotif.OnWaveCompleted -= OnWaveCompleted;
        }

        private void OnWaveStarted(int wave)
        {
            // Open ceiling when wave starts
            OpenCeiling();
        }

        private void OnWaveCompleted(int wave)
        {
            // Close ceiling when wave ends (optional, or keep it open)
            // For now, let's keep it open or maybe close it after a delay?
            // Discover keeps it open during the game usually.
        }

        public void OpenCeiling()
        {
            if (m_animationCoroutine != null) StopCoroutine(m_animationCoroutine);
            m_animationCoroutine = StartCoroutine(AnimateVisibility(1.0f, 0.0f));
        }

        public void CloseCeiling()
        {
            if (m_animationCoroutine != null) StopCoroutine(m_animationCoroutine);
            m_animationCoroutine = StartCoroutine(AnimateVisibility(0.0f, 1.0f));
        }

        private IEnumerator AnimateVisibility(float start, float end)
        {
            float elapsed = 0f;
            while (elapsed < m_animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / m_animationDuration);
                float current = Mathf.Lerp(start, end, t);
                
                if (m_ceilingMaterial != null)
                {
                    m_ceilingMaterial.SetFloat(m_visibilityPropID, current);
                }
                yield return null;
            }
            
            if (m_ceilingMaterial != null)
            {
                m_ceilingMaterial.SetFloat(m_visibilityPropID, end);
            }
        }

        private Texture2D GenerateNoiseTexture()
        {
            int width = 256;
            int height = 256;
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float val = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    pixels[y * width + x] = new Color(val, val, val, 1);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
