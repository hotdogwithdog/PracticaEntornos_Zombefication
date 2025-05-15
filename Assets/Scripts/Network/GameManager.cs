using System;
using System.Collections.Generic;
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
        private Dictionary<ulong, string> _clientsIDs;
        private Utilities.UniqueIdGenerator _idGenerator;

        #endregion

        #region ConectionMethods
        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(this);
            if (IsHost)
            {
                _clientsIDs = new Dictionary<ulong, string>();
                _idGenerator = new Utilities.UniqueIdGenerator();
                string hostID = _idGenerator.GenerateUniqueID();
                _clientsIDs.Add(NetworkManager.Singleton.LocalClientId, hostID);
                Debug.Log($"Host name : {hostID}");
            }
            else if (IsClient)
            {
                Debug.Log("Client AAAAAAAAAAA");
            }
        }

        public override void OnNetworkDespawn()
        {

        }

        public bool StartHost()
        {
            bool state = NetworkManager.Singleton.StartHost();
            Debug.Log($"Server init : {state}");
            if (state)
            {
                nPlayers.Value = 1; // The actual host
                NetworkManager.Singleton.OnConnectionEvent += HandleConnection;
            }
            return state;
        }

        private void HandleConnection(NetworkManager manager, ConnectionEventData data)
        {
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    nPlayers.Value++;
                    AddName(data.ClientId);
                    Debug.Log($"Client connected : {data.ClientId}");
                    break;
                case ConnectionEvent.ClientDisconnected:
                    nPlayers.Value--;
                    RemoveName(data.ClientId);
                    Debug.Log($"Client Disconnected : {data.ClientId}");
                    break;
                default:
                    Debug.Log($"OTHER TYPE OF CONNECTION : {data.EventType}");
                    return;
            }
        }

        public bool StartClient()
        {
            return NetworkManager.Singleton.StartClient();
        }

        public void ShutDown()
        {
            Debug.Log("Server shutdown");
            NetworkManager.Singleton.OnConnectionEvent -= HandleConnection;

            _clientsIDs.Clear();

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
        public void SetGameMode(Level.GameMode gameMode)
        {
            gameOptions.Value = new GameOptions(gameMode, gameOptions.Value.maxTime, gameOptions.Value.coinsDensity); 
        }
        public void SetMaxTime(float maxTime)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, maxTime, gameOptions.Value.coinsDensity);
        }
        public void SetCoinsDensity(float coinsDensity)
        {
            gameOptions.Value = new GameOptions(gameOptions.Value.gameMode, gameOptions.Value.maxTime, coinsDensity);
        }
        #endregion


        private void AddName(ulong clientId)
        {
            string id = _idGenerator.GenerateUniqueID();
            if (!_clientsIDs.ContainsKey(clientId))
            {
                _clientsIDs.Add(clientId, id);
                Debug.Log($"Name added: {id}");
            }
        }

        private void RemoveName(ulong clientId)
        {
            if (_clientsIDs.ContainsKey(clientId))
            {
                Debug.Log($"Name removed: {_clientsIDs[clientId]}");
                _clientsIDs.Remove(clientId);
            }
        }
    }
}

