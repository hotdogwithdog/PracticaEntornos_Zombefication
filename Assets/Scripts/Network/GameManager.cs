using Unity.Netcode;
using UnityEngine;

namespace Network
{
    public class GameManager : Utilities.Singleton<GameManager>
    {
        public bool StartHost()
        {
            return NetworkManager.Singleton.StartHost();
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


    }
}

