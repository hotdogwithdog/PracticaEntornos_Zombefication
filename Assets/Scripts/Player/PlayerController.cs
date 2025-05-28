using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode.Components;
using Level;

namespace Player
{
    public class PlayerController : NetworkBehaviour
    {
        #region NetWorkVariables
        public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector2> _move = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
        #endregion

        private LevelManager _levelManager;

        [Header("Character settings")]
        public bool isZombie = false; // This is not neccesary that are a networkVariable because the player and zombie are created and destroy in all the clients and this is constant

        [Header("Movement Settings")]
        public float moveSpeed = 5f;           // Velocidad de movimiento
        public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
        public NetworkAnimator animator;              // Referencia al Animator
        public Transform cameraTransform;      // Referencia a la cámara

        private float _horizontalInput;
        private float _verticalInput;

        private void Awake()
        {
            _levelManager = GameObject.FindWithTag("LevelManager").GetComponent<LevelManager>();
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

        public void CoinCollected(int coinId)
        {
            if (isZombie) return;

            CoinCollectedRpc(coinId);
        }

        #region NetworkMethods
        [Rpc(SendTo.Server)]
        public void CoinCollectedRpc(int coinId)
        {
            _levelManager.CoinCollected(coinId);
        }
        #endregion
    }
}


