using Network;
using UI.Menu.Navigation;
using Unity.Netcode;
using UnityEngine;

namespace UI.Menu.States
{
    internal class Lobby : AMenuState
    {
        private GameObject _startGameButtonObject;
        private GameObject _hostGameButtonObject;
        private GameObject _joinGameButtonObject;

        public Lobby() : base("Menus/Lobby") { }

        public override void Enter()
        {
            base.Enter();

            foreach (MenuOption button in _menu.GetComponentInChildren<MenuOptionsGroup>().GetComponentsInChildren<MenuOption>())
            {
                if (button.Action == MenuButtons.StartGame) _startGameButtonObject = button.gameObject;
                else if (button.Action == MenuButtons.CreateLobby) _hostGameButtonObject = button.gameObject;
                else if (button.Action == MenuButtons.JoinLobby) _joinGameButtonObject = button.gameObject;
            }
            _startGameButtonObject?.SetActive(false);
        }

        protected override void OnOptionClicked(MenuButtons option)
        {
            switch (option)
            {
                case MenuButtons.StartGame:
                    GameManager.Instance.StartGame();
                    break;
                case MenuButtons.CreateLobby:
                    Debug.Log("Create");
                    GameManager.Instance.StartHost();
                    _startGameButtonObject.SetActive(true);
                    _hostGameButtonObject.SetActive(false);
                    _joinGameButtonObject.SetActive(false);
                    break;
                case MenuButtons.JoinLobby:
                    Debug.Log("Join");
                    GameManager.Instance.StartClient();
                    _hostGameButtonObject.SetActive(false);
                    _joinGameButtonObject.SetActive(false);
                    break;
                case MenuButtons.Back:
                    if (_startGameButtonObject.activeSelf)
                    {
                        GameManager.Instance.ShutDown();
                        _startGameButtonObject.SetActive(false);
                        _hostGameButtonObject.SetActive(true);
                        _joinGameButtonObject.SetActive(true);
                    }
                    else if (!_hostGameButtonObject.activeSelf && !_joinGameButtonObject.activeSelf)
                    {
                        GameManager.Instance.DisconectClient();
                        _hostGameButtonObject.SetActive(true);
                        _joinGameButtonObject.SetActive(true);
                    }
                    else
                    {
                        MenuManager.Instance.SetState(new Main());
                    }
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }
        public override void Update(float deltaTime) { }
    }
}
