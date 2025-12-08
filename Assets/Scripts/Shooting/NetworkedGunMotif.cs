using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace MRMotifs.Shooting
{
    /// <summary>
    /// Controls a physical networked gun that can be picked up and fired.
    /// Handles ammo, reloading, and networked bullet spawning.
    /// </summary>
    public class NetworkedGunMotif : NetworkBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] private NetworkObject m_bulletPrefab;
        [SerializeField] private Transform m_muzzlePoint;
        [SerializeField] private float m_fireRate = 0.2f;
        [SerializeField] private float m_bulletForce = 20f;
        [SerializeField] private int m_maxAmmo = 30;
        [SerializeField] private float m_reloadTime = 2.5f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem m_muzzleFlash;
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_fireSound;
        [SerializeField] private AudioClip m_reloadSound;
        [SerializeField] private AudioClip m_emptyClickSound;

        // Networked State
        private NetworkVariable<int> m_currentAmmo = new NetworkVariable<int>(30);
        private NetworkVariable<bool> m_isReloading = new NetworkVariable<bool>(false);

        private float m_nextFireTime;

        public int CurrentAmmo => m_currentAmmo.Value;
        public bool IsReloading => m_isReloading.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_currentAmmo.Value = m_maxAmmo;
            }
        }

        private void Update()
        {
            // Only the owner (the player holding the gun) controls input
            if (!IsOwner) return;

            HandleInput();
        }

        private void HandleInput()
        {
            if (m_isReloading.Value) return;

            // Reload Input (A Button or X Button)
            if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
            {
                if (m_currentAmmo.Value < m_maxAmmo)
                {
                    RequestReloadServerRpc();
                }
            }

            // Fire Input (Index Trigger)
            // We check both controllers since we don't know which hand holds it yet (can be refined)
            if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
            {
                if (Time.time >= m_nextFireTime)
                {
                    m_nextFireTime = Time.time + m_fireRate;
                    TryFire();
                }
            }
        }

        private void TryFire()
        {
            if (m_currentAmmo.Value > 0)
            {
                FireServerRpc(m_muzzlePoint.position, m_muzzlePoint.rotation);
            }
            else
            {
                // Play empty click locally
                if (m_audioSource && m_emptyClickSound)
                {
                    m_audioSource.PlayOneShot(m_emptyClickSound);
                }
            }
        }

        [ServerRpc]
        private void FireServerRpc(Vector3 position, Quaternion rotation)
        {
            if (m_currentAmmo.Value <= 0 || m_isReloading.Value) return;

            // Decrement Ammo
            m_currentAmmo.Value--;

            // Spawn Bullet
            if (m_bulletPrefab != null)
            {
                NetworkObject bulletInstance = Instantiate(m_bulletPrefab, position, rotation);
                bulletInstance.Spawn();

                // Apply Force
                Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(rotation * Vector3.forward * m_bulletForce, ForceMode.Impulse);
                }
            }

            // Trigger Effects on all clients
            FireClientRpc();
        }

        [ClientRpc]
        private void FireClientRpc()
        {
            // Visuals
            if (m_muzzleFlash != null)
            {
                m_muzzleFlash.Play();
            }

            // Audio
            if (m_audioSource && m_fireSound)
            {
                m_audioSource.PlayOneShot(m_fireSound);
            }
        }

        [ServerRpc]
        private void RequestReloadServerRpc()
        {
            if (m_isReloading.Value) return;

            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            m_isReloading.Value = true;
            
            // Notify clients to play reload sound
            ReloadEffectsClientRpc();

            yield return new WaitForSeconds(m_reloadTime);

            m_currentAmmo.Value = m_maxAmmo;
            m_isReloading.Value = false;
        }

        [ClientRpc]
        private void ReloadEffectsClientRpc()
        {
            if (m_audioSource && m_reloadSound)
            {
                m_audioSource.PlayOneShot(m_reloadSound);
            }
        }
    }
}
