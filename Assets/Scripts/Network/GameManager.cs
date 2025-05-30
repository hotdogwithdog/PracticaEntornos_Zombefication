using System.Collections.Generic;
using Level;
using UI.Menu;
using UI.Menu.States;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.FilePathAttribute;

namespace Network
{
    public struct GameOptions : INetworkSerializable
    {
        public Level.GameMode gameMode;
        public float maxTime;
        public float coinsDensity;
        public int numberOfRooms;
        public int roomWidth;
        public int roomLenght;

        public GameOptions(Level.GameMode gameMode, float maxTime, float coinsDensity, int numberOfRooms, int roomWidth, int roomLenght)
        {
            this.gameMode = gameMode;
            this.maxTime = maxTime;
            this.coinsDensity = coinsDensity;
            this.numberOfRooms = numberOfRooms;
            this.roomWidth = roomWidth;
            this.roomLenght = roomLenght;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref gameMode);
            serializer.SerializeValue(ref maxTime);
            serializer.SerializeValue(ref coinsDensity);
            serializer.SerializeValue(ref numberOfRooms);
            serializer.SerializeValue(ref roomWidth);
            serializer.SerializeValue(ref roomLenght);
        }

        public override string ToString()
        {
            return $"GameMode: {gameMode}; maxTime: {maxTime}; coinsDensity: {coinsDensity}; numberOfRooms: {numberOfRooms}; roomWidth: {roomWidth}; roomLenght: {roomLenght}";
        }
    }



    public class GameManager : NetworkBehaviour
    {
        #region NetworkVariables
        public NetworkVariable<GameOptions> gameOptions = new NetworkVariable<GameOptions>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> nPlayers = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        [SerializeField] private int maxConnections = 6;

        #region OnlyServer
        private Dictionary<ulong, FixedString64Bytes> _clientPlayerNames;
        private Utilities.UniqueIdGenerator _idGenerator;
        public string JoinCode {  get; private set; }
        #endregion

        #region ConectionMethods
        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(this);
            _clientPlayerNames = new Dictionary<ulong, FixedString64Bytes>();
        }

        public override void OnNetworkDespawn()
        {
            Debug.LogWarning("GAME MANAGER DESPAWN OF NET");
        }

        public async void StartHostRelay()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

                JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                RelayServerData serverData = new RelayServerData(allocation, "udp");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

                StartHost();
            }
            catch (RelayServiceException e)
            {
                Debug.LogError($"ERROR CREATING THE RELAY: {e.ToString()}");
            }
        }

        private bool StartHost()
        {
            bool state = NetworkManager.Singleton.StartHost();
            Debug.Log($"Server init : {state}");
            if (state)
            {
                _clientPlayerNames.Clear();
                _idGenerator = new Utilities.UniqueIdGenerator();   // Here one new for reset the internal cache of the class and don't repeat names
                AddName(NetworkManager.Singleton.LocalClientId);

                nPlayers.Value = 1; // The actual host
                NetworkManager.Singleton.OnClientConnectedCallback += ClientConnect;
                NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnect;
            }
            return state;
        }
        private void ClientConnect(ulong clientId)
        {
            if (!IsHost) return;    // no deberia ser necesario porque solo se suscribe el host
            nPlayers.Value++;
            AddName(clientId);
            Debug.Log($"Client connected : {clientId}");
        }

        private void ClientDisconnect(ulong clientId)
        {
            if (!IsHost) return;    // no deberia ser necesario porque solo se suscribe el host
            nPlayers.Value--;
            RemoveName(clientId);
            Debug.Log($"Client Disconnected : {clientId}");

            // In case that i are in gameplay i eliminate him in the clients lists of the gameplay
            LevelManager levelManager = GameObject.FindGameObjectWithTag("LevelManager").GetComponent<LevelManager>();
            if (levelManager != null)
            {
                levelManager.AbruptClientDisconnectUpdateLists(clientId);
            }
        }

        public async void StartClientRelay(string joinCode)
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData serverData = new RelayServerData(joinAllocation, "udp");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(serverData);

            StartClient();
        }

        private bool StartClient()
        {
            SceneManager.activeSceneChanged += OnSceneChange;
            return NetworkManager.Singleton.StartClient();
        }

        private void OnSceneChange(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuScene")
            {
                MenuManager.Instance.SetState(new Main());
            }
        }

        public void ShutDown()
        {
            Debug.Log("Server shutdown");
            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnect;

            _clientPlayerNames.Clear();

            NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.SetSingleton();
            DestroyNetworkManagerCopies();
        }

        public void StartGame()
        {
            Debug.Log("Game Starting");

            SetGameplayStateInAllClientsRpc();

            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public void DestroyNetworkManagerCopies()
        {
            GameObject[] netManagers = GameObject.FindGameObjectsWithTag("NetworkManager");
            for (int i = 0; i < netManagers.Length; ++i)
            {
                if (netManagers[i].GetComponent<NetworkManager>() != NetworkManager.Singleton)
                {
                    Destroy(netManagers[i]);
                }
            }
        }

        public void DisconectClient()
        {
            Debug.Log($"Client Disconnected: {NetworkManager.Singleton.LocalClientId}");
            // The client can't disconnect hitself of the server in a clean way without an Rpc so for don't convert this class that is a singleton
            // i restart the all network manager of the client.
            NetworkManager.Singleton.Shutdown();
            NetworkManager.Singleton.SetSingleton();
            DestroyAllManagers();
            SceneManager.LoadScene("MenuScene");
        }
        #endregion

        public ulong GetID()
        {
            return NetworkManager.Singleton.LocalClientId;
        }

        #region OptionSetters
        [Rpc(SendTo.Server)]
        public void SetGameModeRpc(Level.GameMode gameMode)
        {
            gameOptions.Value = new GameOptions(gameMode, gameOptions.Value.maxTime, gameOptions.Value.coinsDensity, 
                gameOptions.Value.numberOfRooms, gameOptions.Value.roomWidth, gameOptions.Value.roomLenght); 
        }
        [Rpc(SendTo.Server)]
        public void SetMaxTimeRpc(float maxTime)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, maxTime, gameOptions.Value.coinsDensity,
                gameOptions.Value.numberOfRooms, gameOptions.Value.roomWidth, gameOptions.Value.roomLenght);
        }
        [Rpc(SendTo.Server)]
        public void SetCoinsDensityRpc(float coinsDensity)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, gameOptions.Value.maxTime, coinsDensity,
                gameOptions.Value.numberOfRooms, gameOptions.Value.roomWidth, gameOptions.Value.roomLenght);
        }
        [Rpc(SendTo.Server)]
        public void SetNumberOfRoomsRpc(int numberOfRooms)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, gameOptions.Value.maxTime, gameOptions.Value.coinsDensity,
                numberOfRooms, gameOptions.Value.roomWidth, gameOptions.Value.roomLenght);
        }
        [Rpc(SendTo.Server)]
        public void SetRoomWidthRpc(int width)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, gameOptions.Value.maxTime, gameOptions.Value.coinsDensity,
                gameOptions.Value.numberOfRooms, width, gameOptions.Value.roomLenght);
        }
        [Rpc(SendTo.Server)]
        public void SetRoomLenghtRpc(int lenght)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, gameOptions.Value.maxTime, gameOptions.Value.coinsDensity,
                gameOptions.Value.numberOfRooms, gameOptions.Value.roomWidth, lenght);
        }
        #endregion

        [Rpc(SendTo.NotServer)]
        private void SetGameplayStateInAllClientsRpc()
        {
            MenuManager.Instance.SetState(new Gameplay());
        }

        [Rpc(SendTo.NotServer)]
        public void ChangeToMainMenuSceneRpc()
        {
            DestroyAllManagers();
            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }

        public void DestroyAllManagers()
        {
            if (MenuManager.Instance != null && MenuManager.Instance.GameManager != null) GameObject.Destroy(MenuManager.Instance.GameManager.gameObject);
            if (NetworkManager.Singleton != null) GameObject.Destroy(NetworkManager.Singleton.gameObject);
            if (MenuManager.Instance != null) GameObject.Destroy(MenuManager.Instance.gameObject);
        }

        private void AddName(ulong clientId)
        {
            string name = _idGenerator.GenerateUniqueID();
            if (!_clientPlayerNames.ContainsKey(clientId))
            {
                _clientPlayerNames.Add(clientId, name);
                Player.PlayerController playerController = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.GetComponent<Player.PlayerController>();
                playerController.playerName.Value = name;
                Debug.Log($"Name added: {name}");
            }
            else
            {
                Debug.LogWarning($"The name to add actually exist: {name}");
            }
        }

        private void RemoveName(ulong clientId)
        {
            if (_clientPlayerNames.ContainsKey(clientId))
            {
                Debug.Log($"Name removed: {_clientPlayerNames[clientId]}");
                _clientPlayerNames.Remove(clientId);
            }
        }
    }
}

