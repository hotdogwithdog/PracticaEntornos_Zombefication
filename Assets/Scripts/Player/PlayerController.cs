using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

namespace Player
{
    public class PlayerController : NetworkBehaviour
    {
        #region NetWorkVariables
        public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        private TextMeshProUGUI coinText;

        [Header("Stats")]
        public int CoinsCollected = 0;

        [Header("Character settings")]
        public bool isZombie = false; // Añadir una propiedad para el estado del jugador

        [Header("Movement Settings")]
        public float moveSpeed = 5f;           // Velocidad de movimiento
        public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
        public Animator animator;              // Referencia al Animator
        public Transform cameraTransform;      // Referencia a la cámara

        private float horizontalInput;         // Entrada horizontal (A/D o flechas)
        private float verticalInput;           // Entrada vertical (W/S o flechas)

        void Start()
        {
            // Buscar el objeto "CanvasPlayer" en la escena
            GameObject canvas = GameObject.Find("CanvasPlayer");

            if (canvas != null)
            {
                Debug.Log("Canvas encontrado");

                // Buscar el Panel dentro del CanvasHud
                Transform panel = canvas.transform.Find("PanelHud");
                if (panel != null)
                {
                    // Buscar el TextMeshProUGUI llamado "CoinsValue" dentro del Panel
                    Transform coinTextTransform = panel.Find("CoinsValue");
                    if (coinTextTransform != null)
                    {
                        coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            UpdateCoinUI();
        }

        private void OnEnable()
        {
            GameInput.InputReader.Instance.onMove += MoveInput;
        }

        private void OnDisable()
        {
            GameInput.InputReader.Instance.onMove -= MoveInput;
        }

        private void MoveInput(Vector2 movement)
        {
            if (!IsOwner) return;
            horizontalInput = movement.x;
            verticalInput = movement.y;
        }

        void Update()
        {
            if (!IsSpawned) return;

            if (IsHost)
            {
                // Mover el jugador
                MovePlayer();
            }

            if (IsOwner)
            {
                // Manejar las animaciones del jugador
                HandleAnimations();
            }
        }

        void MovePlayer()
        {
            if (cameraTransform == null) { return; }

            // Calcular la dirección de movimiento en relación a la cámara
            Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
            moveDirection.y = 0f; // Asegurarnos de que el movimiento es horizontal (sin componente Y)

            // Mover el jugador usando el Transform
            if (moveDirection != Vector3.zero)
            {
                // Calcular la rotación en Y basada en la dirección del movimiento
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);

                // Ajustar la velocidad si es zombie
                float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;

                // Mover al jugador en la dirección deseada
                transform.Translate(moveDirection * adjustedSpeed * Time.deltaTime, Space.World);
            }
        }

        void HandleAnimations()
        {
            // Animaciones basadas en la dirección del movimiento
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));  // Controla el movimiento (caminar/correr)
        }

        public void CoinCollected()
        {
            if (!isZombie) // Solo los humanos pueden recoger monedas
            {
                this.CoinsCollected++;
                UpdateCoinUI();
            }
        }

        void UpdateCoinUI()
        {
            if (coinText != null)
            {
                coinText.text = $"{CoinsCollected}";
            }
        }
    }
}


