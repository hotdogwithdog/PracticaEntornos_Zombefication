using UnityEngine;
using Player;
using Level;
using Unity.Netcode;

namespace Enemies
{
    public class ZombieCollisionHandler : NetworkBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (!IsHost) return;

            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            Debug.Log("Colisión detectada con " + collision.gameObject.name);
            if (playerController != null && !playerController.isZombie)
            {
                playerController.isZombie = true;
                Debug.Log("PlayerController encontrado: " + playerController.playerName.Value);

                // Obtener el prefab de humano desde el LevelManager
                LevelManager levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null && collision.gameObject.name.Contains(levelManager.PlayerPrefabName))
                {
                    // Cambiar el humano a zombie
                    levelManager.ChangeToZombie(collision.gameObject);
                }
            }
        }
    }
}



