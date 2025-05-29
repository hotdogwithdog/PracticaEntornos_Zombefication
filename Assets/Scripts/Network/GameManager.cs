using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public struct GameOptions : INetworkSerializable
    {
        public Level.GameMode gameMode;
        public float maxTime;
        public float coinsDensity;

        public GameOptions(Level.GameMode gameMode, float maxTime, float coinsDensity)
        {
            this.gameMode = gameMode;
            this.maxTime = maxTime;
            this.coinsDensity = coinsDensity;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref gameMode);
            serializer.SerializeValue(ref maxTime);
            serializer.SerializeValue(ref coinsDensity);
        }

        public override string ToString()
        {
            return $"GameMode: {gameMode}; maxTime: {maxTime}; coinsDensity: {coinsDensity}";
        }
    }



    public class GameManager : NetworkBehaviour
    {
        #region NetworkVariables
        public NetworkVariable<GameOptions> gameOptions = new NetworkVariable<GameOptions>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> nPlayers = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        #endregion

        #region OnlyServer
        private Dictionary<ulong, FixedString64Bytes> _clientPlayerNames;
        private Utilities.UniqueIdGenerator _idGenerator;
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

        public bool StartHost()
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
        }

        public bool StartClient()
        {
            return NetworkManager.Singleton.StartClient();
        }

        public void ShutDown()
        {
            Debug.Log("Server shutdown");
            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnect;

            _clientPlayerNames.Clear();

            NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.SetSingleton();
        }

        public void StartGame()
        {
            Debug.Log("Game Starting");

            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public void DisconectClient()
        {
            Debug.Log($"Client Disconnected: {NetworkManager.Singleton.LocalClientId}");
            // The client can't disconnect hitself of the server in a clean way without an Rpc so for don't convert this class that is a singleton
            // i restart the all network manager of the client.
            NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.SetSingleton();
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
            gameOptions.Value = new GameOptions(gameMode, gameOptions.Value.maxTime, gameOptions.Value.coinsDensity); 
        }
        [Rpc(SendTo.Server)]
        public void SetMaxTimeRpc(float maxTime)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, maxTime, gameOptions.Value.coinsDensity);
        }
        [Rpc(SendTo.Server)]
        public void SetCoinsDensityRpc(float coinsDensity)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, gameOptions.Value.maxTime, coinsDensity);
        }
        #endregion

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

