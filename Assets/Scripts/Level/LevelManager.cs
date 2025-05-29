using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Player;
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
        private NetworkVariable<int> numberOfHumans = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> numberOfZombies = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> remainingSeconds = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> coinsCollected = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        #region OnlyServer
        private HashSet<ulong> _zombies;
        private HashSet<ulong> _humans;
        private int indexForHumans = 0;
        private int indexForZombies = 0;
        private bool[] _coinsPicked;
        #endregion

        private GameManager _gameManager;
        private GameOptions _gameOptions;   // Store locally because the real value is in the GameManager (must do not change while gameplay so in this state is somthing const)

        #region Properties

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject zombiePrefab;

        [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
        [SerializeField] private int minutes = 5;

        private List<Vector3> humanSpawnPoints = new List<Vector3>();
        private List<Vector3> zombieSpawnPoints = new List<Vector3>();

        // Referencias a los elementos de texto en el canvas
        private TextMeshProUGUI humansText;
        private TextMeshProUGUI zombiesText;
        private TextMeshProUGUI gameModeText;
        private TextMeshProUGUI gameModeConditionValueText;

        private int CoinsGenerated = 0;

        public string PlayerPrefabName => playerPrefab.name;
        public string ZombiePrefabName => zombiePrefab.name;

        private LevelBuilder levelBuilder;

        private PlayerController playerController;

        private bool isGameOver = false;

        public GameObject gameOverPanel; // Asigna el panel desde el inspector

        #endregion

        #region Unity game loop methods

        private void Awake()
        {
            levelBuilder = GetComponent<LevelBuilder>();
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
                    Transform gameModeTextTransform = panel.Find("GameMode");
                    Transform gameModeConditionTextTransform = panel.Find("GameModeConditionValue");

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

                    if (gameModeConditionTextTransform != null)
                    {
                        gameModeConditionValueText = gameModeConditionTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            Time.timeScale = 1f; // Asegurarse de que el tiempo no esté detenido
        }

        private void Update()
        {
            if (_gameOptions.gameMode == GameMode.Tiempo)
            {
                // Lógica para el modo de juego basado en tiempo
                HandleTimeLimitedGameMode();
            }
            else if (_gameOptions.gameMode == GameMode.Monedas)
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

            _gameOptions = _gameManager.gameOptions.Value;

            UpdateGameModeUI();

            if (IsHost)
            {
                GameInput.InputReader.Instance.onHumanConvert += OnHumanConvert;
                GameInput.InputReader.Instance.onZombieConvert += OnZombieConvert;

                GenerateTeams();

                GenerateWorldRpc(UnityEngine.Random.Range(0, 10000));

                remainingSeconds.Value = minutes * 60;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsHost)
            {
                GameInput.InputReader.Instance.onHumanConvert -= OnHumanConvert;
                GameInput.InputReader.Instance.onZombieConvert -= OnZombieConvert;
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
                if (IsHost) _coinsPicked = new bool[CoinsGenerated];
                Debug.Log($"Nivel generado en el cliente: {NetworkManager.Singleton.LocalClientId}");
            }
            // Say to the server that i can spawn (no problems of shyncronisation because the map exist in local at the time this petition is send
            SpawnMyPlayerObjectRpc(NetworkManager.Singleton.LocalClientId);

            UpdateTeamUI();
        }

        [Rpc(SendTo.Server)]
        private void SpawnMyPlayerObjectRpc(ulong clientId)
        {
            Debug.Log($"TP of the client {clientId} is in process");
            GameObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            if (_humans.Contains(clientId))
            {
                SpawnPlayer(true, player, clientId, humanSpawnPoints[indexForHumans % humanSpawnPoints.Count]);
                indexForHumans++;
            }
            else if (_zombies.Contains(clientId))
            {
                SpawnPlayer(false, player, clientId, zombieSpawnPoints[indexForZombies % zombieSpawnPoints.Count]);
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
            // TODO: Change that to be 50/50 humans/zombies with one more zombie in the even number of players cases
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
            GameObject currentPlayer = null;

            foreach(GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player.GetComponent<PlayerController>().OwnerClientId == NetworkManager.Singleton.LocalClientId) currentPlayer = player;
            }

            if (currentPlayer != null && currentPlayer.name.Contains(playerPrefab.name))
            {
                ChangeToZombie(currentPlayer);
            }
            else
            {
                Debug.Log("El jugador actual no es un humano.");
            }
        }

        public void ChangeToZombie(GameObject human)
        {
            Debug.Log("Cambiando a Zombie");

            if (human != null)
            {
                // Guardar la posición, rotación y uniqueID del humano actual
                Vector3 playerPosition = human.transform.position;
                Quaternion playerRotation = human.transform.rotation;
                FixedString64Bytes uniqueID = human.GetComponent<PlayerController>().playerName.Value;

                NetworkObject playerNetObject = human.GetComponent<NetworkObject>();
                ulong clientId = playerNetObject.OwnerClientId;

                // Update the dictionaries with the teams
                _humans.Remove(clientId);
                _zombies.Add(clientId);

                // Despawnear al humano actual
                playerNetObject.Despawn();

                // Spawnear el prefab del zombie en la misma posición y rotación
                playerNetObject = NetworkManager.SpawnManager.InstantiateAndSpawn(zombiePrefab.GetComponent<NetworkObject>(), clientId, false, true, true, playerPosition, playerRotation);
                playerNetObject.GetComponent<PlayerController>().playerName.Value = name;
                playerNetObject.GetComponent<PlayerController>().isZombie = true;    // Security

                // Set the camera in the player
                var rpcParams = new RpcParams
                {
                    Send = new RpcSendParams
                    {
                        Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
                    }
                };
                SetCameraRpc(playerNetObject.NetworkObjectId, rpcParams);

                // Obtener el componente PlayerController del zombie instanciado
                PlayerController playerController = playerNetObject.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    numberOfHumans.Value--; // Reducir el número de humanos
                    numberOfZombies.Value++; // Aumentar el número de zombis
                    UpdateTeamUI();
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
            GameObject currentPlayer = null;

            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player.GetComponent<PlayerController>().OwnerClientId == NetworkManager.Singleton.LocalClientId) currentPlayer = player;
            }

            if (currentPlayer != null && currentPlayer.name.Contains(zombiePrefab.name))
            {
                ChangeToHuman(currentPlayer);
            }
            else
            {
                Debug.Log("El jugador actual no es un zombie.");
            }
        }
        private void ChangeToHuman(GameObject zombie)
        {
            Debug.Log("Cambiando a Humano");

            

            if (zombie != null)
            {
                // Guardar la posición y rotación del jugador actual
                Vector3 playerPosition = zombie.transform.position;
                Quaternion playerRotation = zombie.transform.rotation;
                FixedString64Bytes uniqueID = zombie.GetComponent<PlayerController>().playerName.Value;

                NetworkObject playerNetObject = zombie.GetComponent<NetworkObject>();
                ulong clientId = playerNetObject.OwnerClientId;

                // Update the dictionaries with the teams
                _zombies.Remove(clientId);
                _humans.Add(clientId);

                // Despawnear al humano actual
                playerNetObject.Despawn();

                // Spawnear el prefab del zombie en la misma posición y rotación
                playerNetObject = NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab.GetComponent<NetworkObject>(), clientId, false, true, true, playerPosition, playerRotation);
                playerNetObject.GetComponent<PlayerController>().playerName.Value = name;
                playerNetObject.GetComponent<PlayerController>().isZombie = false;    // Security

                // Set the camera in the player
                var rpcParams = new RpcParams
                {
                    Send = new RpcSendParams
                    {
                        Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
                    }
                };
                SetCameraRpc(playerNetObject.NetworkObjectId, rpcParams);

                PlayerController playerController = playerNetObject.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    numberOfZombies.Value--;
                    numberOfHumans.Value++;
                    UpdateTeamUI();
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el humano instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontró el zombie actual.");
            }
        }

        private void SpawnPlayer(bool isHuman, GameObject player, ulong clientId, Vector3 spawnPosition)
        {
            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            if (isHuman)
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                // Store all the relevant data
                FixedString64Bytes name = playerController.playerName.Value;
                networkObject.Despawn();
                networkObject = NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab.GetComponent<NetworkObject>(), clientId, false, true, true, spawnPosition);
                networkObject.GetComponent<PlayerController>().playerName.Value = name;
                networkObject.GetComponent<PlayerController>().isZombie = false;    // Security
            }
            else
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                // Store all the relevant data
                FixedString64Bytes name = playerController.playerName.Value;
                networkObject.Despawn();
                networkObject = NetworkManager.SpawnManager.InstantiateAndSpawn(zombiePrefab.GetComponent<NetworkObject>(), clientId, false, true, true, spawnPosition);
                networkObject.GetComponent<PlayerController>().playerName.Value = name;
                networkObject.GetComponent<PlayerController>().isZombie = true;    // Security
            }
            Debug.Log($"Spawn of a {((isHuman) ? "human" : "zombie")} that is the client {clientId} in the position {spawnPosition}");
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
                humansText.text = $"{numberOfHumans.Value}";
            }

            if (zombiesText != null)
            {
                zombiesText.text = $"{numberOfZombies.Value}";
            }
        }

        private void UpdateGameModeUI()
        {
            if (gameModeText != null)
            {
                switch (_gameOptions.gameMode)
                {
                    case Level.GameMode.Tiempo:
                        gameModeText.text = "Time:";
                        break;
                    case Level.GameMode.Monedas:
                        gameModeText.text = "Coins:";
                        break;
                    default:
                        Debug.LogError($"ERROR UNKONW GAMEMODE: {_gameOptions.gameMode}");
                        return;
                }
            }
        }

        #endregion

        #region Modo de juego

        private void HandleTimeLimitedGameMode()
        {
            // Implementar la lógica para el modo de juego basado en tiempo
            if (isGameOver) return;

            if (IsHost)
            {
                // Decrementar remainingSeconds basado en Time.deltaTime
                remainingSeconds.Value -= Time.deltaTime;

                // Comprobar si el tiempo ha llegado a cero
                if (remainingSeconds.Value <= 0)
                {
                    isGameOver = true;
                    remainingSeconds.Value = 0;
                }
            }

            float remainingActualSeconds = remainingSeconds.Value;
            // Convertir remainingSeconds a minutos y segundos
            int minutesRemaining = Mathf.FloorToInt(remainingActualSeconds / 60);
            int secondsRemaining = Mathf.FloorToInt(remainingActualSeconds % 60);

            // Actualizar el texto de la interfaz de usuario
            if (gameModeConditionValueText != null)
            {
                gameModeConditionValueText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
            }

        }

        public void CoinCollected(int coinId)
        {
            if (_coinsPicked[coinId]) return;
            _coinsPicked[coinId] = true;
            coinsCollected.Value++;
        }

        private void HandleCoinBasedGameMode()
        {
            if (isGameOver) return;

            // Implementar la lógica para el modo de juego basado en monedas
            if (gameModeConditionValueText != null)
            {
                gameModeConditionValueText.text = $"{coinsCollected.Value}/{CoinsGenerated}";
                if (coinsCollected.Value == CoinsGenerated)
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





