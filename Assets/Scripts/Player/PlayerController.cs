using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode.Components;

namespace Player
{
    public class PlayerController : NetworkBehaviour
    {
        #region NetWorkVariables
        public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector2> _move = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        #endregion

        private TextMeshProUGUI coinText;

        [Header("Stats")]
        public int CoinsCollected = 0;

        [Header("Character settings")]
        public bool isZombie = false; // Añadir una propiedad para el estado del jugador

        [Header("Movement Settings")]
        public float moveSpeed = 5f;           // Velocidad de movimiento
        public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
        public NetworkAnimator animator;              // Referencia al Animator
        public Transform cameraTransform;      // Referencia a la cámara

        private float _horizontalInput;
        private float _verticalInput;

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
            _horizontalInput = movement.x;  // this two for client animations
            _verticalInput = movement.y;
        }

        void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner && cameraTransform != null)
            {
                Vector3 moveDirection = (cameraTransform.forward * _verticalInput + cameraTransform.right * _horizontalInput).normalized;
                _move.Value = new Vector2(moveDirection.x, moveDirection.z);
            }

            if (IsHost)
            {
                // Mover el jugador
                MovePlayer();
                // Manejar las animaciones del jugador
                HandleAnimations();
            }
        }

        void MovePlayer()
        {
            Vector3 moveDirection = new Vector3(_move.Value.x, 0, _move.Value.y);

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
            animator.Animator.SetFloat("Speed", Mathf.Abs(_move.Value.x) + Mathf.Abs(_move.Value.y));  // Controla el movimiento (caminar/correr)
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


