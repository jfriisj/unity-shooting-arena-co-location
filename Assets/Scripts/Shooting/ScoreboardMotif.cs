using UnityEngine;
using Unity.Netcode;
using TMPro;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Displays player scores.
    /// </summary>
    public class ScoreboardMotif : NetworkBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_scoreText;

        public void UpdateScores(string scoreString)
        {
            if (m_scoreText != null)
            {
                m_scoreText.text = scoreString;
            }
        }
    }
}
