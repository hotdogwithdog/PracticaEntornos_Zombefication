using Network;
using UI.Lobby;
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
        private GameObject _lobbyOptions;
        private GameObject _lobbyCodeObjects;
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

            _lobbyOptions = _menu.GetComponentInChildren<LobbyOptionsManager>().gameObject;
            _lobbyOptions.SetActive(false);
            _lobbyCodeObjects = _menu.GetComponentInChildren<LobbyCodesManager>().gameObject;
            if (NetworkManager.Singleton.IsHost)
            {
                _lobbyCodeObjects.SetActive(false);
            }
            else
            {
                _lobbyCodeObjects.SetActive(true);
                _lobbyCodeObjects.GetComponent<LobbyCodesManager>().InitClient();
            }
        }

        protected override void OnOptionClicked(MenuButtons option)
        {
            switch (option)
            {
                case MenuButtons.StartGame:
                    MenuManager.Instance.SetState(new Gameplay());
                    MenuManager.Instance.GameManager.StartGame();
                    break;
                case MenuButtons.CreateLobby:
                    Debug.Log("Create");
                    MenuManager.Instance.GameManager.StartHostRelay();
                    _startGameButtonObject.SetActive(true);
                    _lobbyOptions.SetActive(true);
                    _hostGameButtonObject.SetActive(false);
                    _joinGameButtonObject.SetActive(false);
                    _lobbyCodeObjects.SetActive(true);
                    if (!MenuManager.Instance.GameManager.IsHostInit)
                    {
                        MenuManager.Instance.GameManager.OnHostInit += _lobbyCodeObjects.GetComponent<LobbyCodesManager>().InitHost;
                    }
                    else _lobbyCodeObjects.GetComponent<LobbyCodesManager>().InitHost();
                    break;
                case MenuButtons.JoinLobby:
                    Debug.Log("Join");
                    MenuManager.Instance.GameManager.StartClientRelay(_lobbyCodeObjects.GetComponent<LobbyCodesManager>().GetJoinCodeInputText());
                    _hostGameButtonObject.SetActive(false);
                    _joinGameButtonObject.SetActive(false);
                    break;
                case MenuButtons.Back:
                    if (_startGameButtonObject.activeSelf)
                    {
                        MenuManager.Instance.GameManager.ShutDown();
                        _startGameButtonObject.SetActive(false);
                        _lobbyOptions.SetActive(false);
                        _hostGameButtonObject.SetActive(true);
                        _joinGameButtonObject.SetActive(true);
                    }
                    else if (!_hostGameButtonObject.activeSelf && !_joinGameButtonObject.activeSelf)
                    {
                        MenuManager.Instance.GameManager.DisconectClient();
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
