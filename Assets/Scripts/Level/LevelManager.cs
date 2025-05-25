using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Player;
using Utilities;
using Unity.Netcode;
using Network;
using Unity.Collections;

namespace Level
{
    public enum GameMode
    {
        Tiempo,
        Monedas
    }

    public class LevelManager : NetworkBehaviour
    {
        #region NetworkVariables
        private NetworkVariable<int> numberOfHumans = new NetworkVariable<int>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> numberOfZombies = new NetworkVariable<int>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        #endregion

        #region OnlyServer
        private HashSet<ulong> _zombies;
        private HashSet<ulong> _humans;
        private int indexForHumans = 0;
        private int indexForZombies = 0;
        #endregion

        private GameManager _gameManager;

        #region Properties

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject zombiePrefab;

        [Header("Game Mode Settings")]
        [Tooltip("Selecciona el modo de juego")]
        [SerializeField] private GameMode gameMode;

        [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
        [SerializeField] private int minutes = 5;

        private List<Vector3> humanSpawnPoints = new List<Vector3>();
        private List<Vector3> zombieSpawnPoints = new List<Vector3>();

        // Referencias a los elementos de texto en el canvas
        private TextMeshProUGUI humansText;
        private TextMeshProUGUI zombiesText;
        private TextMeshProUGUI gameModeText;

        private int CoinsGenerated = 0;

        public string PlayerPrefabName => playerPrefab.name;
        public string ZombiePrefabName => zombiePrefab.name;

        private LevelBuilder levelBuilder;

        private PlayerController playerController;

        private float remainingSeconds;
        private bool isGameOver = false;

        public GameObject gameOverPanel; // Asigna el panel desde el inspector

        #endregion

        #region Unity game loop methods

        private void OnEnable()
        {
            GameInput.InputReader.Instance.onHumanConvert += OnHumanConvert;
            GameInput.InputReader.Instance.onZombieConvert += OnZombieConvert;
        }

        private void OnDisable()
        {
            GameInput.InputReader.Instance.onHumanConvert -= OnHumanConvert;
            GameInput.InputReader.Instance.onZombieConvert -= OnZombieConvert;
        }

        private void Awake()
        {
            Debug.Log("Despertando el nivel");

            // Obtener la referencia al LevelBuilder
            levelBuilder = GetComponent<LevelBuilder>();

            Time.timeScale = 1f; // Asegurarse de que el tiempo no esté detenido
        }

        private void Start()
        {
            Debug.Log("Iniciando el nivel");
            // Buscar el objeto "CanvasPlayer" en la escena
            GameObject canvas = GameObject.Find("CanvasPlayer");
            if (canvas != null)
            {
                Debug.Log("Canvas encontrado");

                // Buscar el Panel dentro del CanvasHud
                Transform panel = canvas.transform.Find("PanelHud");
                if (panel != null)
                {
                    // Buscar los TextMeshProUGUI llamados "HumansValue" y "ZombiesValue" dentro del Panel
                    Transform humansTextTransform = panel.Find("HumansValue");
                    Transform zombiesTextTransform = panel.Find("ZombiesValue");
                    Transform gameModeTextTransform = panel.Find("GameModeConditionValue");

                    if (humansTextTransform != null)
                    {
                        humansText = humansTextTransform.GetComponent<TextMeshProUGUI>();
                    }

                    if (zombiesTextTransform != null)
                    {
                        zombiesText = zombiesTextTransform.GetComponent<TextMeshProUGUI>();
                    }

                    if (gameModeTextTransform != null)
                    {
                        gameModeText = gameModeTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            
        }

        private void Update()
        {
            if (gameMode == GameMode.Tiempo)
            {
                // Lógica para el modo de juego basado en tiempo
                HandleTimeLimitedGameMode();
            }
            else if (gameMode == GameMode.Monedas)
            {
                // Lógica para el modo de juego basado en monedas
                HandleCoinBasedGameMode();
            }

            UpdateTeamUI();

            if (isGameOver)
            {
                ShowGameOverPanel();
            }
        }

        #endregion

        #region Network

        public override void OnNetworkSpawn()
        {
            _gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
            if (IsHost)
            {
                GenerateTeams();

                GenerateWorldRpc(UnityEngine.Random.Range(0, 10000));


                // Set the teams and spawn(tp) the players to their positions must wait until all the clients have been spawn the map this have a shyncronisation problem
                // TpPlayersToSpawn();

                remainingSeconds = minutes * 60;
            }


        }


        /// <summary>
        /// This RPC it's called by the server when the seed is generated to pass the seed to all the clients and let him generate the same world
        /// </summary>
        /// <param name="seed"></param>
        [Rpc(SendTo.ClientsAndHost)]
        private void GenerateWorldRpc(int seed)
        {
            if (levelBuilder != null)
            {
                levelBuilder.Build(seed);
                humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
                zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
                Debug.Log("SPAWN POINTS PICKED");
                CoinsGenerated = levelBuilder.GetCoinsGenerated();
            }

            // Say to the server that i can spawn (no problems of shyncronisation because the map exist in local at the time this petition is send
            TpMyPlayerObjectToSpawnRpc(NetworkManager.Singleton.LocalClientId);

            UpdateTeamUI();
        }

        [Rpc(SendTo.Server)]
        private void TpMyPlayerObjectToSpawnRpc(ulong clientId)
        {
            GameObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            if (_humans.Contains(clientId))
            {
                TpPlayer(true, player, clientId, humanSpawnPoints[indexForHumans % humanSpawnPoints.Count]);
                indexForHumans++;
            }
            else if (_zombies.Contains(clientId))
            {
                TpPlayer(false, player, clientId, zombieSpawnPoints[indexForZombies % zombieSpawnPoints.Count]);
                indexForZombies++;
            }
            else
            {
                Debug.LogError($"ERROR: The Client with ID {clientId} it's not in any team");
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SetCameraRpc(ulong playerObjectId, RpcParams rpcParams = default)
        {
            Debug.Log($"Client Id: {NetworkManager.Singleton.LocalClientId}");
            // Obtener la referencia a la cámara principal
            Camera mainCamera = Camera.main;
            GameObject player = null;

            foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (gameObject.GetComponent<NetworkObject>().NetworkObjectId == playerObjectId) player = gameObject;
            }

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la cámara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    Debug.Log($"CameraController encontrado en la cámara principal.");
                    // Asignar el jugador al script CameraController
                    cameraController.player = player.transform;
                }

                Debug.Log($"Cámara principal encontrada en {mainCamera}");
                // Obtener el componente PlayerController del jugador instanciado
                playerController = player.GetComponent<PlayerController>();
                // Asignar el transform de la cámara al PlayerController
                if (playerController != null)
                {
                    Debug.Log($"PlayerController encontrado en el jugador instanciado.");
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el jugador instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontró la cámara principal.");
            }
        }

        #endregion

        #region Team management methods

        /// <summary>
        /// Generates two sets for the client ids one of humans the other of zombies and they are all humans minus one of them
        /// </summary>
        private void GenerateTeams()
        {
            _zombies = new HashSet<ulong>();
            _humans = new HashSet<ulong>();

            ulong[] clientsIds = NetworkManager.Singleton.ConnectedClientsIds.ToArray();
            int index;
            if (_gameManager.nPlayers.Value <= 1)
            {
                index = -1;
            }
            else index = UnityEngine.Random.Range(0, _gameManager.nPlayers.Value);

            for (int i = 0; i < _gameManager.nPlayers.Value; ++i)
            {
                if (i == index) _zombies.Add(clientsIds[i]);
                else _humans.Add(clientsIds[i]);
            }

            numberOfHumans.Value = _humans.Count;
            numberOfZombies.Value = _zombies.Count;

            Debug.Log($"nPlayers: {_gameManager.nPlayers.Value}");
            Debug.Log($"HUMANS: {_humans.Count}");
            Debug.Log($"ZOMBIES: {_zombies.Count}");
        }

        private void OnZombieConvert()
        {
            // Comprobar si el jugador actual está usando el prefab de humano
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(playerPrefab.name))
            {
                ChangeToZombie();
            }
            else
            {
                Debug.Log("El jugador actual no es un humano.");
            }
        }

        private void ChangeToZombie()
        {
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            ChangeToZombie(currentPlayer, true);
        }

        public void ChangeToZombie(GameObject human, bool enabled)
        {
            Debug.Log("Cambiando a Zombie");

            if (human != null)
            {
                // Guardar la posición, rotación y uniqueID del humano actual
                Vector3 playerPosition = human.transform.position;
                Quaternion playerRotation = human.transform.rotation;
                string uniqueID = human.GetComponent<PlayerController>().playerName.Value.ToString();

                // Destruir el humano actual
                Destroy(human);

                // Instanciar el prefab del zombie en la misma posición y rotación
                GameObject zombie = Instantiate(zombiePrefab, playerPosition, playerRotation);
                if (enabled) { zombie.tag = "Player"; }

                // Obtener el componente PlayerController del zombie instanciado
                PlayerController playerController = zombie.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = enabled;
                    playerController.isZombie = true; // Cambiar el estado a zombie
                    numberOfHumans.Value--; // Reducir el número de humanos
                    numberOfZombies.Value++; // Aumentar el número de zombis
                    UpdateTeamUI();

                    if (enabled)
                    {
                        // Obtener la referencia a la cámara principal
                        Camera mainCamera = Camera.main;

                        if (mainCamera != null)
                        {
                            // Obtener el script CameraController de la cámara principal
                            CameraController cameraController = mainCamera.GetComponent<CameraController>();

                            if (cameraController != null)
                            {
                                // Asignar el zombie al script CameraController
                                cameraController.player = zombie.transform;
                            }

                            // Asignar el transform de la cámara al PlayerController
                            playerController.cameraTransform = mainCamera.transform;
                        }
                        else
                        {
                            Debug.LogError("No se encontró la cámara principal.");
                        }
                    }
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el zombie instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontró el humano actual.");
            }
        }

        private void OnHumanConvert()
        {
            // Comprobar si el jugador actual está usando el prefab de zombie
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(zombiePrefab.name))
            {
                ChangeToHuman();
            }
            else
            {
                Debug.Log("El jugador actual no es un zombie.");
            }
        }
        private void ChangeToHuman()
        {
            Debug.Log("Cambiando a Humano");

            // Obtener la referencia al jugador actual
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");

            if (currentPlayer != null)
            {
                // Guardar la posición y rotación del jugador actual
                Vector3 playerPosition = currentPlayer.transform.position;
                Quaternion playerRotation = currentPlayer.transform.rotation;

                // Destruir el jugador actual
                Destroy(currentPlayer);

                // Instanciar el prefab del humano en la misma posición y rotación
                GameObject human = Instantiate(playerPrefab, playerPosition, playerRotation);
                human.tag = "Player";

                // Obtener la referencia a la cámara principal
                Camera mainCamera = Camera.main;

                if (mainCamera != null)
                {
                    // Obtener el script CameraController de la cámara principal
                    CameraController cameraController = mainCamera.GetComponent<CameraController>();

                    if (cameraController != null)
                    {
                        // Asignar el humano al script CameraController
                        cameraController.player = human.transform;
                    }

                    // Obtener el componente PlayerController del humano instanciado
                    playerController = human.GetComponent<PlayerController>();
                    // Asignar el transform de la cámara al PlayerController
                    if (playerController != null)
                    {
                        playerController.enabled = true;
                        playerController.cameraTransform = mainCamera.transform;
                        playerController.isZombie = false; // Cambiar el estado a humano
                        numberOfHumans.Value++; // Aumentar el número de humanos
                        numberOfZombies.Value--; // Reducir el número de zombis
                    }
                    else
                    {
                        Debug.LogError("PlayerController no encontrado en el humano instanciado.");
                    }
                }
                else
                {
                    Debug.LogError("No se encontró la cámara principal.");
                }
            }
            else
            {
                Debug.LogError("No se encontró el jugador actual.");
            }
        }

        private void SpawnPlayer(Vector3 spawnPosition, GameObject prefab)
        {
            Debug.Log($"Instanciando jugador en {spawnPosition}");
            if (prefab != null)
            {
                Debug.Log($"Instanciando jugador en {spawnPosition}");
                // Crear una instancia del prefab en el punto especificado
                GameObject player = Instantiate(prefab, spawnPosition, Quaternion.identity);
                player.tag = "Player";
                // Obtener la referencia a la cámara principal
                Camera mainCamera = Camera.main;

                if (mainCamera != null)
                {
                    // Obtener el script CameraController de la cámara principal
                    CameraController cameraController = mainCamera.GetComponent<CameraController>();

                    if (cameraController != null)
                    {
                        Debug.Log($"CameraController encontrado en la cámara principal.");
                        // Asignar el jugador al script CameraController
                        cameraController.player = player.transform;
                    }

                    Debug.Log($"Cámara principal encontrada en {mainCamera}");
                    // Obtener el componente PlayerController del jugador instanciado
                    playerController = player.GetComponent<PlayerController>();
                    // Asignar el transform de la cámara al PlayerController
                    if (playerController != null)
                    {
                        Debug.Log($"PlayerController encontrado en el jugador instanciado.");
                        playerController.enabled = true;
                        playerController.cameraTransform = mainCamera.transform;
                    }
                    else
                    {
                        Debug.LogError("PlayerController no encontrado en el jugador instanciado.");
                    }
                }
                else
                {
                    Debug.LogError("No se encontró la cámara principal.");
                }
            }
            else
            {
                Debug.LogError("Faltan referencias al prefab o al punto de aparición.");
            }
        }

        /// <summary>
        /// Tp the players to the corresponding map positions and change the prefabs to the correct prefabs
        /// </summary>
        private void TpPlayersToSpawn()
        {
            Debug.Log("Instanciando equipos");

            int i = 0;
            Debug.Log($"SIZE OF HUMANSPOINTS: {humanSpawnPoints.Count}");
            Debug.Log($"SIZE OF ZOMBIESPOINTS: {zombieSpawnPoints.Count}");
            foreach (ulong clientId in _humans)
            {
                GameObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
                TpPlayer(true, player, clientId, humanSpawnPoints[i % humanSpawnPoints.Count]);
                i++;
            }

            foreach (ulong clientId in _zombies)
            {
                GameObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
                TpPlayer(false, player, clientId, zombieSpawnPoints[i % zombieSpawnPoints.Count]);
                i++;
            }
        }


        private void TpPlayer(bool isHuman, GameObject player, ulong clientId, Vector3 spawnPosition)
        {
            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            if (isHuman)
            {
                player.transform.position = spawnPosition;
            }
            else
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                // Store all the relevant data
                FixedString64Bytes name = playerController.playerName.Value;
                networkObject.Despawn();
                networkObject = NetworkManager.SpawnManager.InstantiateAndSpawn(zombiePrefab.GetComponent<NetworkObject>(), clientId, false, true, true, spawnPosition);
                networkObject.GetComponent<PlayerController>().playerName.Value = name;
            }
            // Rpc for set the camera on the client that are this player
            var rpcParams = new RpcParams
            {
                Send = new RpcSendParams
                {
                    Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
                }
            };
            SetCameraRpc(networkObject.NetworkObjectId, rpcParams);
        }



        private void UpdateTeamUI()
        {
            if (humansText != null)
            {
                humansText.text = $"{numberOfHumans}";
            }

            if (zombiesText != null)
            {
                zombiesText.text = $"{numberOfZombies}";
            }
        }

        #endregion

        #region Modo de juego

        private void HandleTimeLimitedGameMode()
        {
            // Implementar la lógica para el modo de juego basado en tiempo
            if (isGameOver) return;

            // Decrementar remainingSeconds basado en Time.deltaTime
            remainingSeconds -= Time.deltaTime;

            // Comprobar si el tiempo ha llegado a cero
            if (remainingSeconds <= 0)
            {
                isGameOver = true;
                remainingSeconds = 0;
            }

            // Convertir remainingSeconds a minutos y segundos
            int minutesRemaining = Mathf.FloorToInt(remainingSeconds / 60);
            int secondsRemaining = Mathf.FloorToInt(remainingSeconds % 60);

            // Actualizar el texto de la interfaz de usuario
            if (gameModeText != null)
            {
                gameModeText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
            }

        }

        private void HandleCoinBasedGameMode()
        {
            if (isGameOver) return;

            // Implementar la lógica para el modo de juego basado en monedas
            if (gameModeText != null && playerController != null)
            {
                gameModeText.text = $"{playerController.CoinsCollected}/{CoinsGenerated}";
                if (playerController.CoinsCollected == CoinsGenerated)
                {
                    isGameOver = true;
                }
            }
        }

        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                Time.timeScale = 0f;
                gameOverPanel.SetActive(true); // Muestra el panel de pausa

                // Gestión del cursor
                Cursor.lockState = CursorLockMode.None; // Desbloquea el cursor
                Cursor.visible = true; // Hace visible el cursor
            }
        }

        public void ReturnToMainMenu()
        {
            // Gestión del cursor
            Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor
            Cursor.visible = false; // Oculta el cursor

            // Cargar la escena del menú principal
            SceneManager.LoadScene("MenuScene"); // Cambia "MenuScene" por el nombre de tu escena principal
        }

        #endregion

    }
}





