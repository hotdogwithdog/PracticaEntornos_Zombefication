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
            NetworkManager.Singleton.Shutdown();
        }

        public void StartGame()
        {
            Debug.Log("Game Starting");

            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

    }
}

