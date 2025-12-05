// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using UnityEngine;
using Meta.XR.Samples;

namespace MRMotifs.SharedActivities.ShootingSample
{
    /// <summary>
    /// Loads and provides audio clips for the shooting game from Resources/Audio folder.
    /// Attach to the [MR Motif] Shooting Game Manager to auto-assign sounds.
    /// Audio files should be placed in Assets/Resources/Audio/ folder.
    /// </summary>
    [MetaCodeSample("MRMotifs-SharedActivities")]
    public class ShootingAudioMotif : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("Volume for sounds (0-1).")]
        [SerializeField] private float m_volume = 0.5f;

        [Header("Audio Clips (Auto-loaded from Resources/Audio/)")]
        [SerializeField] private AudioClip m_roundStartClip;
        [SerializeField] private AudioClip m_roundEndClip;
        [SerializeField] private AudioClip m_countdownTickClip;
        [SerializeField] private AudioClip m_hitClip;
        [SerializeField] private AudioClip m_deathClip;
        [SerializeField] private AudioClip m_respawnClip;
        [SerializeField] private AudioClip m_fireClip;

        public AudioClip RoundStartClip => m_roundStartClip;
        public AudioClip RoundEndClip => m_roundEndClip;
        public AudioClip CountdownTickClip => m_countdownTickClip;
        public AudioClip HitClip => m_hitClip;
        public AudioClip DeathClip => m_deathClip;
        public AudioClip RespawnClip => m_respawnClip;
        public AudioClip FireClip => m_fireClip;

        private void Awake()
        {
            LoadAudioClips();
            AssignToGameManager();
        }

        private void LoadAudioClips()
        {
            // Load audio clips from Resources/Audio folder
            // Files must be in Assets/Resources/Audio/ for Resources.Load to work
            m_roundStartClip = LoadClip("Audio/RoundStart");
            m_roundEndClip = LoadClip("Audio/RoundEnd");
            m_countdownTickClip = LoadClip("Audio/CountdownTick");
            m_hitClip = LoadClip("Audio/Hit");
            m_deathClip = LoadClip("Audio/Death");
            m_respawnClip = LoadClip("Audio/Respawn");
            m_fireClip = LoadClip("Audio/Fire");

            int loadedCount = 0;
            if (m_roundStartClip != null) loadedCount++;
            if (m_roundEndClip != null) loadedCount++;
            if (m_countdownTickClip != null) loadedCount++;
            if (m_hitClip != null) loadedCount++;
            if (m_deathClip != null) loadedCount++;
            if (m_respawnClip != null) loadedCount++;
            if (m_fireClip != null) loadedCount++;

            Debug.Log($"[ShootingAudioMotif] Loaded {loadedCount}/7 audio clips from Resources/Audio/");
        }

        private AudioClip LoadClip(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[ShootingAudioMotif] Could not load audio clip: {path}");
            }
            return clip;
        }

        private void AssignToGameManager()
        {
            var gameManager = GetComponent<ShootingGameManagerMotif>();
            if (gameManager != null)
            {
                // Use reflection to set the private audio fields
                var type = typeof(ShootingGameManagerMotif);
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                
                var roundStartField = type.GetField("m_roundStartSound", flags);
                var roundEndField = type.GetField("m_roundEndSound", flags);

                if (roundStartField != null && m_roundStartClip != null)
                {
                    roundStartField.SetValue(gameManager, m_roundStartClip);
                    Debug.Log("[ShootingAudioMotif] Assigned RoundStart sound to GameManager");
                }
                if (roundEndField != null && m_roundEndClip != null)
                {
                    roundEndField.SetValue(gameManager, m_roundEndClip);
                    Debug.Log("[ShootingAudioMotif] Assigned RoundEnd sound to GameManager");
                }
            }
        }

        /// <summary>
        /// Play a specific clip at a position in world space.
        /// </summary>
        public void PlayClipAtPoint(AudioClip clip, Vector3 position)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, m_volume);
            }
        }

        /// <summary>
        /// Get the singleton instance (finds in scene).
        /// </summary>
        public static ShootingAudioMotif Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindAnyObjectByType<ShootingAudioMotif>();
                }
                return s_instance;
            }
        }
        private static ShootingAudioMotif s_instance;
    }
}
#endif
