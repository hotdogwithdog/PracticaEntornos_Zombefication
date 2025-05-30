using System;
using System.Runtime.CompilerServices;
using TMPro;
using UI.Menu.Navigation;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Menu.States
{
    internal class FinalScreen : AMenuState
    {
        private bool _isHost;
        private string _finalText;
        public FinalScreen(bool isHost, string finalText) : base("Menus/GameFinalPanel")
        {
            _isHost = isHost;
            _finalText = finalText;
        }

        public override void Enter()
        {
            base.Enter();

            foreach (MenuOption option in _menu.GetComponentInChildren<MenuOptionsGroup>().GetComponentsInChildren<MenuOption>())
            {
                if (option.Action == MenuButtons.Restart && _isHost == false) option.gameObject.SetActive(false);
            }

            GameObject.FindWithTag("FinishText").GetComponent<TextMeshProUGUI>().text = _finalText;
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
                case MenuButtons.Restart:
                    MenuManager.Instance.GameManager.StartGame();
                    MenuManager.Instance.SetState(new Gameplay());
                    break;
                case MenuButtons.MainMenu:
                    if (_isHost)
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
