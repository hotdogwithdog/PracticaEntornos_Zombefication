using Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Menu.States
{
    internal class Pause : AMenuState
    {

        public Pause() : base("Menus/Pause") { }

        public override void Enter()
        {
            base.Enter();
            SceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnSceneChange(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuScene")
            {
                MenuManager.Instance.SetState(new Main());
            }
        }

        protected override void OnOptionClicked(MenuButtons option)
        {
            switch (option)
            {
                case MenuButtons.Resume:
                    MenuManager.Instance.SetState(new Gameplay());
                    break;
                case MenuButtons.Options:
                    MenuManager.Instance.SetState(new Options(false));
                    break;
                case MenuButtons.MainMenu:
                    if (NetworkManager.Singleton.IsHost)
                    {
                        MenuManager.Instance.GameManager.ChangeToMainMenuSceneRpc();
                        MenuManager.Instance.GameManager.ShutDown();
                        MenuManager.Instance.GameManager.DestroyAllManagers();
                        SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
                    }
                    else
                    {
                        MenuManager.Instance.GameManager.DisconectClient();
                        MenuManager.Instance.GameManager.DestroyAllManagers();
                        SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
                    }
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }

        public override void Exit()
        {
            base.Exit();
            SceneManager.activeSceneChanged -= OnSceneChange;
        }

        public override void Update(float deltaTime) { }
    }
}
