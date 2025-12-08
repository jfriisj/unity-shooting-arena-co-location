using UnityEngine;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    public class ColocationManager : MonoBehaviour
    {
        private Transform m_cameraRigTransform;
        private OVRSpatialAnchor m_lastAnchor;

        public bool HasCalibrated { get; private set; }

        private void Awake()
        {
            var cameraRig = FindAnyObjectByType<OVRCameraRig>();
            if (cameraRig)
            {
                m_cameraRigTransform = cameraRig.transform;
            }
            else
            {
                Debug.LogError("OVRCameraRig not found in scene.");
            }
        }

        /// <summary>
        /// Call this when a shared anchor is loaded to align the player.
        /// </summary>
        public void AlignUserToAnchor(OVRSpatialAnchor anchor)
        {
            if (!anchor || !anchor.Localized)
            {
                Debug.LogError("Invalid or un-localized anchor");
                return;
            }

            if (m_cameraRigTransform == null)
            {
                 var cameraRig = FindAnyObjectByType<OVRCameraRig>();
                 if (cameraRig) m_cameraRigTransform = cameraRig.transform;
                 else return;
            }

            var anchorTransform = anchor.transform;

            // The magic formula for camera rig alignment:
            m_cameraRigTransform.position = anchorTransform.InverseTransformPoint(Vector3.zero);
            m_cameraRigTransform.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);

            m_lastAnchor = anchor;
            HasCalibrated = true;
        }

        public void RealignToLastAnchor()
        {
            if (m_lastAnchor != null)
            {
                AlignUserToAnchor(m_lastAnchor);
            }
        }
    }
}
