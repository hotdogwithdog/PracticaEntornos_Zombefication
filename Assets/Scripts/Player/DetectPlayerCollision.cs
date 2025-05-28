using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class DetectPlayerCollision : MonoBehaviour
    {
        [SerializeField] private AudioClip pickupSound; // Sonido al recoger la moneda
        public int _coinId;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) // Verifica si el jugador tocó la moneda
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null && !player.isZombie) // Verifica si el jugador no es un zombie
                {
                    player.CoinCollected(_coinId);
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                    Destroy(gameObject); // Elimina la moneda de la escena
                }
            }
        }
    }
}



