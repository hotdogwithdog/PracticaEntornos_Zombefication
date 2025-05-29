using TMPro;
using UI.Menu.Navigation;
using UnityEditor;
using UnityEngine;

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
                    MenuManager.Instance.GameManager.ShutDown();
                    MenuManager.Instance.SetState(new Main());
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }
        public override void Update(float deltaTime) { }
    }
}
