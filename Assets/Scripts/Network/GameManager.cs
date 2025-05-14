using System;
using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class GameManager : Utilities.Singleton<GameManager>
    {
        private int _nClients = 0;

        public bool StartHost()
        {
            bool state = NetworkManager.Singleton.StartHost();
            Debug.Log($"Server init : {state}");
            if (state)
            {
                _nClients = 0;
                NetworkManager.Singleton.OnConnectionEvent += HandleConnection;
            }
            return state;
        }

        private void HandleConnection(NetworkManager manager, ConnectionEventData data)
        {
            if (manager != NetworkManager.Singleton) return;

            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    _nClients++;
                    break;
                case ConnectionEvent.ClientDisconnected:
                    _nClients--;
                    break;
                default:
                    Debug.Log($"OTHER TYPE OF CONNECTION : {data.EventType}");
                    return;
            }
            Debug.Log($"CLientes conectados: {_nClients}");
        }

        public bool StartClient()
        {
            return NetworkManager.Singleton.StartClient();
        }

        public void ShutDown()
        {
            Debug.Log("Server shutdown");
            NetworkManager.Singleton.Shutdown();
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

        public void GetNClients() { }

        public ulong GetID()
        {
            return NetworkManager.Singleton.LocalClientId;
        }
    }
}

