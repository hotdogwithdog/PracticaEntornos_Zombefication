using Player;
using Unity.Netcode;
using UnityEngine;

namespace UI.Menu.States
{
    internal class Pause : AMenuState
    {
        public Pause() : base("Menus/Pause") { }

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
                    Debug.Log("Go to main menu");
                    break;
                default:
                    Debug.LogError($"ERROR_UNKOWN_OPTION: {option}");
                    return;
            }
        }
        public override void Update(float deltaTime) { }
    }
}
